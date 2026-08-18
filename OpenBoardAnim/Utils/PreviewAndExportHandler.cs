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
using System.Windows.Threading;

namespace OpenBoardAnim.Utils
{
    public class PreviewAndExportHandler
    {
        public static async Task RunAnimationsOnCanvas(ProjectDetails project, Canvas canvas, bool isExport, IProgress<ExportProgressInfo> progress = null, string outputVideoPath = null, CancellationToken cancellationToken = default)
        {
            VideoExporter exporter = null;
            MediaPlayer voiceoverPlayer = null;
            DispatcherTimer voiceoverTrimTimer = null;
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
                // export doesn't play audio live (frame capture is visual-only). Populated after
                // the loop below (see rawVoiceoverCues) once every scene's start time is known,
                // so each voiceover can be capped to not bleed into the next scene.
                List<SceneAudioCue> sceneAudioCues = new();
                // Keyed by scene loop index rather than a plain sequential list, so a null scene
                // (skipped via `continue` before it gets an entry) can't shift the alignment
                // between this and each raw cue's SceneIndex below.
                Dictionary<int, double> sceneStartTimes = new();
                List<(string Path, double Start, double TrimStart, double TrimEnd, int SceneIndex)> rawVoiceoverCues = new();
                Stopwatch sceneClock = Stopwatch.StartNew();
                if (isExport)
                {
                    exporter = new(canvas, 30, outputVideoPath, project.AudioPath, project.AudioVolume, sceneAudioCues,
                        project.AudioTrimStart, project.AudioTrimEnd);
                    exporter.StartCapture();
                    sceneClock.Restart();
                }
                int totalGraphics = project.Scenes.Sum(s => s.Graphics?.Count(g => g.IsVisible) ?? 0);
                int processedGraphics = 0;
                int index = 1;
                // Excludes the trailing "+" add-scene card either way; PreviewSceneIndex further
                // narrows this to a single scene for an isolated preview (see
                // ProjectDetails.PreviewSceneIndex) instead of always starting from scene 1.
                int startSceneIndex = 0;
                int endSceneIndex = project.Scenes.Count - 2;
                if (project.PreviewSceneIndex is int previewIndex && previewIndex >= 0 && previewIndex <= endSceneIndex)
                {
                    startSceneIndex = previewIndex;
                    endSceneIndex = previewIndex;
                }
                for (int i = startSceneIndex; i <= endSceneIndex; i++)
                {
                    if (i > startSceneIndex && sceneTransition != SceneTransition.None)
                        await PlaySceneTransition(canvas, sceneTransition, cancellationToken);
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
                        double sceneStart = sceneClock.Elapsed.TotalSeconds;
                        sceneStartTimes[i] = sceneStart;
                        if (hasVoiceover)
                            rawVoiceoverCues.Add((scene.VoiceoverPath, sceneStart, scene.VoiceoverTrimStart, scene.VoiceoverTrimEnd, i));
                    }
                    else
                    {
                        voiceoverTrimTimer?.Stop();
                        voiceoverTrimTimer = null;
                        voiceoverPlayer?.Close();
                        voiceoverPlayer = null;
                        if (hasVoiceover)
                        {
                            voiceoverPlayer = new MediaPlayer();
                            voiceoverPlayer.Open(new Uri(scene.VoiceoverPath));
                            voiceoverPlayer.Position = TimeSpan.FromSeconds(Math.Max(0, scene.VoiceoverTrimStart));
                            voiceoverPlayer.Play();
                            if (scene.VoiceoverTrimEnd > scene.VoiceoverTrimStart)
                                voiceoverTrimTimer = StartTrimStopTimer(voiceoverPlayer, scene.VoiceoverTrimEnd);
                        }
                    }

                    for (int j = 0; j < scene.Graphics.Count; j++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        GraphicModelBase graphic = scene.Graphics[j];
                        // A hidden layer contributes nothing to the animation sequence - not
                        // even its own Delay - so the next visible graphic just waits for its
                        // own configured delay as normal, as if the hidden one weren't there.
                        if (!graphic.IsVisible) continue;
                        await Task.Delay((int)graphic.Delay * 1000, cancellationToken);
                        Geometry geometry = null;
                        UIElement element = null;
                        if (graphic is DrawingModel drawing)
                        {
                            DrawingGroup drawingGroup = drawing.ImgDrawingGroup.Clone();
                            // Scale from the drawing's own untransformed bounds to its current
                            // Height/Width - the same values the canvas resize handle edits -
                            // rather than the separately-tracked ResizeRatio, which only reflects
                            // the scale delta of the most recent resize gesture (not the
                            // cumulative scale from the drawing's natural size) once a graphic has
                            // been resized more than once.
                            Rect drawingBounds = drawingGroup.Bounds;
                            double drawingScale = drawingBounds.Width > 0 && drawingBounds.Height > 0
                                ? Math.Min(drawing.Width / drawingBounds.Width, drawing.Height / drawingBounds.Height)
                                : 1;
                            drawingGroup.Transform = new ScaleTransform(drawingScale, drawingScale);
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
                                Foreground = text.SelectedColor,
                                FontFamily = text.SelectedFontFamily,
                                FontSize = text.SelectedFontSize,
                                FontStyle = text.SelectedFontStyle,
                                FontWeight = text.SelectedFontWeight,
                                TextDecorations = text.IsUnderline ? TextDecorations.Underline : null
                            };
                            // Same rationale as the DrawingModel branch above - scale from the
                            // text's natural (unscaled) geometry bounds to its current
                            // Height/Width so a canvas resize is reflected here too, since
                            // TextBlock rendering otherwise has no relationship to those at all.
                            Rect textBounds = text.TextGeometry?.Bounds ?? Rect.Empty;
                            double textScale = !textBounds.IsEmpty && textBounds.Width > 0 && textBounds.Height > 0
                                ? Math.Min(text.Width / textBounds.Width, text.Height / textBounds.Height)
                                : 1;
                            if (textScale != 1)
                                element.RenderTransform = new ScaleTransform(textScale, textScale);
                            if (entranceStyle == EntranceStyle.HandDrawn)
                            {
                                geometry = text.TextGeometry?.Clone();
                                if (geometry != null)
                                    geometry.Transform = new ScaleTransform(textScale, textScale);
                            }
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
                            // PathAnimationHelper isn't cancellation-aware internally (it
                            // completes tcs.Task via a Storyboard callback) - WaitAsync stops
                            // *waiting* as soon as the token fires without needing that, so
                            // Play/Close doesn't have to sit through a whole stroke animation
                            // (previously the biggest reason cancelling only took effect after
                            // roughly a full scene's worth of drawing).
                            await example.tcs.Task.WaitAsync(cancellationToken);

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
                            await AnimateElementEntrance(canvas, element, graphic, entranceStyle, cancellationToken);
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
                await Task.Delay(500, cancellationToken);

                // Cap each voiceover to the following scene's start time so it can't bleed into
                // a scene it doesn't belong to - adelay only controls when a clip starts, not
                // when it stops, so without this a voiceover longer than its own scene (or with
                // no explicit trim end) would keep playing over whatever comes next. The last
                // scene has no following start time to cap against, so it's left uncapped.
                if (isExport)
                {
                    foreach (var raw in rawVoiceoverCues)
                    {
                        double effectiveTrimEnd = raw.TrimEnd;
                        if (sceneStartTimes.TryGetValue(raw.SceneIndex + 1, out double nextSceneStart))
                        {
                            double capEnd = raw.TrimStart + Math.Max(0, nextSceneStart - raw.Start);
                            if (effectiveTrimEnd <= raw.TrimStart || effectiveTrimEnd > capEnd)
                                effectiveTrimEnd = capEnd;
                        }
                        sceneAudioCues.Add(new SceneAudioCue(raw.Path, raw.Start, raw.TrimStart, effectiveTrimEnd));
                    }
                }
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
                voiceoverTrimTimer?.Stop();
                voiceoverPlayer?.Close();
            }
        }

