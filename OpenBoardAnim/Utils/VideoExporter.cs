using System.IO;
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
        private List<BitmapFrame> frames = [];

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
        public async Task StopCapture(IProgress<ExportProgressInfo> progress = null)
        {
            try
            {
                CompositionTarget.Rendering -= OnRendering;
                await Task.Run(() => CompileVideo(progress));
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }

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
                BitmapFrame frame = BitmapFrame.Create(rtb);
                frame.Freeze();
                frames.Add(frame);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }
        private void CompileVideo(IProgress<ExportProgressInfo> progress)
        {
            try
            {
                // Save as PNG
                for (int currentFrame = 0; currentFrame < frames.Count; currentFrame++)
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(frames[currentFrame]);
                    string framePath = Path.Combine(_tempImageDir, $"frame_{currentFrame:D4}.png");
                    using var stream = new FileStream(framePath, FileMode.Create);
                    encoder.Save(stream);

                    if (frames.Count > 0)
                    {
                        double pct = 80 + (currentFrame + 1) / (double)frames.Count * 15;
                        progress?.Report(new ExportProgressInfo(pct, $"Writing frame {currentFrame + 1} of {frames.Count}..."));
                    }
                }

                progress?.Report(new ExportProgressInfo(95, "Encoding video..."));

                string ffmpegPath = "DLLs\\ffmpeg.exe"; // Path to FFmpeg

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-y -framerate {_frameRate} -i \"{_tempImageDir}/frame_%04d.png\" -c:v libx264 -pix_fmt yuv420p \"{_outputVideoPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new Process { StartInfo = processStartInfo })
                {
                    process.Start();
                    process.WaitForExit();
                }

                progress?.Report(new ExportProgressInfo(100, "Export complete"));
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }
    }
}
