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
        private List<BitmapFrame> frames = [];

        public VideoExporter(Canvas canvas, int frameRate)
        {
            try
            {
                _targetCanvas = canvas;
                _frameRate = frameRate;
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
        public void StopCapture()
        {
            try
            {
                CompositionTarget.Rendering -= OnRendering;
                CompileVideo();
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
                frames.Add(BitmapFrame.Create(rtb));
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }
        private void CompileVideo()
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
                }

                string ffmpegPath = "DLLs\\ffmpeg.exe"; // Path to FFmpeg
                string outputVideoPath = "output.mp4";

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-y -framerate {_frameRate} -i \"{_tempImageDir}/frame_%04d.png\" -c:v libx264 -pix_fmt yuv420p \"{outputVideoPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = new Process { StartInfo = processStartInfo })
                {
                    process.Start();
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }
    }
}
