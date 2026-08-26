namespace OpenBoardAnim.Models
{
    // One camera effect's position/width in pixels, local to its scene's own timeline segment -
    // view-only, computed by EditorTimelineViewModel.RecomputeSegments, never persisted.
    public class CameraEffectTimelineBlock
    {
        public double X { get; set; }
        public double Width { get; set; }
        public CameraEffectModel Effect { get; set; }

        public string ToolTipText => Effect == null ? "" : $"{Effect.StartZoom:0.0}x → {Effect.EndZoom:0.0}x, {Effect.StartTime:0.0}s–{Effect.EndTime:0.0}s";
    }
}
