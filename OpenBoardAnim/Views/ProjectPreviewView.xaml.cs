using OpenBoardAnim.Models;
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
            ProjectDetails project = this.DataContext as ProjectDetails;
            NameScope.SetNameScope(this, new NameScope());
            if (project != null)
            {
                Image hand = new Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/Resources/pencil.png"))
                };
                PreviewCanvas.Children.Add(hand);
                Canvas.SetLeft(hand, 0);
                Canvas.SetTop(hand, 1150);
                for (int i = 0; i < project.Scenes.Count - 1; i++)
                {
                    SceneModel scene = project.Scenes[i];
                    if (scene != null)
                    {
                        for (int j = 0; j < scene.Graphics.Count; j++)
                        {
                            GraphicModelBase graphic = scene.Graphics[j];
                            await Task.Delay((int)graphic.Delay * 1000);
                            List<Path> paths = [];
                            Geometry geometry = null;
                            UIElement element = null;
                            if (graphic is DrawingModel drawing)
                            {
                                DrawingGroup drawingGroup = drawing.ImgDrawingGroup.Clone();
                                drawingGroup.Transform = new ScaleTransform(drawing.ResizeRatio, drawing.ResizeRatio);
                                geometry = GeometryHelper.ConvertToGeometry(drawingGroup);
                                element = new Image
                                {
                                    Source = new DrawingImage(drawingGroup)
                                };
                            }
                            else if (graphic is TextModel text)
                            {
                                geometry = text.TextGeometry;
                                element = new TextBlock()
                                {
                                    Text = text.RawText,
                                    Foreground = Brushes.Black,
                                    FontFamily = text.SelectedFontFamily,
                                    FontSize = text.SelectedFontSize,
                                    FontStyle = text.SelectedFontStyle,
                                    FontWeight = text.SelectedFontWeight
                                };
                                //paths.Add(GetPathFromGeometry(Brushes.Black, text.TextGeometry));
                            }
                            PathGeometry pathGeometry = geometry.GetFlattenedPathGeometry();

                            List<PathGeometry> pathGeometries = GenrateMultiplePaths(pathGeometry, graphic is DrawingModel);
                            foreach (var geo in pathGeometries)
                            {
                                Path path = new Path
                                {
                                    Data = geo,
                                    Stroke = Brushes.Black
                                };
                                paths.Add(path);
                            }
                            var example = new PathAnimationExample(PreviewCanvas, paths, graphic, hand);
                            example.AnimatePathOnCanvas();

                            await example.tcs.Task;
                            if (element != null)
                            {
                                PreviewCanvas.Children.Add(element);
                                Canvas.SetLeft(element, graphic.X);
                                Canvas.SetTop(element, graphic.Y);
                            }
                        }
                    }
                }
                PreviewCanvas.Children.Remove(hand);
            }
        }

        private static List<PathGeometry> GenrateMultiplePaths(PathGeometry pathGeometry, bool v)
        {
            List <PathGeometry> paths = new List<PathGeometry>();
            // Iterate through each PathFigure in the PathGeometry
            foreach (PathFigure figure in pathGeometry.Figures)
            {
                PathFigure[] arr = [figure.Clone()];
                PathGeometry geometry = new PathGeometry(arr);
                if(v)
                    geometry.Transform = new TranslateTransform(-pathGeometry.Bounds.Left,-pathGeometry.Bounds.Top);
                paths.Add(geometry);
            }
            return paths;
        }
        private static Path GetPathFromGeometry(Brush brush, PathGeometry pathGeo)
        {
            pathGeo.Freeze();
            Path path = new Path
            {
                Data = pathGeo,
                Fill = brush,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            return path;
        }
    }
}
