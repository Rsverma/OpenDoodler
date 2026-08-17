using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using OpenBoardAnim.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
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

namespace OpenBoardAnim.Views
{
    /// <summary>
    /// Interaction logic for ProjectPreviewView.xaml
    /// </summary>
    public partial class ProjectPreviewView : UserControl
    {
        private MediaPlayer _audioPlayer;

        public ProjectPreviewView()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProjectDetails project = this.DataContext as ProjectDetails;
                if (!string.IsNullOrWhiteSpace(project?.AudioPath) && File.Exists(project.AudioPath))
                {
                    _audioPlayer = new MediaPlayer();
                    _audioPlayer.Open(new Uri(project.AudioPath));
                    _audioPlayer.Volume = project.AudioVolume / 100.0;
                    _audioPlayer.Play();
                }
                await PreviewAndExportHandler.RunAnimationsOnCanvas(project, PreviewCanvas, false);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
            finally
            {
                _audioPlayer?.Stop();
                _audioPlayer?.Close();
                _audioPlayer = null;
            }
        }
    }
}
