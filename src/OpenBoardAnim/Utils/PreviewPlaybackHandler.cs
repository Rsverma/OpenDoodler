using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OpenBoardAnim.Utils
{
    // Live in-app preview playback (ProjectPreviewView). Split out from ExportRenderHandler
    // deliberately - both used to live in one isExport-branching method, but that made every
    // export-only timing change (e.g. moving export to a deterministic, non-realtime frame
    // clock) a risk to preview's real-time playback too. The two are now free to diverge:
    // this file owns real-time Storyboard/BeginAnimation playback and PathAnimationHelper: it
    // should stay exactly this way even if ExportRenderHandler's timing model changes.
    public class PreviewPlaybackHandler
    {
        public static async Task PlayAsync(ProjectDetails project, Canvas canvas, CancellationToken cancellationToken = default)
        {
            MediaPlayer voiceoverPlayer = null;
            DispatcherTimer voiceoverTrimTimer = null;
            try
            {
                if (project == null) return;
                // A zoomed-in camera effect would otherwise render past the canvas's own bounds
                // in the live preview (Canvas doesn't clip by default).
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

                // Null for HandStyle.None (or a Custom style with no/missing file) - hand stays a
                // sourceless, never-added Image in that case (see the HandDrawn branch below),
                // harmlessly passed through to PathAnimationHelper regardless since it only ever
                // moves/transforms it.
                Image hand = new() { Source = SceneRenderHelpers.ResolveHandImage(handStyle, project.Settings?.CustomHandImagePath) };
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
                // Clamped to a small positive minimum - a zero/negative duration would make
                // the crossfade/wipe DoubleAnimation below meaningless (or throw).
                double transitionDurationSeconds = Math.Max(0.05, project.Settings?.TransitionDurationSeconds ?? 0.6);

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
                        // below via PathAnimationHelper) but skips showing the cursor image itself.
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
                            voiceoverTrimTimer = SceneRenderHelpers.StartTrimStopTimer(voiceoverPlayer, scene.VoiceoverTrimEnd);
                    }

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
                            var example = new PathAnimationHelper(canvas, paths, graphic, hand);
                            example.AnimatePathOnCanvas();
                            // PathAnimationHelper isn't cancellation-aware internally (it
                            // completes tcs.Task via a Storyboard callback) - WaitAsync stops
                            // *waiting* as soon as the token fires without needing that, so
                            // Play/Close doesn't have to sit through a whole stroke animation.
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
                voiceoverTrimTimer?.Stop();
                voiceoverPlayer?.Close();
            }
        }

        // Plays a hard-cut alternative between the outgoing (fully-drawn) scene and the
        // incoming (blank) one: lays a plain white rectangle over the existing content and
        // animates it in (fading in, or wiping across) to obscure the old scene, rather than
        // capturing/animating a bitmap snapshot of it.
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
            // see PlaySceneTransition for why that event isn't trustworthy here either.
            storyboard.Begin();
            await Task.Delay(duration, cancellationToken);
        }

        // Plays a scene's camera-effects layer (SceneModel.CameraEffects), sorted by StartTime
        // (absolute seconds from the scene's start - not list/add order), concurrently with the
        // scene's graphic entrance animations - see the call site in PlayAsync. Before the first
        // effect's StartTime, between effects, and after the last one's EndTime, the camera
        // simply holds wherever it last landed, since nothing touches scale/translate during
        // those gaps.
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

                // Animate scale/translate directly on the Transform objects (Transform
                // implements IAnimatable) rather than via a Storyboard.SetTarget(...)+Begin() -
                // these two Transforms are standalone Freezables (children of a TransformGroup,
                // never a named/rooted element), and a fresh Storyboard re-targeting them on
                // every effect in this loop was not reliably driving their values; direct
                // BeginAnimation is the standard technique for animating a detached Freezable
                // and needs no target/property-path resolution at all. Each call's explicit
                // From (start.Scale/TranslateX/Y) makes an earlier "snap to start" assignment
                // unnecessary - BeginAnimation establishes that starting value itself.
                IEasingFunction easingFunction = ToEasingFunction(effect.Easing);
                DoubleAnimation scaleXAnimation = new(start.Scale, end.Scale, duration) { FillBehavior = FillBehavior.HoldEnd, EasingFunction = easingFunction };
                DoubleAnimation scaleYAnimation = new(start.Scale, end.Scale, duration) { FillBehavior = FillBehavior.HoldEnd, EasingFunction = easingFunction };
                DoubleAnimation translateXAnimation = new(start.TranslateX, end.TranslateX, duration) { FillBehavior = FillBehavior.HoldEnd, EasingFunction = easingFunction };
                DoubleAnimation translateYAnimation = new(start.TranslateY, end.TranslateY, duration) { FillBehavior = FillBehavior.HoldEnd, EasingFunction = easingFunction };

                scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
                translate.BeginAnimation(TranslateTransform.XProperty, translateXAnimation);
                translate.BeginAnimation(TranslateTransform.YProperty, translateYAnimation);

                await Task.Delay(duration, cancellationToken);
                elapsed = effect.EndTime;
            }
        }

        // QuadraticEase's per-EasingMode curve is exactly the formula ExportRenderHandler's
        // CameraTransformMath.ApplyEasing hand-computes for the same CameraEasing value (verified
        // against QuadraticEase.EaseInCore(t) = t*t) - using it here keeps preview and export
        // pixel-identical without either side depending on the other's animation system. Null for
        // Linear, WPF's own DoubleAnimation default.
        private static IEasingFunction ToEasingFunction(CameraEasing easing) => easing switch
        {
            CameraEasing.EaseIn => new QuadraticEase { EasingMode = EasingMode.EaseIn },
            CameraEasing.EaseOut => new QuadraticEase { EasingMode = EasingMode.EaseOut },
            CameraEasing.EaseInOut => new QuadraticEase { EasingMode = EasingMode.EaseInOut },
            _ => null
        };
    }
}
