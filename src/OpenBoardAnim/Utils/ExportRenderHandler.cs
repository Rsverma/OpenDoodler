using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    // Video export capture. Split from PreviewPlaybackHandler deliberately - both used to live
    // in one isExport-branching method, but that made every preview-only tweak a risk to export
    // and vice versa. This file owns export's animation-timing code (currently identical in
    // spirit to preview's - real-time Storyboards played out while VideoExporter samples the
    // canvas) and is free to move to a deterministic, non-realtime frame clock later without
    // PreviewPlaybackHandler ever needing to change.
    public class ExportRenderHandler
    {
        public static async Task ExportAsync(ProjectDetails project, Canvas canvas, IProgress<ExportProgressInfo> progress = null, string outputVideoPath = null, CancellationToken cancellationToken = default)
        {
            VideoExporter exporter = null;
            try
            {
                if (project == null) return;
                canvas.ClipToBounds = true;
                double cameraViewportWidth = project.Settings?.EditorWidth ?? 0;
                double cameraViewportHeight = project.Settings?.EditorHeight ?? 0;
                EntranceStyle entranceStyle = project.Settings?.EntranceStyle ?? EntranceStyle.HandDrawn;
                HandStyle handStyle = project.Settings?.HandStyle ?? HandStyle.LightSkin;
                SceneTransition sceneTransition = project.Settings?.SceneTransition ?? SceneTransition.None;
                Brush strokeBrush = Brushes.Black;
                try
                {
                    if (!string.IsNullOrWhiteSpace(project.Settings?.StrokeColorHex))
                        strokeBrush = (Brush)new BrushConverter().ConvertFromString(project.Settings.StrokeColorHex);
                }
                catch (FormatException) { /* keep default black on an unparsable hex value */ }
                double strokeWidth = project.Settings != null && project.Settings.StrokeWidth > 0 ? project.Settings.StrokeWidth : 1;

                // ThumbnailUri is null for HandStyle.None - hand stays a sourceless, never-added
                // Image in that case (see the HandDrawn branch below), harmlessly passed through
                // to ExportPathAnimationHelper regardless since it only ever moves/transforms it.
                string handImageUri = HandStyleOptions.All.FirstOrDefault(o => o.Style == handStyle)?.ThumbnailUri;
                Image hand = new();
                if (handImageUri != null)
                    hand.Source = new BitmapImage(new Uri(handImageUri));
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
                const int exportFrameRate = 30;
                int index = 1;
                // Excludes the trailing "+" add-scene card either way; PreviewSceneIndex further
                // narrows this to a single scene for an isolated preview instead of always
                // starting from scene 1.
                int startSceneIndex = 0;
                int endSceneIndex = project.Scenes.Count - 2;
                if (project.PreviewSceneIndex is int previewIndex && previewIndex >= 0 && previewIndex <= endSceneIndex)
                {
                    startSceneIndex = previewIndex;
                    endSceneIndex = previewIndex;
                }
                // Clamped to a small positive minimum - a zero/negative duration would make
                // the crossfade/wipe DoubleAnimation below meaningless (or throw).
                double transitionDurationSeconds = Math.Max(0.05, project.Settings?.TransitionDurationSeconds ?? 0.6);

                // Frame count (rather than scene or graphic count) is what actually tracks
                // linearly with real capture progress - a single hand-drawn stroke scene can
                // take far longer to render than several static ones combined, so counting
                // scenes/graphics made the bar jump in uneven lurches. GetEstimatedSceneDurationSeconds
                // is the same rough per-scene estimate the timeline already uses; scene
                // transitions and the trailing 0.5s hold are accounted for too so the estimate
                // roughly matches the real capture length.
                double estimatedSeconds = 0;
                for (int s = startSceneIndex; s <= endSceneIndex; s++)
                    estimatedSeconds += SceneRenderHelpers.GetEstimatedSceneDurationSeconds(project.Scenes[s]);
                for (int s = startSceneIndex; s < endSceneIndex; s++)
                    if (SceneRenderHelpers.GetEffectiveTransition(project.Scenes[s], sceneTransition) != SceneTransition.None)
                        estimatedSeconds += transitionDurationSeconds;
                estimatedSeconds += 0.5;
                int estimatedTotalFrames = Math.Max(1, (int)Math.Round(estimatedSeconds * exportFrameRate));

                exporter = new(canvas, exportFrameRate, outputVideoPath, project.AudioPath, project.AudioVolume, sceneAudioCues,
                    project.AudioTrimStart, project.AudioTrimEnd, progress, estimatedTotalFrames);
                exporter.StartCapture();
                sceneClock.Restart();

                for (int i = startSceneIndex; i <= endSceneIndex; i++)
                {
                    SceneTransition boundaryTransition = i > startSceneIndex
                        ? SceneRenderHelpers.GetEffectiveTransition(project.Scenes[i - 1], sceneTransition)
                        : SceneTransition.None;
                    if (boundaryTransition != SceneTransition.None)
                        await PlaySceneTransition(canvas, boundaryTransition, transitionDurationSeconds, cancellationToken);
                    else
                        canvas.Children.Clear();

                    // RenderTransform is a canvas-level property that Children.Clear() doesn't
                    // touch - reset it every scene (camera effects or not) so a previous scene's
                    // pan/zoom end-state can't bleed into this one.
                    ScaleTransform cameraScale = new(1, 1);
                    TranslateTransform cameraTranslate = new(0, 0);
                    canvas.RenderTransform = new TransformGroup { Children = { cameraScale, cameraTranslate } };

                    if (entranceStyle == EntranceStyle.HandDrawn)
                    {
                        // HandStyle.None keeps the stroke-by-stroke draw animation (still driven
                        // below via ExportPathAnimationHelper) but skips showing the cursor image itself.
                        if (handStyle != HandStyle.None)
                        {
                            canvas.Children.Add(hand);
                            Canvas.SetLeft(hand, 0);
                            Canvas.SetTop(hand, 1150);
                            Canvas.SetZIndex(hand, 1);
                        }
                        index = canvas.Children.Count;
                    }
                    SceneModel scene = project.Scenes[i];
                    if (scene == null) continue;

                    bool hasVoiceover = !string.IsNullOrWhiteSpace(scene.VoiceoverPath) && System.IO.File.Exists(scene.VoiceoverPath);
                    double sceneStart = sceneClock.Elapsed.TotalSeconds;
                    sceneStartTimes[i] = sceneStart;
                    if (hasVoiceover)
                        rawVoiceoverCues.Add((scene.VoiceoverPath, sceneStart, scene.VoiceoverTrimStart, scene.VoiceoverTrimEnd, i));

                    async Task PlayGraphicsAsync()
                    {
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
                            element = SceneRenderHelpers.BuildTextBlock(text);
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
                            var example = new ExportPathAnimationHelper(canvas, paths, graphic, hand);
                            example.AnimatePathOnCanvas();
                            // ExportPathAnimationHelper isn't cancellation-aware internally (it
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

                    }
                    }

                    Task graphicsTask = PlayGraphicsAsync();
                    Task cameraTask = PlayCameraEffectsAsync(scene, cameraScale, cameraTranslate, cameraViewportWidth, cameraViewportHeight, cancellationToken);
                    await Task.WhenAll(graphicsTask, cameraTask);
                }
                canvas.Children.Remove(hand);
                await Task.Delay(500, cancellationToken);

                // Cap each voiceover to the following scene's start time so it can't bleed into
                // a scene it doesn't belong to - adelay only controls when a clip starts, not
                // when it stops, so without this a voiceover longer than its own scene (or with
                // no explicit trim end) would keep playing over whatever comes next. The last
                // scene has no following start time to cap against, so it's left uncapped.
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
                if (exporter != null)
                    await exporter.StopCapture(progress, cancellationToken);
            }
        }

        // Plays a hard-cut alternative between the outgoing (fully-drawn) scene and the
        // incoming (blank) one: lays a plain white rectangle over the existing content and
        // animates it in (fading in, or wiping across) to obscure the old scene, rather than
        // capturing/animating a bitmap snapshot of it - simpler and avoids relying on
        // RenderTargetBitmap producing a usable capture of a canvas that isn't backed by an
        // on-screen HWND during export. Runs in real time so frame-capture records it.
        private static async Task PlaySceneTransition(Canvas canvas, SceneTransition transition, double durationSeconds, CancellationToken cancellationToken)
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

            TimeSpan duration = TimeSpan.FromSeconds(durationSeconds);
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
        // Runs in real time (like the hand-drawn path animation) so the frame-capture loop in
        // VideoExporter, which samples the live canvas, records the motion.
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

        // Plays a scene's camera-effects layer (SceneModel.CameraEffects), sorted by StartTime
        // (absolute seconds from the scene's start - not list/add order), concurrently with the
        // scene's graphic entrance animations - see the call site in ExportAsync. Before the
        // first effect's StartTime, between effects, and after the last one's EndTime, the
        // camera simply holds wherever it last landed, since nothing touches scale/translate
        // during those gaps. Runs in real time, awaited via Task.Delay rather than
        // Storyboard.Completed, for the same export-capture-safety reason as PlaySceneTransition
        // and AnimateElementEntrance above.
        private static async Task PlayCameraEffectsAsync(SceneModel scene, ScaleTransform scale, TranslateTransform translate, double viewportWidth, double viewportHeight, CancellationToken cancellationToken)
        {
            if (scene?.CameraEffects == null || viewportWidth <= 0 || viewportHeight <= 0) return;

            double elapsed = 0;
            foreach (CameraEffectModel effect in scene.CameraEffects.OrderBy(e => e.StartTime))
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(0, effect.StartTime - elapsed)), cancellationToken);
                elapsed = effect.StartTime;

                CameraTransform start = CameraTransformMath.ComputeTransform(effect.StartFocusX, effect.StartFocusY, effect.StartZoom, viewportWidth, viewportHeight);
                CameraTransform end = CameraTransformMath.ComputeTransform(effect.EndFocusX, effect.EndFocusY, effect.EndZoom, viewportWidth, viewportHeight);

                TimeSpan duration = TimeSpan.FromSeconds(Math.Max(effect.EndTime - effect.StartTime, 0.01));

                DoubleAnimation scaleXAnimation = new(start.Scale, end.Scale, duration) { FillBehavior = FillBehavior.HoldEnd };
                DoubleAnimation scaleYAnimation = new(start.Scale, end.Scale, duration) { FillBehavior = FillBehavior.HoldEnd };
                DoubleAnimation translateXAnimation = new(start.TranslateX, end.TranslateX, duration) { FillBehavior = FillBehavior.HoldEnd };
                DoubleAnimation translateYAnimation = new(start.TranslateY, end.TranslateY, duration) { FillBehavior = FillBehavior.HoldEnd };

                scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
                translate.BeginAnimation(TranslateTransform.XProperty, translateXAnimation);
                translate.BeginAnimation(TranslateTransform.YProperty, translateYAnimation);

                await Task.Delay(duration, cancellationToken);
                elapsed = effect.EndTime;
            }
        }
    }
}
