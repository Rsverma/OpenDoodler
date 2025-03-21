using OpenBoardAnim.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OpenBoardAnim.Utils
{
    public class PreviewAndExportHandler
    {
        public static async Task RunAnimationsOnCanvas(ProjectDetails project, Canvas canvas, bool isExport)
        {
            if (project != null)
            {
                Image hand = new()
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/Resources/pencil.png"))
                };
                VideoExporter exporter = null;
                if (isExport)
                {
                    exporter = new(canvas, 30);
                    exporter.StartCapture();
                }
                int index = 1;
                for (int i = 0; i < project.Scenes.Count - 1; i++)
                {
                    canvas.Children.Clear();
                    canvas.Children.Add(hand);
                    Canvas.SetLeft(hand, 0);
                    Canvas.SetTop(hand, 1150);
                    Canvas.SetZIndex(hand, 1);
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

                            List<PathGeometry> pathGeometries = GeometryHelper.GenerateMultiplePaths(pathGeometry, graphic is DrawingModel);
                            foreach (var geo in pathGeometries)
                            {
                                Path path = new Path
                                {
                                    Data = geo,
                                    Stroke = Brushes.Black
                                };
                                paths.Add(path);
                            }
                            var example = new PathAnimationHelper(canvas, paths, graphic, hand);
                            example.AnimatePathOnCanvas();

                            await example.tcs.Task;
                            if (element != null)
                            {
                                canvas.Children.Add(element);
                                Canvas.SetLeft(element, graphic.X);
                                Canvas.SetTop(element, graphic.Y);
                                int count = canvas.Children.Count - index - 1;
                                canvas.Children.RemoveRange(index, count);
                                index = canvas.Children.Count;
                            }
                        }
                    }
                }
                canvas.Children.Remove(hand);
                await Task.Delay(500);
                if (isExport)
                    exporter.StopCapture();
            }
        }
    }
}
