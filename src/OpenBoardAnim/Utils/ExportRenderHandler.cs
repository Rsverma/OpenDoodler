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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OpenBoardAnim.Utils
{
    // Video export capture. Split from PreviewPlaybackHandler deliberately - both used to live
    // in one isExport-branching method, but that made every preview-only tweak a risk to export
    // and vice versa.
    //
    // Unlike preview (which plays real-time WPF Storyboards while VideoExporter samples whatever
    // is on screen whenever CompositionTarget.Rendering fires), export is fully deterministic and
    // does NOT use WPF's Storyboard/Clock system at all: an earlier version drove a paused,
    // controllable Storyboard via Seek() per frame, but Seek doesn't reliably flush into the
    // property system before an off-screen RenderTargetBitmap.Render() reads it (it depends on
    // WPF's own composition tick, which nothing here was actually forcing) - every captured frame
    // in a scene came out identical, frozen on the scene's first frame. Instead, every property
    // this needs (stroke reveal, hand position, camera pan/zoom, entrance opacity/scale) is
    // computed analytically for the exact virtual time of each frame and set directly - no clock,
    // nothing to defer, so there's nothing that can silently not-yet-apply. Two public WPF APIs
    // keep this exact rather than an approximation of the original animations: PathGeometry.
    // GetPointAtFractionLength (the same primitive MatrixAnimationUsingPath used internally) for
    // hand tracking, and IEasingFunction.Ease(double) so BackEase's curve isn't reimplemented.
    public class ExportRenderHandler
    {
        private const double EndHoldSeconds = 0.5;
        private static readonly IEasingFunction PopInEase = new BackEase { EasingMode = EasingMode.EaseOut };

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

                string handImageUri = HandStyleOptions.All.FirstOrDefault(o => o.Style == handStyle)?.ThumbnailUri;
                Image hand = new();
                if (handImageUri != null)
                    hand.Source = new BitmapImage(new Uri(handImageUri));

                List<SceneAudioCue> sceneAudioCues = new();
                Dictionary<int, double> sceneStartTimes = new();
                List<(string Path, double Start, double TrimStart, double TrimEnd, int SceneIndex)> rawVoiceoverCues = new();
                double virtualClock = 0;
                const int exportFrameRate = 30;

                int startSceneIndex = 0;
                int endSceneIndex = project.Scenes.Count - 2;
                if (project.PreviewSceneIndex is int previewIndex && previewIndex >= 0 && previewIndex <= endSceneIndex)
                {
                    startSceneIndex = previewIndex;
                    endSceneIndex = previewIndex;
                }
                double transitionDurationSeconds = Math.Max(0.05, project.Settings?.TransitionDurationSeconds ?? 0.6);

                double estimatedSeconds = 0;
                for (int s = startSceneIndex; s <= endSceneIndex; s++)
                    estimatedSeconds += SceneRenderHelpers.GetEstimatedSceneDurationSeconds(project.Scenes[s]);
                for (int s = startSceneIndex; s < endSceneIndex; s++)
                    if (SceneRenderHelpers.GetEffectiveTransition(project.Scenes[s], sceneTransition) != SceneTransition.None)
                        estimatedSeconds += transitionDurationSeconds;
                estimatedSeconds += EndHoldSeconds;
                int estimatedTotalFrames = Math.Max(1, (int)Math.Round(estimatedSeconds * exportFrameRate));

                exporter = new(canvas, exportFrameRate, outputVideoPath, project.AudioPath, project.AudioVolume, sceneAudioCues,
                    project.AudioTrimStart, project.AudioTrimEnd, progress, estimatedTotalFrames);
                exporter.StartCapture();

                for (int i = startSceneIndex; i <= endSceneIndex; i++)
                {
                    SceneTransition boundaryTransition = i > startSceneIndex
                        ? SceneRenderHelpers.GetEffectiveTransition(project.Scenes[i - 1], sceneTransition)
                        : SceneTransition.None;
                    if (boundaryTransition != SceneTransition.None)
                    {
                        await RunTransitionAsync(canvas, boundaryTransition, transitionDurationSeconds, exporter, exportFrameRate, cancellationToken);
                        virtualClock += transitionDurationSeconds;
                    }
                    else
                    {
                        canvas.Children.Clear();
                    }

                    ScaleTransform cameraScale = new(1, 1);
                    TranslateTransform cameraTranslate = new(0, 0);
                    canvas.RenderTransform = new TransformGroup { Children = { cameraScale, cameraTranslate } };

                    bool showHand = entranceStyle == EntranceStyle.HandDrawn && handStyle != HandStyle.None;
                    if (showHand)
                    {
                        canvas.Children.Add(hand);
                        Canvas.SetLeft(hand, 0);
                        Canvas.SetTop(hand, 1150);
                        Canvas.SetZIndex(hand, 1);
                        hand.RenderTransform = new MatrixTransform();
                    }

                    SceneModel scene = project.Scenes[i];
                    if (scene == null) continue;

                    bool hasVoiceover = !string.IsNullOrWhiteSpace(scene.VoiceoverPath) && System.IO.File.Exists(scene.VoiceoverPath);
                    sceneStartTimes[i] = virtualClock;
                    if (hasVoiceover)
                        rawVoiceoverCues.Add((scene.VoiceoverPath, virtualClock, scene.VoiceoverTrimStart, scene.VoiceoverTrimEnd, i));

                    double sceneDuration = await RunSceneAsync(canvas, scene, entranceStyle, hand, showHand, strokeBrush, strokeWidth,
                        cameraScale, cameraTranslate, cameraViewportWidth, cameraViewportHeight, exporter, exportFrameRate, cancellationToken);
                    virtualClock += sceneDuration;
                }
                canvas.Children.Remove(hand);

                int holdFrameCount = Math.Max(1, (int)Math.Round(EndHoldSeconds * exportFrameRate));
                for (int f = 0; f < holdFrameCount; f++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    exporter.CaptureFrame();
                    await YieldToUiAsync(canvas);
                }

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

        // Plays a hard-cut alternative between the outgoing (fully-drawn) scene and the incoming
        // (blank) one: a plain white rectangle whose Opacity (crossfade) or Width (wipe) is
        // computed directly per frame, linearly, from 0 to full over durationSeconds.
        private static async Task RunTransitionAsync(Canvas canvas, SceneTransition transition, double durationSeconds, VideoExporter exporter, int frameRate, CancellationToken cancellationToken)
        {
            if (canvas.Children.Count == 0)
                return;

            Rectangle overlay = new()
            {
                Fill = Brushes.White,
                Width = transition == SceneTransition.Wipe ? 0 : canvas.Width,
                Height = canvas.Height,
                Opacity = transition == SceneTransition.Wipe ? 1 : 0
            };
            Canvas.SetLeft(overlay, 0);
            Canvas.SetTop(overlay, 0);
            Canvas.SetZIndex(overlay, 1000);
            canvas.Children.Add(overlay);

            int frameCount = Math.Max(1, (int)Math.Round(durationSeconds * frameRate));
            for (int f = 0; f < frameCount; f++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                double fraction = durationSeconds > 0 ? Math.Clamp(f / (double)frameRate / durationSeconds, 0, 1) : 1;
                if (transition == SceneTransition.Wipe)
                    overlay.Width = fraction * canvas.Width;
                else
                    overlay.Opacity = fraction;

                canvas.UpdateLayout();
                exporter.CaptureFrame();
                await YieldToUiAsync(canvas);
            }

            canvas.Children.Clear();
        }

        // Precomputed, timing-independent facts about one visible graphic - built once per scene,
        // then evaluated at every frame's virtual scene-elapsed time by ApplyGraphicState.
        private sealed class GraphicPlan
        {
            public double Start;
            public double Duration;
            public bool IsHandDrawn;
            public double X;
            public double Y;
            public UIElement SettledElement;
            public List<PathPlan> Segments;
            public EntranceStyle EntranceStyle;
        }

        private sealed class PathPlan
        {
            public Path Element;
            public PathGeometry Geometry;
            public double LocalStart;
            public double LocalDuration;
            public double Length;
        }

        private sealed class CameraPlan
        {
            public double Start;
            public double Duration;
            public double StartScale;
            public double EndScale;
            public double StartTranslateX;
            public double EndTranslateX;
            public double StartTranslateY;
            public double EndTranslateY;
        }

        // Builds one scene's full visual timeline (every graphic's stroke/entrance windows, every
        // camera effect's window) and steps through it frame by frame, evaluating and applying
        // state directly at each step rather than through any WPF animation clock. Returns the
        // scene's real content duration (the same value SceneRenderHelpers.
        // GetEstimatedSceneDurationSeconds approximates from outside).
        private static async Task<double> RunSceneAsync(Canvas canvas, SceneModel scene, EntranceStyle entranceStyle, Image hand, bool showHand,
            Brush strokeBrush, double strokeWidth, ScaleTransform cameraScale, TranslateTransform cameraTranslate,
            double cameraViewportWidth, double cameraViewportHeight, VideoExporter exporter, int frameRate, CancellationToken cancellationToken)
        {
            List<GraphicPlan> graphicPlans = new();
            double cursor = 0;
            foreach (GraphicModelBase graphic in scene.Graphics.Where(g => g.IsVisible))
            {
                double start = cursor + graphic.Delay;
                Geometry geometry = null;
                UIElement element = null;

                if (graphic is DrawingModel drawing)
                {
                    DrawingGroup drawingGroup = drawing.ImgDrawingGroup.Clone();
                    Rect drawingBounds = drawingGroup.Bounds;
                    double drawingScale = drawingBounds.Width > 0 && drawingBounds.Height > 0
                        ? Math.Min(drawing.Width / drawingBounds.Width, drawing.Height / drawingBounds.Height)
                        : 1;
                    drawingGroup.Transform = new ScaleTransform(drawingScale, drawingScale);
                    element = new Image { Source = new DrawingImage(drawingGroup) };
                    if (entranceStyle == EntranceStyle.HandDrawn)
                        geometry = GeometryHelper.ConvertToGeometry(drawingGroup);
                }
                else if (graphic is TextModel text)
                {
                    element = SceneRenderHelpers.BuildTextBlock(text);
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

                double graphicDuration;
                GraphicPlan plan = new() { Start = start, X = graphic.X, Y = graphic.Y, EntranceStyle = entranceStyle };

                if (entranceStyle == EntranceStyle.HandDrawn && geometry != null)
                {
                    graphicDuration = Math.Max(graphic.Duration, 0);
                    plan.IsHandDrawn = true;
                    plan.Duration = graphicDuration;
                    plan.Segments = new List<PathPlan>();

                    PathGeometry pathGeometry = geometry.GetFlattenedPathGeometry();
                    List<PathGeometry> pathGeometries = GeometryHelper.GenerateMultiplePaths(pathGeometry, graphic is DrawingModel);
                    List<double> lengths = pathGeometries.Select(GetTotalLength).ToList();
                    double totalLength = lengths.Sum();
                    double localCursor = 0;
                    for (int i = 0; i < pathGeometries.Count; i++)
                    {
                        Path pathElement = new() { Stroke = strokeBrush, StrokeThickness = strokeWidth, Data = pathGeometries[i] };
                        // A single-value StrokeDashArray alternates [dash=length, gap=length] -
                        // StrokeDashOffset=length shifts the pattern so the whole path starts in
                        // the gap (hidden); offset=0 puts the whole path in the dash (fully
                        // drawn). Without StrokeDashArray set at all, StrokeDashOffset has no
                        // effect and the stroke always renders solid regardless.
                        pathElement.StrokeDashArray = new DoubleCollection(new double[] { lengths[i] });
                        pathElement.StrokeDashOffset = lengths[i];
                        canvas.Children.Add(pathElement);
                        Canvas.SetLeft(pathElement, graphic.X);
                        Canvas.SetTop(pathElement, graphic.Y);

                        double ratio = totalLength > 0 ? lengths[i] / totalLength : 0;
                        double segDuration = graphicDuration * ratio;
                        plan.Segments.Add(new PathPlan
                        {
                            Element = pathElement,
                            Geometry = pathGeometries[i],
                            LocalStart = localCursor,
                            LocalDuration = segDuration,
                            Length = lengths[i]
                        });
                        localCursor += segDuration;
                    }

                    if (element != null)
                    {
                        element.Opacity = 0;
                        canvas.Children.Add(element);
                        Canvas.SetLeft(element, graphic.X);
                        Canvas.SetTop(element, graphic.Y);
                        plan.SettledElement = element;
                    }
                }
                else if (element != null)
                {
                    graphicDuration = Math.Max(graphic.Duration, 0.1);
                    plan.IsHandDrawn = false;
                    plan.Duration = graphicDuration;
                    plan.SettledElement = element;
                    if (entranceStyle == EntranceStyle.PopIn && element is FrameworkElement frameworkElement)
                    {
                        frameworkElement.RenderTransformOrigin = new Point(0.5, 0.5);
                        frameworkElement.RenderTransform = new ScaleTransform(0, 0);
                    }
                    else
                    {
                        element.Opacity = 0;
                    }
                    Canvas.SetLeft(element, graphic.X);
                    Canvas.SetTop(element, graphic.Y);
                    canvas.Children.Add(element);
                }
                else
                {
                    graphicDuration = Math.Max(graphic.Duration, 0.1);
                    plan.Duration = graphicDuration;
                }

                graphicPlans.Add(plan);
                cursor = start + graphicDuration;
            }
            double graphicsTotal = cursor;

            List<CameraPlan> cameraPlans = new();
            double cameraTotal = 0;
            if (scene.CameraEffects != null && cameraViewportWidth > 0 && cameraViewportHeight > 0)
            {
                foreach (CameraEffectModel effect in scene.CameraEffects.OrderBy(e => e.StartTime))
                {
                    CameraTransform start = CameraTransformMath.ComputeTransform(effect.StartFocusX, effect.StartFocusY, effect.StartZoom, cameraViewportWidth, cameraViewportHeight);
                    CameraTransform end = CameraTransformMath.ComputeTransform(effect.EndFocusX, effect.EndFocusY, effect.EndZoom, cameraViewportWidth, cameraViewportHeight);
                    cameraPlans.Add(new CameraPlan
                    {
                        Start = effect.StartTime,
                        Duration = Math.Max(effect.EndTime - effect.StartTime, 0.01),
                        StartScale = start.Scale,
                        EndScale = end.Scale,
                        StartTranslateX = start.TranslateX,
                        EndTranslateX = end.TranslateX,
                        StartTranslateY = start.TranslateY,
                        EndTranslateY = end.TranslateY
                    });
                    cameraTotal = Math.Max(cameraTotal, effect.EndTime);
                }
            }

            double sceneDuration = Math.Max(graphicsTotal, cameraTotal);
            int frameCount = Math.Max(1, (int)Math.Round(sceneDuration * frameRate));
            for (int f = 0; f < frameCount; f++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                double t = f / (double)frameRate;

                foreach (GraphicPlan plan in graphicPlans)
                    ApplyGraphicState(plan, t, showHand ? hand : null);
                ApplyCameraState(cameraPlans, cameraScale, cameraTranslate, t);

                canvas.UpdateLayout();
                exporter.CaptureFrame();
                await YieldToUiAsync(canvas);
            }

            return sceneDuration;
        }

        // Task.Yield()'s continuation resumes at DispatcherPriority.Normal under WPF's dispatcher
        // - since Normal outranks Render, a tight loop that re-posts itself every frame at that
        // priority starves the window's own repaint (and IProgress<T> reports, also posted to the
        // dispatcher) for as long as the loop keeps running: the queue never drains down to
        // Render/lower while Normal-priority work keeps arriving right behind it. Yielding at
        // Background - below both Render and Normal - guarantees a paint pass and any pending
        // progress report get serviced every single frame instead of only once capture finishes.
        private static Task YieldToUiAsync(Canvas canvas)
        {
            return canvas.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background).Task;
        }

        // Evaluates one graphic's state at scene-elapsed time t and applies it directly - stroke
        // dash-offset reveal + hand tracking for HandDrawn, or opacity/scale for a fade/pop-in
        // entrance. Graphics before t settle to their final look; graphics after t stay hidden;
        // clamping the local-progress fraction to [0, 1] handles both automatically without
        // needing separate before/after branches.
        private static void ApplyGraphicState(GraphicPlan plan, double t, Image hand)
        {
            double local = Math.Clamp(t - plan.Start, 0, plan.Duration);
            bool started = t >= plan.Start;
            bool finished = t >= plan.Start + plan.Duration;

            if (plan.IsHandDrawn)
            {
                if (plan.SettledElement != null)
                    plan.SettledElement.Opacity = finished ? 1 : 0;

                foreach (PathPlan seg in plan.Segments)
                {
                    double segLocal = Math.Clamp(local - seg.LocalStart, 0, seg.LocalDuration);
                    double fraction = seg.LocalDuration > 0 ? segLocal / seg.LocalDuration : (local >= seg.LocalStart ? 1 : 0);
                    seg.Element.StrokeDashOffset = seg.Length * (1 - fraction);
                    seg.Element.Opacity = finished ? 0 : 1;

                    // started (not just local >= seg.LocalStart) matters here: for a graphic that
                    // hasn't started yet, local is clamped to 0, which trivially satisfies
                    // `local >= seg.LocalStart` for that graphic's own first segment (LocalStart
                    // 0) too - without the started guard, every not-yet-started graphic later in
                    // the list would also touch the hand's matrix each frame and, being processed
                    // after the actually-active graphic, stomp its position back to a fixed point.
                    if (hand != null && started && local >= seg.LocalStart)
                    {
                        seg.Geometry.GetPointAtFractionLength(fraction, out Point point, out _);
                        ((MatrixTransform)hand.RenderTransform).Matrix = new Matrix(1, 0, 0, 1, point.X, point.Y);
                    }
                }

                if (hand != null)
                {
                    if (finished)
                    {
                        Canvas.SetLeft(hand, 2000);
                        Canvas.SetTop(hand, 1100);
                    }
                    else if (started)
                    {
                        Canvas.SetLeft(hand, plan.X);
                        Canvas.SetTop(hand, plan.Y);
                    }
                }
            }
            else if (plan.SettledElement != null)
            {
                double fraction = plan.Duration > 0 ? local / plan.Duration : (started ? 1 : 0);
                if (plan.EntranceStyle == EntranceStyle.PopIn && plan.SettledElement is FrameworkElement frameworkElement
                    && frameworkElement.RenderTransform is ScaleTransform scaleTransform)
                {
                    double eased = fraction <= 0 ? 0 : fraction >= 1 ? 1 : PopInEase.Ease(fraction);
                    scaleTransform.ScaleX = eased;
                    scaleTransform.ScaleY = eased;
                }
                else
                {
                    plan.SettledElement.Opacity = fraction;
                }
            }
        }

        // Evaluates the camera transform at scene-elapsed time t: holds at (1, 0, 0) before the
        // first effect, linearly interpolates within whichever effect's window contains t, and
        // holds at that effect's end value in the gap before the next one starts.
        private static void ApplyCameraState(List<CameraPlan> plans, ScaleTransform scale, TranslateTransform translate, double t)
        {
            CameraPlan active = null;
            foreach (CameraPlan plan in plans)
            {
                if (t >= plan.Start)
                    active = plan;
                else
                    break;
            }
            if (active == null)
            {
                scale.ScaleX = 1;
                scale.ScaleY = 1;
                translate.X = 0;
                translate.Y = 0;
                return;
            }

            double fraction = active.Duration > 0 ? Math.Clamp((t - active.Start) / active.Duration, 0, 1) : 1;
            double currentScale = Lerp(active.StartScale, active.EndScale, fraction);
            scale.ScaleX = currentScale;
            scale.ScaleY = currentScale;
            translate.X = Lerp(active.StartTranslateX, active.EndTranslateX, fraction);
            translate.Y = Lerp(active.StartTranslateY, active.EndTranslateY, fraction);
        }

        private static double Lerp(double from, double to, double fraction) => from + (to - from) * fraction;

        private static double GetTotalLength(PathGeometry geometry)
        {
            double length = 0;
            foreach (PathFigure figure in geometry.Figures)
            {
                Point start = figure.StartPoint;
                foreach (PathSegment segment in figure.Segments)
                {
                    if (segment is LineSegment line)
                    {
                        length += (line.Point - start).Length;
                        start = line.Point;
                    }
                    else if (segment is PolyLineSegment polyLine)
                    {
                        foreach (Point point in polyLine.Points)
                        {
                            length += (point - start).Length;
                            start = point;
                        }
                    }
                }
            }
            return length;
        }
    }
}
