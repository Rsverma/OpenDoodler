using System;

namespace OpenBoardAnim.TimeLineTool
{
    public class TempDataType : ITimeLineDataItem
    {
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool TimelineViewExpanded { get; set; }
        public string Name { get; set; }
    }
}