using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Diagnostics;
using OpenBoardAnim.Utilities;

namespace OpenBoardAnim.Utils
{
    // A per-scene voiceover clip and the real-time offset (seconds from the start of the
    // export capture) at which its scene began - used to delay the clip into place when
    // mixing it against the looped background music. TrimStart/TrimEnd are seconds into the
    // source file itself (which portion of it plays), independent of StartSeconds (when that
    // portion begins in the exported timeline); TrimEnd of 0 means "play to the file's end".
    public record SceneAudioCue(string Path, double StartSeconds, double TrimStart = 0, double TrimEnd = 0);

    public class VideoExporter
    {
        private Canvas _targetCanvas;
        private string _tempImageDir;
        private int _frameRate;
        private string _outputVideoPath;
        private string _audioPath;
        private double _audioVolumePercent;
        private double _audioTrimStart;
        private double _audioTrimEnd;
        private List<SceneAudioCue> _sceneAudioCues;
        private int _frameCount;
        // Rough estimate (GetEstimatedSceneDurationSeconds * frame rate) of how many frames
        // this export will end up capturing - used to report progress as a fraction of frames
        // captured so far rather than scenes/graphics processed, since those don't track real
        // capture time linearly (a single hand-drawn stroke scene can outlast several static
        // ones combined).
        private readonly IProgress<ExportProgressInfo> _progress;
        private readonly int _estimatedTotalFrames;
        // Frames captured (OnRendering, live during capture) can outpace frames actually
        // written to disk (WriteQueuedFramesAsync, a single background consumer) if disk I/O is
        // the bottleneck - StopCapture then has to sit and wait for that backlog to drain before
        // ffmpeg can start, since it reads the frame files, not the in-memory queue. _isDraining
        // gates a second round of progress reporting (70-80%) for exactly that wait, so it isn't
        // just a stall at whatever percentage capture happened to end on.
        private int _framesWritten;
        private volatile bool _isDraining;
        // Measures real elapsed time across the capture. Also the source of each frame's own
        // timestamp (see _frameTimestamps) - a single averaged fps across the whole capture
        // isn't good enough, because real per-frame throughput varies a lot within one export
        // (a hand-drawn stroke scene renders far slower than a static one), so assuming uniform
        // spacing drifts audio cues (based on true wall-clock time) out of sync with a video
        // built on that average rate - most visibly, a voiceover creeping into the wrong scene
        // the further into the export it is.
        private Stopwatch _captureStopwatch;
        // CompositionTarget.Rendering fires at the display's own refresh rate (60/120/144Hz+),
        // not at _frameRate - without gating, a high-refresh monitor would render, BMP-encode,
        // and disk-write several times more frames than the export video will ever use, all for
        // no visual benefit (the concat list already times each frame by its own real elapsed
        // timestamp, so extra frames beyond _frameRate just add I/O, not smoothness). Tracks the
        // stopwatch time of the last frame actually captured so OnRendering can skip ticks that
        // land inside the same 1/_frameRate window.
        private double _lastCapturedSeconds = double.NegativeInfinity;
        // Real capture time of each frame, in the same order as _frameCount - used to build a
        // concat-demuxer list with each frame's own duration instead of assuming a fixed
        // -framerate, so the video's internal timing matches wall-clock time exactly rather than
        // on average.
        private readonly List<double> _frameTimestamps = new();
        // OnRendering only renders+freezes the bitmap and hands it off here; the actual
        // encode-to-BMP and disk write happen on this single background consumer instead of
        // synchronously on the UI thread. That's what OnRendering was actually bottlenecked on
        // (PNG encoding a full-canvas bitmap is expensive) - CompositionTarget.Rendering could
        // only fire as often as that finished, capping real throughput far below 30fps and
        // making the exported motion visibly choppy even once the timing math above was correct.
        private Channel<(int Index, BitmapSource Bitmap)> _frameChannel;
        private Task _frameWriterTask;

