using System;

namespace TimeLineTool
{
    public interface ITimeLineManager
    {
        Boolean CanAddToTimeLine(ITimeLineDataItem item);
    }
}