using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Diagnostics;
using OpenBoardAnim.Utilities;

namespace OpenBoardAnim.Utils
{
    // A per-scene voiceover clip and the real-time offset (seconds from the start of the
    // export capture) at which its scene began - used to delay the clip into place when
    // mixing it against the looped background music.
    public record SceneAudioCue(string Path, double StartSeconds);

    public class VideoExporter
    {
        private Canvas _targetCanvas;
        private string _tempImageDir;
        private int _frameRate;
        private string _outputVideoPath;
        private string _audioPath;
        private double _audioVolumePercent;
        private List<SceneAudioCue> _sceneAudioCues;
        private int _frameCount;

        public VideoExporter(Canvas canvas, int frameRate, string outputVideoPath, string audioPath = null, double audioVolumePercent = 100, List<SceneAudioCue> sceneAudioCues = null)
        {
            try
            {
                _targetCanvas = canvas;
                _frameRate = frameRate;
                _outputVideoPath = outputVideoPath;
                _audioPath = audioPath;
                _audioVolumePercent = audioVolumePercent;
                _sceneAudioCues = sceneAudioCues ?? new List<SceneAudioCue>();
                _tempImageDir = Path.Combine(Path.GetTempPath(), "WpfAnimationFrames");
                if (Directory.Exists(_tempImageDir)) Directory.Delete(_tempImageDir, true); // Cleanup
                Directory.CreateDirectory(_tempImageDir);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }

        // Start capturing frames
        public void StartCapture()
        {
            CompositionTarget.Rendering += OnRendering;
        }

        // Stop capturing and compile the video
        public async Task StopCapture(IProgress<ExportProgressInfo> progress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                CompositionTarget.Rendering -= OnRendering;

                if (cancellationToken.IsCancellationRequested)
                {
                    CleanupTempFrames();
                    return;
                }

                await CompileVideo(progress, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                CleanupTempFrames();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }

        // Captures a single frame and writes it straight to disk instead of buffering
        // every frame in memory - a long export can be thousands of ~8MB frames.
        private void OnRendering(object sender, EventArgs e)
        {
            try
            {
                var rtb = new RenderTargetBitmap(
                            (int)_targetCanvas.Width,
                            (int)_targetCanvas.Height,
                            96, 96, PixelFormats.Pbgra32
                        );
                rtb.Render(_targetCanvas);

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));
                string framePath = Path.Combine(_tempImageDir, $"frame_{_frameCount:D4}.png");
                using (var stream = new FileStream(framePath, FileMode.Create))
                {
                    encoder.Save(stream);
                }
                _frameCount++;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }

        private async Task CompileVideo(IProgress<ExportProgressInfo> progress, CancellationToken cancellationToken)
        {
            Process process = null;
            try
            {
                progress?.Report(new ExportProgressInfo(85, "Encoding video..."));

                string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DLLs", "ffmpeg.exe");

                bool hasAudio = !string.IsNullOrWhiteSpace(_audioPath) && File.Exists(_audioPath);
                List<SceneAudioCue> voiceovers = _sceneAudioCues.Where(c => !string.IsNullOrWhiteSpace(c.Path) && File.Exists(c.Path)).ToList();
                string arguments;
                if (!hasAudio && voiceovers.Count == 0)
                {
                    arguments = $"-y -framerate {_frameRate} -i \"{_tempImageDir}/frame_%04d.png\" -c:v libx264 -pix_fmt yuv420p \"{_outputVideoPath}\"";
                }
                else if (hasAudio && voiceovers.Count == 0)
                {
                    // -stream_loop -1 on the (usually shorter) music track so it doesn't run
                    // out before the video does; -shortest then caps the output at the video's
                    // actual length instead of looping the audio forever.
                    string volume = (_audioVolumePercent / 100.0).ToString(CultureInfo.InvariantCulture);
                    arguments = $"-y -framerate {_frameRate} -i \"{_tempImageDir}/frame_%04d.png\" " +
                        $"-stream_loop -1 -i \"{_audioPath}\" -filter:a \"volume={volume}\" " +
                        $"-map 0:v:0 -map 1:a:0 -c:v libx264 -pix_fmt yuv420p -c:a aac -shortest \"{_outputVideoPath}\"";
                }
                else
                {
                    // Mix the (optional) looped background music with one or more voiceover
                    // clips, each delayed to the real-time offset its scene started at.
                    StringBuilder inputs = new();
                    inputs.Append($"-y -framerate {_frameRate} -i \"{_tempImageDir}/frame_%04d.png\" ");

                    List<string> filterParts = new();
                    List<string> mixLabels = new();
                    int nextInputIndex = 1;

                    if (hasAudio)
                    {
                        inputs.Append($"-stream_loop -1 -i \"{_audioPath}\" ");
                        string volume = (_audioVolumePercent / 100.0).ToString(CultureInfo.InvariantCulture);
                        filterParts.Add($"[{nextInputIndex}:a]volume={volume}[bg]");
                        mixLabels.Add("[bg]");
                        nextInputIndex++;
                    }

                    for (int i = 0; i < voiceovers.Count; i++)
                    {
                        inputs.Append($"-i \"{voiceovers[i].Path}\" ");
                        int delayMs = Math.Max(0, (int)Math.Round(voiceovers[i].StartSeconds * 1000));
                        filterParts.Add($"[{nextInputIndex}:a]adelay={delayMs}:all=1[vo{i}]");
                        mixLabels.Add($"[vo{i}]");
                        nextInputIndex++;
                    }

                    string finalAudioLabel;
                    if (mixLabels.Count == 1)
                    {
                        finalAudioLabel = mixLabels[0];
                    }
                    else
                    {
                        filterParts.Add($"{string.Join("", mixLabels)}amix=inputs={mixLabels.Count}:duration=longest:dropout_transition=0[aout]");
                        finalAudioLabel = "[aout]";
                    }

                    string filterComplex = string.Join(";", filterParts);

                    // A background track loops forever, so it's always at least as long as the
                    // video and -shortest safely caps the output at the video's length. Without
                    // one, voiceover clips are typically shorter than the whole video, so
                    // -shortest is skipped - ffmpeg then just lets the video continue silently
                    // once the mixed audio runs out, instead of truncating the video early.
                    string shortestFlag = hasAudio ? "-shortest " : "";
                    arguments = inputs.ToString() +
                        $"-filter_complex \"{filterComplex}\" -map 0:v:0 -map \"{finalAudioLabel}\" " +
                        $"-c:v libx264 -pix_fmt yuv420p -c:a aac {shortestFlag}\"{_outputVideoPath}\"";
                }

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process = new Process { StartInfo = processStartInfo };
                process.Start();

                try
                {
                    await process.WaitForExitAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    if (!process.HasExited)
                        process.Kill();
                    throw;
                }

                if (process.ExitCode != 0)
                    throw new InvalidOperationException($"ffmpeg exited with code {process.ExitCode} while encoding \"{_outputVideoPath}\".");

                CleanupTempFrames();
                progress?.Report(new ExportProgressInfo(100, "Export complete"));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
            finally
            {
                process?.Dispose();
            }
        }

        private void CleanupTempFrames()
        {
            try
            {
                if (Directory.Exists(_tempImageDir))
                    Directory.Delete(_tempImageDir, true);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to clean up temp export frames at {_tempImageDir}: {ex.Message}");
            }
        }
    }
}