        public VideoExporter(Canvas canvas, int frameRate, string outputVideoPath, string audioPath = null, double audioVolumePercent = 100,
            List<SceneAudioCue> sceneAudioCues = null, double audioTrimStart = 0, double audioTrimEnd = 0,
            IProgress<ExportProgressInfo> progress = null, int estimatedTotalFrames = 0)
        {
            try
            {
                _targetCanvas = canvas;
                _frameRate = frameRate;
                _outputVideoPath = outputVideoPath;
                _audioPath = audioPath;
                _audioVolumePercent = audioVolumePercent;
                _audioTrimStart = audioTrimStart;
                _audioTrimEnd = audioTrimEnd;
                _sceneAudioCues = sceneAudioCues ?? new List<SceneAudioCue>();
                _progress = progress;
                _estimatedTotalFrames = estimatedTotalFrames;
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
            _frameChannel = Channel.CreateUnbounded<(int, BitmapSource)>();
            _frameWriterTask = Task.Run(() => WriteQueuedFramesAsync(_frameChannel.Reader));
            _captureStopwatch = Stopwatch.StartNew();
            CompositionTarget.Rendering += OnRendering;
        }

        // Stop capturing and compile the video
        public async Task StopCapture(IProgress<ExportProgressInfo> progress = null, CancellationToken cancellationToken = default)
        {
            try
            {
                CompositionTarget.Rendering -= OnRendering;
                _captureStopwatch?.Stop();
                _isDraining = true;
                _frameChannel.Writer.Complete();
                await _frameWriterTask; // wait for every queued frame to actually land on disk

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

        // Renders and hands the bitmap off to the background writer instead of encoding/saving
        // it here - Freeze() is required to make it safe to touch from that other thread. This
        // is the part that has to stay on the UI thread (RenderTargetBitmap.Render needs it);
        // keeping it to just that is what lets CompositionTarget.Rendering fire close to its
        // natural rate instead of being capped by encode+disk-write time on every tick.
        private void OnRendering(object sender, EventArgs e)
        {
            try
            {
                double elapsedSeconds = _captureStopwatch.Elapsed.TotalSeconds;
                if (!ExportProgressMath.ShouldCaptureFrame(elapsedSeconds, _lastCapturedSeconds, _frameRate))
                    return;
                _lastCapturedSeconds = elapsedSeconds;

                var rtb = new RenderTargetBitmap(
                            (int)_targetCanvas.Width,
                            (int)_targetCanvas.Height,
                            96, 96, PixelFormats.Pbgra32
                        );
                rtb.Render(_targetCanvas);
                rtb.Freeze();

                _frameTimestamps.Add(elapsedSeconds);
                _frameChannel.Writer.TryWrite((_frameCount, rtb));
                _frameCount++;

                if (_progress != null && _estimatedTotalFrames > 0)
                {
                    // Capped below 70 - actual progress can outrun the rough estimate (a real
                    // scene often finishes faster/slower than GetEstimatedSceneDurationSeconds
                    // guessed); 70-80 is reserved for flushing any not-yet-written frame backlog
                    // to disk (see _isDraining) and 80-100 for the encoding phase in CompileVideo.
                    double pct = ExportProgressMath.CapturePercentage(_frameCount, _estimatedTotalFrames);
                    // Once real capture overruns the estimate, "X of ~Y" would show X past Y,
                    // which reads as broken rather than just an estimate falling short - drop
                    // the "of ~Y" part in that case instead.
                    string status = _frameCount <= _estimatedTotalFrames
                        ? $"Capturing frame {_frameCount} of ~{_estimatedTotalFrames}..."
                        : $"Capturing frame {_frameCount}...";
                    _progress.Report(new ExportProgressInfo(pct, status));
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }

        // Single background consumer so frame writes don't contend with each other on disk I/O.
        // BMP instead of PNG - these are temp files immediately fed to ffmpeg and deleted right
        // after, so there's no reason to pay PNG's compression cost (the main thing that was
        // capping real capture throughput) for a smaller intermediate file nobody keeps.
        private async Task WriteQueuedFramesAsync(ChannelReader<(int Index, BitmapSource Bitmap)> reader)
        {
            await foreach ((int index, BitmapSource bitmap) in reader.ReadAllAsync())
            {
                try
                {
                    var encoder = new BmpBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    string framePath = Path.Combine(_tempImageDir, $"frame_{index:D4}.bmp");
                    using var stream = new FileStream(framePath, FileMode.Create);
                    encoder.Save(stream);
                    _framesWritten++;

                    // Only report once StopCapture has stopped capturing and is waiting on this
                    // same task to drain the backlog - during live capture, OnRendering's own
                    // 0-70% reports already track overall progress, and this writer runs
                    // concurrently the whole time (started back in StartCapture), so reporting
                    // here too would just make the percentage jump around between two different
                    // notions of "how far along". _frameCount is fixed by the time draining
                    // starts (capture has stopped), so it's safe to use as the drain's total.
                    if (_isDraining && _progress != null && _frameCount > 0)
                    {
                        double pct = ExportProgressMath.DrainPercentage(_framesWritten, _frameCount);
                        _progress.Report(new ExportProgressInfo(pct, $"Saving frame {_framesWritten} of {_frameCount}..."));
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Failed to write export frame {index}: {ex.Message}");
                }
            }
        }

        private async Task CompileVideo(IProgress<ExportProgressInfo> progress, CancellationToken cancellationToken)
        {
            Process process = null;
            try
            {
                // Starts at 80 (not some arbitrary placeholder like 85) so this can't ever look
                // like it regresses once ReadEncodingProgressAsync's first real out_time_us=0
                // reading comes in and reports 80 itself.
                progress?.Report(new ExportProgressInfo(80, "Encoding video..."));

                string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DLLs", "ffmpeg.exe");

                // Total real capture time - used to cap the output at the video's actual length
                // (below) so audio (background music or an uncapped last-scene voiceover) can
                // never outlast the video and leave it frozen on the last frame while audio
                // keeps playing.
                double videoDurationSeconds = _captureStopwatch?.Elapsed.TotalSeconds ?? 0;
                string videoDuration = videoDurationSeconds.ToString("0.000", CultureInfo.InvariantCulture);
                string concatListPath = BuildConcatListFile();
                string videoInput = $"-f concat -safe 0 -i \"{concatListPath}\"";

                bool hasAudio = !string.IsNullOrWhiteSpace(_audioPath) && File.Exists(_audioPath);
                List<SceneAudioCue> voiceovers = _sceneAudioCues.Where(c => !string.IsNullOrWhiteSpace(c.Path) && File.Exists(c.Path)).ToList();
                string arguments;
                // veryfast trades some compression efficiency (slightly larger file) for a much
                // faster encode than libx264's "medium" default - a reasonable trade for a local,
                // one-off export that isn't optimizing for streaming bandwidth.
                const string encodePreset = "-preset veryfast";
                if (!hasAudio && voiceovers.Count == 0)
                {
                    arguments = $"-y {videoInput} -r {_frameRate} -c:v libx264 {encodePreset} -pix_fmt yuv420p \"{_outputVideoPath}\"";
                }
                else if (hasAudio && voiceovers.Count == 0)
                {
                    // -stream_loop -1 on the (usually shorter) music track so it doesn't run out
                    // before the video does; -t below caps the output at the video's actual
                    // length instead of looping the audio forever. The trim args (-ss/-t) are
                    // input options, so they must sit right before this input's own -i and apply
                    // to each loop iteration, looping just the trimmed segment.
                    string volume = (_audioVolumePercent / 100.0).ToString(CultureInfo.InvariantCulture);
                    string audioTrimArgs = ExportProgressMath.BuildTrimArgs(_audioTrimStart, _audioTrimEnd);
                    arguments = $"-y {videoInput} " +
                        $"{audioTrimArgs}-stream_loop -1 -i \"{_audioPath}\" -filter:a \"volume={volume}\" " +
                        $"-map 0:v:0 -map 1:a:0 -r {_frameRate} -c:v libx264 {encodePreset} -pix_fmt yuv420p -c:a aac -t {videoDuration} \"{_outputVideoPath}\"";
                }
                else
                {
                    // Mix the (optional) looped background music with one or more voiceover
                    // clips, each delayed to the real-time offset its scene started at.
                    StringBuilder inputs = new();
                    inputs.Append($"-y {videoInput} ");

                    List<string> filterParts = new();
                    List<string> mixLabels = new();
                    int nextInputIndex = 1;

                    if (hasAudio)
                    {
                        inputs.Append($"{ExportProgressMath.BuildTrimArgs(_audioTrimStart, _audioTrimEnd)}-stream_loop -1 -i \"{_audioPath}\" ");
                        string volume = (_audioVolumePercent / 100.0).ToString(CultureInfo.InvariantCulture);
                        filterParts.Add($"[{nextInputIndex}:a]volume={volume}[bg]");
                        mixLabels.Add("[bg]");
                        nextInputIndex++;
                    }

                    for (int i = 0; i < voiceovers.Count; i++)
                    {
                        inputs.Append($"{ExportProgressMath.BuildTrimArgs(voiceovers[i].TrimStart, voiceovers[i].TrimEnd)}-i \"{voiceovers[i].Path}\" ");
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

                    // -t caps the output at the video's actual length - needed both for a
                    // looped background track (which never ends on its own) and for a voiceover
                    // whose own natural/trimmed length runs past the video (most likely on the
                    // last scene, which has nothing after it to cap its end against). Without
                    // this, audio could outlast the video and leave it frozen on the last frame
                    // while audio kept playing.
                    arguments = inputs.ToString() +
                        $"-filter_complex \"{filterComplex}\" -map 0:v:0 -map \"{finalAudioLabel}\" " +
                        $"-r {_frameRate} -c:v libx264 {encodePreset} -pix_fmt yuv420p -c:a aac -t {videoDuration} \"{_outputVideoPath}\"";
                }

                // -progress pipe:1 makes ffmpeg emit machine-readable key=value progress lines
                // (including out_time_us=<microseconds encoded so far>) to stdout as it works,
                // instead of only the human-readable stats line on stderr - lets the 80-99%
                // range advance smoothly through encoding instead of jumping straight from the
                // 80% report above to 100% once the whole process exits. -nostats suppresses
                // the stderr stats line, which would otherwise still print (harmless, just
                // unused - not redirected).
                arguments = $"-progress pipe:1 -nostats {arguments}";

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                process = new Process { StartInfo = processStartInfo };
                process.Start();

                Task progressReaderTask = ReadEncodingProgressAsync(process, progress, videoDurationSeconds, cancellationToken);

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
                finally
                {
                    await progressReaderTask.ConfigureAwait(false);
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

        // Reads ffmpeg's `-progress pipe:1` output (a stream of key=value lines, one block per
        // update) off the redirected stdout and turns each out_time_us= line into an 80-99%
        // progress report - the encoded output's own timestamp as a fraction of the total video
        // duration. Runs concurrently with the WaitForExitAsync in CompileVideo, for as long as
        // ffmpeg keeps the pipe open.
        private static async Task ReadEncodingProgressAsync(Process process, IProgress<ExportProgressInfo> progress, double videoDurationSeconds, CancellationToken cancellationToken)
        {
            if (progress == null) return;
            try
            {
                string line;
                while ((line = await process.StandardOutput.ReadLineAsync(cancellationToken).ConfigureAwait(false)) != null)
                {
                    if (!line.StartsWith("out_time_us="))
                        continue;
                    if (!double.TryParse(line.AsSpan("out_time_us=".Length), NumberStyles.Number, CultureInfo.InvariantCulture, out double outTimeUs))
                        continue;

                    double elapsedSeconds = outTimeUs / 1_000_000.0;
                    double pct = ExportProgressMath.EncodePercentage(elapsedSeconds, videoDurationSeconds);
                    progress.Report(new ExportProgressInfo(pct, "Encoding video..."));
                }
            }
            catch (OperationCanceledException)
            {
                // Export was canceled - the caller kills the process, which closes this pipe and
                // unblocks ReadLineAsync on its own; nothing further to report.
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to read ffmpeg encoding progress: {ex.Message}");
            }
        }

        // Writes an ffmpeg concat-demuxer list giving each captured frame its own real duration
        // (the gap to the next frame's actual timestamp), instead of assuming every frame is
        // spaced by a fixed 1/framerate interval. That's what lets the video's internal timing
        // track wall-clock time exactly - and so stay in sync with voiceover cues, which are
        // scheduled by wall-clock time too - even though real per-frame capture speed varies
        // through the export (a hand-drawn stroke scene renders much slower than a static one).
        private string BuildConcatListFile()
        {
            string listPath = Path.Combine(_tempImageDir, "concat_list.txt");
            double totalElapsedSeconds = _captureStopwatch?.Elapsed.TotalSeconds ?? 0;
            List<FrameDurationEntry> entries = ExportProgressMath.BuildFrameDurations(_frameTimestamps, totalElapsedSeconds);
            using (StreamWriter writer = new(listPath, false))
            {
                foreach (FrameDurationEntry entry in entries)
                {
                    writer.WriteLine($"file '{entry.FrameName}'");
                    writer.WriteLine($"duration {entry.Duration.ToString("0.000000", CultureInfo.InvariantCulture)}");
                }
                if (_frameTimestamps.Count > 0)
                    writer.WriteLine($"file '{ExportProgressMath.LastFrameName(_frameTimestamps.Count)}'");
            }
            return listPath;
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
