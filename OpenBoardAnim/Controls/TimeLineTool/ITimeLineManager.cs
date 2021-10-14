using System;

namespace OpenBoardAnim.TimeLineTool
{
    public interface ITimeLineManager
    {
        Boolean CanAddToTimeLine(ITimeLineDataItem item);
    }
}