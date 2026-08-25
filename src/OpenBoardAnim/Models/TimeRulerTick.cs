namespace OpenBoardAnim.Models
{
    // One labeled tick mark on the timeline's time ruler - view-only, computed by
    // EditorTimelineViewModel.RecomputeTimeRuler, never persisted. X is pixels from the
    // timeline's own origin, on the same scale as SceneTimelineSegment.X.
    public class TimeRulerTick
    {
        public double X { get; set; }
        public string Label { get; set; }
    }
}
