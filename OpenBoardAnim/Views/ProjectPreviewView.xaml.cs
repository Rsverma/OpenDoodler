using OpenBoardAnim.Models;
using OpenBoardAnim.Utils;
using System;
using System.Collections.Generic;
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
            ProjectDetails project = this.DataContext as ProjectDetails;
            NameScope.SetNameScope(this, new NameScope());
            if (project != null)
            {
                Image hand = new Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/Resources/pencil.png"))
                };
                    PreviewCanvas.Children.Add(hand);
                for (int i = 0; i < project.Scenes.Count - 1; i++)
                {
                    SceneModel scene = project.Scenes[i];
                    if (scene != null)
                    {
                        for (int j = 0; j < scene.Graphics.Count; j++)
                        {
                            GraphicModel graphic = scene.Graphics[j];
                            await Task.Delay((int)graphic.Delay * 1000);
                            List<GeometryWithFill> list = SVGHelper.ConvertToGeometry(graphic.ImgGeometry.Clone(), new TransformGroup());
                            List<Path> paths = new List<Path>();
                            foreach (GeometryWithFill geo in list)
                            {
                                PathGeometry pathGeo = geo.Geometry.GetFlattenedPathGeometry();
                                pathGeo.Freeze();
                                Path path = new Path
                                {
                                    Data = pathGeo,
                                    Fill = geo.Brush,
                                    Stroke = Brushes.Black,
                                    StrokeThickness = 1
                                };
                                paths.Add(path);
                            }
                            var example = new PathAnimationExample(PreviewCanvas, paths, graphic, hand);
                            example.AnimatePathOnCanvas();


                            //var animation = new ControlPathAnimation();
                            //animation.AnimateControlAlongPath(PreviewCanvas, path, graphic, hand);
                            await Task.Delay((int)graphic.Duration * 1000);
                        }
                    }
                }
                PreviewCanvas.Children.Remove(hand);
            }
        }

    }
}
