using System.IO;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Diagnostics;
using OpenBoardAnim.Utilities;

namespace OpenBoardAnim.Utils
{
    public class VideoExporter
    {
        private Canvas _targetCanvas;
        private string _tempImageDir;
        private int _frameRate;
        private string _outputVideoPath;
        private int _frameCount;

        public VideoExporter(Canvas canvas, int frameRate, string outputVideoPath)
        {
            try
            {
                _targetCanvas = canvas;
                _frameRate = frameRate;
                _outputVideoPath = outputVideoPath;
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

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-y -framerate {_frameRate} -i \"{_tempImageDir}/frame_%04d.png\" -c:v libx264 -pix_fmt yuv420p \"{_outputVideoPath}\"",
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