        // Rough per-scene duration estimate (sum of each visible graphic's Delay + Duration) -
        // the same approximation EditorTimelineViewModel already uses for the timeline's
        // proportional layout (hand-drawn stroke timing isn't known ahead of time, so this is
        // "good enough", not a promise). Used by ProjectPreviewView to position/cap the
        // background-music track for a single-scene preview so it lines up roughly where that
        // scene would fall in the full project, without actually having to play through
        // everything before it.
        public static double GetEstimatedSceneDurationSeconds(SceneModel scene)
        {
            if (scene?.Graphics == null) return 0;
            return scene.Graphics.Where(g => g.IsVisible).Sum(g => g.Delay + g.Duration);
        }

        // Live playback (preview) has no equivalent to ffmpeg's -t, so a trimmed clip's end is
        // enforced by polling position and pausing once it's reached. Shared with
        // ProjectPreviewView for the background-music track, which needs the same behavior.
        public static DispatcherTimer StartTrimStopTimer(MediaPlayer player, double trimEndSeconds)
        {
            DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(200) };
            timer.Tick += (s, e) =>
            {
                if (player.Position.TotalSeconds >= trimEndSeconds)
                {
                    player.Pause();
                    timer.Stop();
                }
            };
            timer.Start();
            return timer;
        }

        // Plays a hard-cut alternative between the outgoing (fully-drawn) scene and the
        // incoming (blank) one: lays a plain white rectangle over the existing content and
        // animates it in (fading in, or wiping across) to obscure the old scene, rather than
        // capturing/animating a bitmap snapshot of it - simpler and avoids relying on
        // RenderTargetBitmap producing a usable capture of a canvas that isn't backed by an
        // on-screen HWND during export. Runs in real time so frame-capture records it.
        private static async Task PlaySceneTransition(Canvas canvas, SceneTransition transition, CancellationToken cancellationToken)
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
            await Task.Delay(duration, cancellationToken);

            canvas.Children.Clear();
        }

        // Non-hand-drawn reveals for graphics that don't need the "drawn by hand" look.
        // Runs in real time (like the hand-drawn path animation) so the frame-capture
        // loop in VideoExporter, which samples the live canvas, records the motion.
        private static async Task AnimateElementEntrance(Canvas canvas, UIElement element, GraphicModelBase graphic, EntranceStyle style, CancellationToken cancellationToken)
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
            await Task.Delay(duration, cancellationToken);
        }
    }
}
