using OpenBoardAnim.Utilities;
using Xunit;

namespace OpenBoardAnim.Tests
{
    public class CameraTransformMathTests
    {
        [Fact]
        public void ComputeTransform_FocusAtViewportCenter_ZoomOne_IsIdentity()
        {
            CameraTransform result = CameraTransformMath.ComputeTransform(480, 270, 1.0, 960, 540);

            Assert.Equal(1.0, result.Scale, 6);
            Assert.Equal(0, result.TranslateX, 6);
            Assert.Equal(0, result.TranslateY, 6);
        }

        [Fact]
        public void ComputeTransform_OffCenterFocus_RecentersOnViewportMiddle()
        {
            // Focus point (100, 50) at zoom 1 should map to the viewport's center (480, 270).
            CameraTransform result = CameraTransformMath.ComputeTransform(100, 50, 1.0, 960, 540);

            double mappedX = 100 * result.Scale + result.TranslateX;
            double mappedY = 50 * result.Scale + result.TranslateY;
            Assert.Equal(480, mappedX, 6);
            Assert.Equal(270, mappedY, 6);
        }

        [Theory]
        [InlineData(0.5)]
        [InlineData(1.0)]
        [InlineData(2.5)]
        public void ComputeTransform_AlwaysMapsFocusPointToViewportCenter(double zoom)
        {
            CameraTransform result = CameraTransformMath.ComputeTransform(300, 200, zoom, 960, 540);

            double mappedX = 300 * result.Scale + result.TranslateX;
            double mappedY = 200 * result.Scale + result.TranslateY;
            Assert.Equal(480, mappedX, 6);
            Assert.Equal(270, mappedY, 6);
            Assert.Equal(zoom, result.Scale, 6);
        }
    }
}
