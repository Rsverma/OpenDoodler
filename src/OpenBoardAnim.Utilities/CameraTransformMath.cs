using System;

namespace OpenBoardAnim.Utilities
{
    // The RenderTransform values (uniform scale + translate) needed to center a given focus
    // point in a viewport at a given zoom level - see CameraTransformMath.ComputeTransform.
    public readonly record struct CameraTransform(double Scale, double TranslateX, double TranslateY);

    // Pure camera pan/zoom math pulled out into its own testable class, mirroring
    // ExportProgressMath - no WPF types, so it can be unit tested without a live Canvas.
    public static class CameraTransformMath
    {
        // screenPoint = editorPoint * scale + translate, solved so (focusX, focusY) maps to the
        // viewport's center at the given zoom.
        public static CameraTransform ComputeTransform(double focusX, double focusY, double zoom, double viewportWidth, double viewportHeight)
        {
            double translateX = viewportWidth / 2.0 - focusX * zoom;
            double translateY = viewportHeight / 2.0 - focusY * zoom;
            return new CameraTransform(zoom, translateX, translateY);
        }
    }
}
