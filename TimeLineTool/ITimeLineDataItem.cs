﻿using System;

namespace TimeLineTool
{
    public interface ITimeLineDataItem
    {
        DateTime? StartTime { get; set; }
        DateTime? EndTime { get; set; }
        bool TimelineViewExpanded { get; set; }
    }
}