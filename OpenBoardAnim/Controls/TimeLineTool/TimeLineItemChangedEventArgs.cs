using System;

namespace OpenBoardAnim.TimeLineTool
{
    internal class TimeLineItemChangedEventArgs : EventArgs
    {
        public TimeLineManipulationMode Mode { get; set; }
        public TimeLineAction Action { get; set; }
        public TimeSpan DeltaTime { get; set; }
        public double DeltaX { get; set; }
    }
}