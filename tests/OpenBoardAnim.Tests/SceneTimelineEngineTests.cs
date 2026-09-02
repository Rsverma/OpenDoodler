using OpenBoardAnim.Utils;
using Xunit;

namespace OpenBoardAnim.Tests
{
    public class SceneTimelineEngineTests
    {
        [Theory]
        [InlineData(1, 0.25)]
        [InlineData(2, 0.25)]
        [InlineData(2, 0.5)]
        [InlineData(5, 0.75)]
        public void StrokeDashProgress_MatchesGeometricHandProgress(double strokeThickness, double fraction)
        {
            const double geometryLength = 100;
            double dashLength = SceneTimelineEngine.ToStrokeDashUnits(geometryLength, strokeThickness);
            double remainingDashOffset = dashLength * (1 - fraction);

            // WPF renders dash units multiplied by StrokeThickness. The remaining hidden
            // distance must therefore match the geometric distance still ahead of the hand.
            double remainingGeometryDistance = remainingDashOffset * strokeThickness;

            Assert.Equal(geometryLength * (1 - fraction), remainingGeometryDistance, 6);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ToStrokeDashUnits_InvalidThickness_FallsBackToGeometryUnits(double strokeThickness)
        {
            Assert.Equal(100, SceneTimelineEngine.ToStrokeDashUnits(100, strokeThickness));
        }
    }
}
