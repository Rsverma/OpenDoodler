using OpenBoardAnim.Utilities;
using Xunit;

namespace OpenBoardAnim.Tests
{
    public class ExportProgressMathTests
    {
        [Fact]
        public void ShouldCaptureFrame_ReturnsFalse_WhenWithinFrameWindow()
        {
            // 30fps -> ~0.0333s per frame; 0.02s since the last capture isn't enough yet.
            Assert.False(ExportProgressMath.ShouldCaptureFrame(1.02, 1.00, 30));
        }

        [Fact]
        public void ShouldCaptureFrame_ReturnsTrue_WhenElapsedMeetsThreshold()
        {
            Assert.True(ExportProgressMath.ShouldCaptureFrame(1.0333334, 1.00, 30));
        }

        [Fact]
        public void ShouldCaptureFrame_ReturnsTrue_OnExactThreshold()
        {
            Assert.True(ExportProgressMath.ShouldCaptureFrame(1.0 + 1.0 / 30, 1.0, 30));
        }

        [Theory]
        [InlineData(0, 100, 0)]
        [InlineData(50, 100, 35)]
        [InlineData(100, 100, 70)]
        public void CapturePercentage_ScalesLinearlyUpToEstimate(int frameCount, int estimatedTotalFrames, double expected)
        {
            Assert.Equal(expected, ExportProgressMath.CapturePercentage(frameCount, estimatedTotalFrames), 3);
        }

        [Fact]
        public void CapturePercentage_CapsAt70_WhenActualOverrunsEstimate()
        {
            double pct = ExportProgressMath.CapturePercentage(500, 100);
            Assert.Equal(70, pct, 3);
        }

        [Theory]
        [InlineData(0, 100, 70)]
        [InlineData(50, 100, 75)]
        [InlineData(100, 100, 80)]
        public void DrainPercentage_ScalesLinearlyBetween70And80(int framesWritten, int frameCount, double expected)
        {
            Assert.Equal(expected, ExportProgressMath.DrainPercentage(framesWritten, frameCount), 3);
        }

        [Fact]
        public void DrainPercentage_CapsAt80()
        {
            Assert.Equal(80, ExportProgressMath.DrainPercentage(1000, 100), 3);
        }

        [Theory]
        [InlineData(0, 10, 80)]
        [InlineData(5, 10, 90)]
        [InlineData(10, 10, 99)]
        [InlineData(20, 10, 99)]
        public void EncodePercentage_ScalesBetween80And99_AndClamps(double elapsedSeconds, double videoDurationSeconds, double expected)
        {
            Assert.Equal(expected, ExportProgressMath.EncodePercentage(elapsedSeconds, videoDurationSeconds), 3);
        }

        [Fact]
        public void EncodePercentage_ReturnsEightyFive_WhenDurationIsZero()
        {
            Assert.Equal(85, ExportProgressMath.EncodePercentage(1.0, 0));
        }

        [Fact]
        public void BuildTrimArgs_ReturnsEmpty_WhenBothZero()
        {
            Assert.Equal("", ExportProgressMath.BuildTrimArgs(0, 0));
        }

        [Fact]
        public void BuildTrimArgs_IncludesStart_WhenStartPositive()
        {
            Assert.Equal("-ss 2.5 ", ExportProgressMath.BuildTrimArgs(2.5, 0));
        }

        [Fact]
        public void BuildTrimArgs_IncludesStartAndDuration_WhenEndPastStart()
        {
            Assert.Equal("-ss 1 -t 4 ", ExportProgressMath.BuildTrimArgs(1, 5));
        }

        [Fact]
        public void BuildTrimArgs_OmitsEnd_WhenEndNotPastStart()
        {
            Assert.Equal("-ss 3 ", ExportProgressMath.BuildTrimArgs(3, 3));
        }

        [Fact]
        public void BuildFrameDurations_UsesGapToNextFrameTimestamp()
        {
            List<double> timestamps = [0.0, 0.1, 0.25];
            List<FrameDurationEntry> entries = ExportProgressMath.BuildFrameDurations(timestamps, 0.30);

            Assert.Equal(3, entries.Count);
            Assert.Equal("frame_0000.bmp", entries[0].FrameName);
            Assert.Equal(0.1, entries[0].Duration, 6);
            Assert.Equal(0.15, entries[1].Duration, 6);
        }

        [Fact]
        public void BuildFrameDurations_LastFrameUsesTotalElapsedMinusItsOwnTimestamp()
        {
            List<double> timestamps = [0.0, 0.2];
            List<FrameDurationEntry> entries = ExportProgressMath.BuildFrameDurations(timestamps, 0.35);

            Assert.Equal(0.15, entries[1].Duration, 6);
        }

        [Fact]
        public void BuildFrameDurations_LastFrameDurationNeverBelowMinimum()
        {
            // totalElapsedSeconds before the last frame's own timestamp (defensive edge case).
            List<double> timestamps = [0.0, 0.5];
            List<FrameDurationEntry> entries = ExportProgressMath.BuildFrameDurations(timestamps, 0.1);

            Assert.Equal(0.001, entries[1].Duration, 6);
        }

        [Fact]
        public void BuildFrameDurations_EmptyInput_ReturnsEmpty()
        {
            Assert.Empty(ExportProgressMath.BuildFrameDurations([], 0));
        }

        [Theory]
        [InlineData(1, "frame_0000.bmp")]
        [InlineData(5, "frame_0004.bmp")]
        [InlineData(1234, "frame_1233.bmp")]
        public void LastFrameName_FormatsWithFourDigits(int frameCount, string expected)
        {
            Assert.Equal(expected, ExportProgressMath.LastFrameName(frameCount));
        }
    }
}
