using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OpenBoardAnim.Utils
{
    public class PreviewAndExportHandler
    {
        public static async Task RunAnimationsOnCanvas(ProjectDetails project, Canvas canvas, bool isExport, IProgress<ExportProgressInfo> progress = null, string outputVideoPath = null, CancellationToken cancellationToken = default)
        {
            VideoExporter exporter = null;
            try
            {
                if (project == null) return;
                EntranceStyle entranceStyle = project.Settings?.EntranceStyle ?? EntranceStyle.HandDrawn;
                Brush strokeBrush = Brushes.Black;
                try
                {
                    if (!string.IsNullOrWhiteSpace(project.Settings?.StrokeColorHex))
                        strokeBrush = (Brush)new BrushConverter().ConvertFromString(project.Settings.StrokeColorHex);
                }
                catch (FormatException) { /* keep default black on an unparsable hex value */ }
                double strokeWidth = project.Settings != null && project.Settings.StrokeWidth > 0 ? project.Settings.StrokeWidth : 1;

                Image hand = new()
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/Resources/pencil.png"))
                };
                if (isExport)
                {
                    exporter = new(canvas, 30, outputVideoPath, project.AudioPath, project.AudioVolume);
                    exporter.StartCapture();
                }
                int totalGraphics = project.Scenes.Sum(s => s.Graphics?.Count ?? 0);
                int processedGraphics = 0;
                int index = 1;
                for (int i = 0; i < project.Scenes.Count - 1; i++)
                {
                    canvas.Children.Clear();
                    if (entranceStyle == EntranceStyle.HandDrawn)
                    {
                        canvas.Children.Add(hand);
                        Canvas.SetLeft(hand, 0);
                        Canvas.SetTop(hand, 1150);
                        Canvas.SetZIndex(hand, 1);
                        index = canvas.Children.Count;
                    }
                    SceneModel scene = project.Scenes[i];
                    if (scene == null) continue;
                    for (int j = 0; j < scene.Graphics.Count; j++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        GraphicModelBase graphic = scene.Graphics[j];
                        await Task.Delay((int)graphic.Delay * 1000, cancellationToken);
                        Geometry geometry = null;
                        UIElement element = null;
                        if (graphic is DrawingModel drawing)
                        {
                            DrawingGroup drawingGroup = drawing.ImgDrawingGroup.Clone();
                            drawingGroup.Transform = new ScaleTransform(drawing.ResizeRatio, drawing.ResizeRatio);
                            element = new Image
                            {
                                Source = new DrawingImage(drawingGroup)
                            };
                            if (entranceStyle == EntranceStyle.HandDrawn)
                                geometry = GeometryHelper.ConvertToGeometry(drawingGroup);
                        }
                        else if (graphic is TextModel text)
                        {
                            element = new TextBlock()
                            {
                                Text = text.RawText,
                                Foreground = Brushes.Black,
                                FontFamily = text.SelectedFontFamily,
                                FontSize = text.SelectedFontSize,
                                FontStyle = text.SelectedFontStyle,
                                FontWeight = text.SelectedFontWeight
                            };
                            if (entranceStyle == EntranceStyle.HandDrawn)
                                geometry = text.TextGeometry;
                        }

                        if (entranceStyle == EntranceStyle.HandDrawn && geometry != null)
                        {
                            PathGeometry pathGeometry = geometry.GetFlattenedPathGeometry();
                            List<PathGeometry> pathGeometries = GeometryHelper.GenerateMultiplePaths(pathGeometry, graphic is DrawingModel);
                            List<Path> paths = [];
                            foreach (var geo in pathGeometries)
                            {
                                paths.Add(new Path
                                {
                                    Data = geo,
                                    Stroke = strokeBrush,
                                    StrokeThickness = strokeWidth
                                });
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
                        else if (element != null)
                        {
                            await AnimateElementEntrance(canvas, element, graphic, entranceStyle);
                            index = canvas.Children.Count;
                        }

                        processedGraphics++;
                        if (isExport)
                        {
                            double pct = totalGraphics > 0 ? processedGraphics / (double)totalGraphics * 80 : 80;
                            progress?.Report(new ExportProgressInfo(pct, $"Rendering {processedGraphics} of {totalGraphics}..."));
                        }
                    }
                }
                canvas.Children.Remove(hand);
                await Task.Delay(500);
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
                if (isExport && exporter != null)
                    await exporter.StopCapture(progress, cancellationToken);
            }
        }

        // Non-hand-drawn reveals for graphics that don't need the "drawn by hand" look.
        // Runs in real time (like the hand-drawn path animation) so the frame-capture
        // loop in VideoExporter, which samples the live canvas, records the motion.
        private static Task AnimateElementEntrance(Canvas canvas, UIElement element, GraphicModelBase graphic, EntranceStyle style)
        {
            var tcs = new TaskCompletionSource<bool>();
            Canvas.SetLeft(element, graphic.X);
            Canvas.SetTop(element, graphic.Y);
            canvas.Children.Add(element);

            TimeSpan duration = TimeSpan.FromSeconds(Math.Max(graphic.Duration, 0.1));
            Storyboard storyboard = new();

            if (style == EntranceStyle.PopIn && element is FrameworkElement frameworkElement)
            {
                frameworkElement.RenderTransformOrigin = new Point(0.5, 0.5);
                frameworkElement.RenderTransform = new ScaleTransform(0, 0);

                DoubleAnimation scaleXAnimation = new(0, 1, duration) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut } };
                DoubleAnimation scaleYAnimation = new(0, 1, duration) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut } };
                Storyboard.SetTarget(scaleXAnimation, frameworkElement);
                Storyboard.SetTargetProperty(scaleXAnimation, new PropertyPath("RenderTransform.ScaleX"));
                Storyboard.SetTarget(scaleYAnimation, frameworkElement);
                Storyboard.SetTargetProperty(scaleYAnimation, new PropertyPath("RenderTransform.ScaleY"));
                storyboard.Children.Add(scaleXAnimation);
                storyboard.Children.Add(scaleYAnimation);
            }
            else
            {
                element.Opacity = 0;
                DoubleAnimation opacityAnimation = new(0, 1, duration);
                Storyboard.SetTarget(opacityAnimation, element);
                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(UIElement.OpacityProperty));
                storyboard.Children.Add(opacityAnimation);
            }

            void OnCompleted(object sender, EventArgs e)
            {
                storyboard.Completed -= OnCompleted;
                tcs.TrySetResult(true);
            }
            storyboard.Completed += OnCompleted;
            storyboard.Begin();
            return tcs.Task;
        }
    }
}
