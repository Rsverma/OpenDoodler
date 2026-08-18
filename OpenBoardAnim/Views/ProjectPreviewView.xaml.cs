using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using OpenBoardAnim.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OpenBoardAnim.Views
{
    /// <summary>
    /// Interaction logic for ProjectPreviewView.xaml
    /// </summary>
    public partial class ProjectPreviewView : UserControl
    {
        private MediaPlayer _audioPlayer;
        private DispatcherTimer _audioTrimTimer;
        // Button_Click is "async void" (a UI event handler), so it isn't tied to the dialog
        // window's lifetime at all - closing the window doesn't stop it or the audio it started.
        // Cancelling this on Unloaded (which fires as the window tears down its content) is what
        // actually stops both the animation loop and the players in the finally block below.
        private CancellationTokenSource _previewCts;

        public ProjectPreviewView()
        {
            InitializeComponent();
            Unloaded += (s, e) => _previewCts?.Cancel();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            _previewCts?.Cancel();
            _previewCts = new CancellationTokenSource();
            CancellationToken cancellationToken = _previewCts.Token;
            try
            {
                ProjectDetails project = this.DataContext as ProjectDetails;
                if (!string.IsNullOrWhiteSpace(project?.AudioPath) && File.Exists(project.AudioPath))
                {
                    _audioPlayer = new MediaPlayer();
                    _audioPlayer.Open(new Uri(project.AudioPath));
                    _audioPlayer.Volume = project.AudioVolume / 100.0;
                    _audioPlayer.Position = TimeSpan.FromSeconds(Math.Max(0, project.AudioTrimStart));
                    _audioPlayer.Play();
                    if (project.AudioTrimEnd > project.AudioTrimStart)
                        _audioTrimTimer = PreviewAndExportHandler.StartTrimStopTimer(_audioPlayer, project.AudioTrimEnd);
                }
                await PreviewAndExportHandler.RunAnimationsOnCanvas(project, PreviewCanvas, false, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when the preview window is closed, or Play is clicked again, mid-playback.
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
            finally
            {
                _audioTrimTimer?.Stop();
                _audioTrimTimer = null;
                _audioPlayer?.Stop();
                _audioPlayer?.Close();
                _audioPlayer = null;
            }
        }
    }
}
