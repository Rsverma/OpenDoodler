﻿using OpenBoardAnim.Models;
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
                            if (graphic is DrawingModel drawing)
                                paths = GetPathsForGraphic(drawing);
                            else if (graphic is TextModel text)
                                paths.Add(GetPathFromGeometry(Brushes.Black, text.TextGeometry));
                            var example = new PathAnimationExample(PreviewCanvas, paths, graphic, hand);
                            example.AnimatePathOnCanvas();

                            await example.tcs.Task;

                            //var animation = new ControlPathAnimation();
                            //animation.AnimateControlAlongPath(PreviewCanvas, path, graphic, hand);
                        }
                    }
                }
                PreviewCanvas.Children.Remove(hand);
            }
        }

        private static List<Path> GetPathsForGraphic(DrawingModel graphic)
        {
            List<Path> paths = new List<Path>();
            TransformGroup group = new TransformGroup();
            group.Children.Add(new ScaleTransform(graphic.ResizeRatio, graphic.ResizeRatio));
            List<GeometryWithFill> list = GeometryHelper.ConvertToGeometry(graphic.ImgDrawingGroup.Clone(), group);
            foreach (GeometryWithFill geo in list)
            {
                PathGeometry pathGeometry = geo.Geometry.GetFlattenedPathGeometry();
                
                PathGeometry pathGeo = pathGeometry;
                Path path = GetPathFromGeometry(geo.Brush, pathGeo);
                paths.Add(path);
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
