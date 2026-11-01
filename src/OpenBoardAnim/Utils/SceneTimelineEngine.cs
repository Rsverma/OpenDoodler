using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace OpenBoardAnim.Utils
{
    // Precomputed, timing-independent facts about one visible graphic - built once per scene by
    // SceneTimelineEngine.BuildScenePlan, then evaluated at any scene-elapsed time t by
    // SceneTimelineEngine.ApplyGraphicState. Exposed (not nested/private) so both
    // ExportRenderHandler and PreviewPlaybackHandler can hold onto plans built for them.
    public sealed class GraphicPlan
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

    public sealed class PathPlan
    {
        public Path Element;
        public PathGeometry Geometry;
        public double LocalStart;
        public double LocalDuration;
        // WPF dash units (multiples of the pen's thickness), not geometry units - see
        // SceneTimelineEngine.ToStrokeDashUnits.
        public double DashLength;
    }

    public sealed class CameraPlan
    {
        public double Start;
        public double Duration;
        public double StartScale;
        public double EndScale;
        public double StartTranslateX;
        public double EndTranslateX;
        public double StartTranslateY;
        public double EndTranslateY;
        public CameraEasing Easing;
    }

    // One scene's full built timeline: every graphic's stroke/entrance window (elements already
    // added to the canvas, hidden until ApplyGraphicState reveals them) and every camera effect's
    // window, plus the scene's real content duration (the same value SceneRenderHelpers.
    // GetEstimatedSceneDurationSeconds only approximates from outside).
    public sealed class SceneTimelinePlan
    {
        public List<GraphicPlan> GraphicPlans;
        public List<CameraPlan> CameraPlans;
        public double Duration;
    }

    // The "what does this scene look like at time t" math shared by ExportRenderHandler (which
    // steps through it frame-by-frame for capture) and PreviewPlaybackHandler (which steps
    // through it either continuously during Play or on-demand for a scrub) - both need to answer
    // the exact same question, so this is the one place the answer is computed rather than two
    // copies that could drift apart. No WPF Storyboard/Clock involved: every property is set
    // directly from t, so the same t always produces the exact same visual state.
    public static class SceneTimelineEngine
    {
        private static readonly IEasingFunction PopInEase = new BackEase { EasingMode = EasingMode.EaseOut };

        // Seconds over which a finished HandDrawn graphic cross-fades from its revealed outline
        // to the settled artwork. Without it the swap is a single-frame jump: the outline is the
        // project stroke brush at the graphic's derived width, while the settled element is the
        // SVG's own filled artwork, so the graphic visibly changes weight and colour all at once.
        // Kept short because the fade runs inside the graphic's own duration (see
        // ApplyGraphicState) - it doesn't extend the scene, but a long window would start to read
        // as the fill leaking in ahead of the pen.
        private const double HandDrawnSettleFadeSeconds = 0.2;

        // Builds one scene's graphic and camera plans, adding every graphic's visual elements to
        // canvas.Children (hidden - ApplyGraphicState reveals them at the right time) as a side
        // effect. Callers own clearing the canvas and resetting camera transforms beforehand.
        public static SceneTimelinePlan BuildScenePlan(Canvas canvas, SceneModel scene, EntranceStyle entranceStyle,
            Brush strokeBrush, double cameraViewportWidth, double cameraViewportHeight)
        {
            List<GraphicPlan> graphicPlans = new();
            double cursor = 0;
            foreach (GraphicModelBase graphic in scene.Graphics.Where(g => g.IsVisible))
            {
                double start = cursor + graphic.Delay;
                Geometry geometry = null;
                UIElement element = null;
                // How far this graphic is scaled away from the artwork's natural size. Drives the
                // hand-drawn outline's thickness below, so the traced line stays visually
                // proportional to the art instead of being pinned to one project-wide width.
                double graphicScale = 1;

                if (graphic is DrawingModel drawing)
                {
                    DrawingGroup drawingGroup = drawing.ImgDrawingGroup.Clone();
                    Rect drawingBounds = drawingGroup.Bounds;
                    double drawingScale = drawingBounds.Width > 0 && drawingBounds.Height > 0
                        ? Math.Min(drawing.Width / drawingBounds.Width, drawing.Height / drawingBounds.Height)
                        : 1;
                    drawingGroup.Transform = new ScaleTransform(drawingScale, drawingScale);
                    graphicScale = drawingScale;
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
                    graphicScale = textScale;
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
                    // One outline weight for the whole graphic, derived from its scale: a graphic
                    // drawn at 3x gets a 3px pen, the same visual weight per unit of art. Guarded
                    // against a degenerate scale so a graphic never renders with a zero-width pen.
                    double outlineWidth = graphicScale > 0 ? graphicScale : 1;
                    double totalLength = lengths.Sum();
                    double localCursor = 0;
                    for (int i = 0; i < pathGeometries.Count; i++)
                    {
                        Path pathElement = new() { Stroke = strokeBrush, StrokeThickness = outlineWidth, Data = pathGeometries[i] };
                        // StrokeDashArray/StrokeDashOffset are measured in multiples of the pen's
                        // thickness, while GetPointAtFractionLength (hand tracking) is geometric.
                        // Normalizing the length into those units is what keeps the stroke tip and
                        // the hand covering the same distance per unit of progress.
                        double dashLength = ToStrokeDashUnits(lengths[i], outlineWidth);
                        pathElement.StrokeDashArray = new DoubleCollection(new double[] { dashLength });
                        pathElement.StrokeDashOffset = dashLength;
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
                            DashLength = dashLength
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
                        EndTranslateY = end.TranslateY,
                        Easing = effect.Easing
                    });
                    cameraTotal = Math.Max(cameraTotal, effect.EndTime);
                }
            }

            return new SceneTimelinePlan
            {
                GraphicPlans = graphicPlans,
                CameraPlans = cameraPlans,
                Duration = Math.Max(graphicsTotal, cameraTotal)
            };
        }

        // Evaluates one graphic's state at scene-elapsed time t and applies it directly - stroke
        // dash-offset reveal + hand tracking for HandDrawn, or opacity/scale for a fade/pop-in
        // entrance. Graphics before t settle to their final look; graphics after t stay hidden;
        // clamping the local-progress fraction to [0, 1] handles both automatically without
        // needing separate before/after branches. A HandDrawn graphic additionally cross-fades
        // outline -> settled artwork over its last moments rather than swapping in one frame.
        public static void ApplyGraphicState(GraphicPlan plan, double t, Image hand)
        {
            double local = Math.Clamp(t - plan.Start, 0, plan.Duration);
            bool started = t >= plan.Start;
            bool finished = t >= plan.Start + plan.Duration;

            if (plan.IsHandDrawn)
            {
                // Cross-fade from outline to settled artwork over the tail of the graphic's own
                // duration (see HandDrawnSettleFadeSeconds). Both layers are driven by the same
                // alpha, inverted, so the outline is fully gone exactly when the artwork is fully
                // in - no pop at either end of the window, which is what the previous hard swap at
                // `finished` produced. Overlapping two partly-transparent layers does dip the
                // combined coverage slightly mid-fade; at this length that's imperceptible, and
                // it's the price of having no visible seam. The window is clamped to the graphic's
                // duration so a very short graphic can't fade for longer than it draws. A settled
                // element is only ever non-null when there was artwork to settle to, so a graphic
                // whose settled element is missing keeps its outline fully visible instead of
                // fading to nothing.
                double settledOpacity;
                if (plan.SettledElement == null)
                {
                    settledOpacity = 0;
                }
                else if (plan.Duration > 0)
                {
                    double fadeSeconds = Math.Min(HandDrawnSettleFadeSeconds, plan.Duration);
                    double fadeStart = plan.Duration - fadeSeconds;
                    settledOpacity = fadeSeconds > 0 ? Math.Clamp((local - fadeStart) / fadeSeconds, 0, 1) : 1;
                }
                else
                {
                    settledOpacity = started ? 1 : 0;
                }

                if (plan.SettledElement != null)
                    plan.SettledElement.Opacity = settledOpacity;

                foreach (PathPlan seg in plan.Segments)
                {
                    double segLocal = Math.Clamp(local - seg.LocalStart, 0, seg.LocalDuration);
                    double fraction = seg.LocalDuration > 0 ? segLocal / seg.LocalDuration : (local >= seg.LocalStart ? 1 : 0);
                    seg.Element.StrokeDashOffset = seg.DashLength * (1 - fraction);
                    seg.Element.Opacity = plan.SettledElement != null ? 1 - settledOpacity : 1;

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
        // first effect, interpolates (through that effect's easing curve) within whichever
        // effect's window contains t, and holds at that effect's end value in the gap before the
        // next one starts.
        public static void ApplyCameraState(List<CameraPlan> plans, ScaleTransform scale, TranslateTransform translate, double t)
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
            double eased = CameraTransformMath.ApplyEasing(fraction, active.Easing);
            double currentScale = Lerp(active.StartScale, active.EndScale, eased);
            scale.ScaleX = currentScale;
            scale.ScaleY = currentScale;
            translate.X = Lerp(active.StartTranslateX, active.EndTranslateX, eased);
            translate.Y = Lerp(active.StartTranslateY, active.EndTranslateY, eased);
        }

        private static double Lerp(double from, double to, double fraction) => from + (to - from) * fraction;

        // Converts a geometry length into the units WPF's dash properties expect (multiples of the
        // pen's thickness). Falls back to raw geometry units for a non-positive thickness, which
        // WPF also treats as "no dash scaling".
        internal static double ToStrokeDashUnits(double geometryLength, double strokeThickness)
        {
            return strokeThickness > 0 ? geometryLength / strokeThickness : geometryLength;
        }

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
