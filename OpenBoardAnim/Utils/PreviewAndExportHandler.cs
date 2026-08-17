using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            MediaPlayer voiceoverPlayer = null;
            try
            {
                if (project == null) return;
                EntranceStyle entranceStyle = project.Settings?.EntranceStyle ?? EntranceStyle.HandDrawn;
                SceneTransition sceneTransition = project.Settings?.SceneTransition ?? SceneTransition.None;
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
                // Cues collected as scenes start, in real (wall-clock) time - handed to the
                // exporter so it can delay each voiceover clip into place when muxing, since
                // export doesn't play audio live (frame capture is visual-only).
                List<SceneAudioCue> sceneAudioCues = new();
                Stopwatch sceneClock = Stopwatch.StartNew();
                if (isExport)
                {
                    exporter = new(canvas, 30, outputVideoPath, project.AudioPath, project.AudioVolume, sceneAudioCues);
                    exporter.StartCapture();
                    sceneClock.Restart();
                }
                int totalGraphics = project.Scenes.Sum(s => s.Graphics?.Count ?? 0);
                int processedGraphics = 0;
                int index = 1;
                for (int i = 0; i < project.Scenes.Count - 1; i++)
                {
                    if (i > 0 && sceneTransition != SceneTransition.None)
                        await PlaySceneTransition(canvas, sceneTransition);
                    else
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

                    bool hasVoiceover = !string.IsNullOrWhiteSpace(scene.VoiceoverPath) && System.IO.File.Exists(scene.VoiceoverPath);
                    if (isExport)
                    {
                        if (hasVoiceover)
                            sceneAudioCues.Add(new SceneAudioCue(scene.VoiceoverPath, sceneClock.Elapsed.TotalSeconds));
                    }
                    else
                    {
                        voiceoverPlayer?.Close();
                        voiceoverPlayer = null;
                        if (hasVoiceover)
                        {
                            voiceoverPlayer = new MediaPlayer();
                            voiceoverPlayer.Open(new Uri(scene.VoiceoverPath));
                            voiceoverPlayer.Play();
                        }
                    }

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
                voiceoverPlayer?.Close();
            }
        }

        // Plays a hard-cut alternative between the outgoing (fully-drawn) scene and the
        // incoming (blank) one: lays a plain white rectangle over the existing content and
        // animates it in (fading in, or wiping across) to obscure the old scene, rather than
        // capturing/animating a bitmap snapshot of it - simpler and avoids relying on
        // RenderTargetBitmap producing a usable capture of a canvas that isn't backed by an
        // on-screen HWND during export. Runs in real time so frame-capture records it.
        private static async Task PlaySceneTransition(Canvas canvas, SceneTransition transition)
        {
            if (canvas.Children.Count == 0)
                return;

            Rectangle overlay = new()
            {
                Fill = Brushes.White,
                Width = canvas.Width,
                Height = canvas.Height
            };
            Canvas.SetLeft(overlay, 0);
            Canvas.SetTop(overlay, 0);
            Canvas.SetZIndex(overlay, 1000);
            canvas.Children.Add(overlay);

            TimeSpan duration = TimeSpan.FromSeconds(0.6);
            Storyboard storyboard = new();

            if (transition == SceneTransition.Wipe)
            {
                overlay.Width = 0;
                DoubleAnimation widthAnimation = new(0, canvas.Width, duration) { FillBehavior = FillBehavior.HoldEnd };
                Storyboard.SetTarget(widthAnimation, overlay);
                Storyboard.SetTargetProperty(widthAnimation, new PropertyPath(FrameworkElement.WidthProperty));
                storyboard.Children.Add(widthAnimation);
            }
            else
            {
                overlay.Opacity = 0;
                DoubleAnimation opacityAnimation = new(0, 1, duration) { FillBehavior = FillBehavior.HoldEnd };
                Storyboard.SetTarget(opacityAnimation, overlay);
                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(UIElement.OpacityProperty));
                storyboard.Children.Add(opacityAnimation);
            }

            storyboard.Begin();
            await Task.Delay(duration);

            canvas.Children.Clear();
        }

        // Non-hand-drawn reveals for graphics that don't need the "drawn by hand" look.
        // Runs in real time (like the hand-drawn path animation) so the frame-capture
        // loop in VideoExporter, which samples the live canvas, records the motion.
        private static async Task AnimateElementEntrance(Canvas canvas, UIElement element, GraphicModelBase graphic, EntranceStyle style)
        {
            Canvas.SetLeft(element, graphic.X);
            Canvas.SetTop(element, graphic.Y);
            canvas.Children.Add(element);

            TimeSpan duration = TimeSpan.FromSeconds(Math.Max(graphic.Duration, 0.1));
            Storyboard storyboard = new();

            if (style == EntranceStyle.PopIn && element is FrameworkElement frameworkElement)
            {
                frameworkElement.RenderTransformOrigin = new Point(0.5, 0.5);
                frameworkElement.RenderTransform = new ScaleTransform(0, 0);

                DoubleAnimation scaleXAnimation = new(0, 1, duration) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.HoldEnd };
                DoubleAnimation scaleYAnimation = new(0, 1, duration) { EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.HoldEnd };
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
                DoubleAnimation opacityAnimation = new(0, 1, duration) { FillBehavior = FillBehavior.HoldEnd };
                Storyboard.SetTarget(opacityAnimation, element);
                Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(UIElement.OpacityProperty));
                storyboard.Children.Add(opacityAnimation);
            }

            // Wait out the real duration directly rather than relying on Storyboard.Completed -
            // see PlaySceneTransition for why that event isn't trustworthy here.
            storyboard.Begin();
            await Task.Delay(duration);
        }
    }
}
