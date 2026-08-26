namespace OpenBoardAnim.Models
{
    // One scene-to-scene transition marker, filling the gap between two adjacent scene cards
    // in the graphics track - view-only, computed by EditorTimelineViewModel.RecomputeSegments,
    // never persisted. Only created for a boundary whose effective transition (scene override,
    // or the project default if the scene inherits) isn't None. Width is proportional to
    // ProjectSettings.TransitionDurationSeconds at the current zoom, the same way a scene
    // card's own width stands in for its estimated duration.
    public class SceneTransitionTimelineBlock
    {
        public double X { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public bool IsOverride { get; set; }
        public string ToolTipText { get; set; }
    }
}
