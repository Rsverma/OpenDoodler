using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using OpenBoardAnim.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        public ProjectPreviewView()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProjectDetails project = this.DataContext as ProjectDetails;
                await PreviewAndExportHandler.RunAnimationsOnCanvas(project, PreviewCanvas, false);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        
    }
}
