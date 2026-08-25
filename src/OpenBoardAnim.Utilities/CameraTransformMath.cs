using System;

namespace OpenBoardAnim.Utilities
{
    // The RenderTransform values (uniform scale + translate) needed to center a given focus
    // point in a viewport at a given zoom level - see CameraTransformMath.ComputeTransform.
    public readonly record struct CameraTransform(double Scale, double TranslateX, double TranslateY);

    // How a camera pan/zoom move's progress is remapped before interpolating - see
    // CameraTransformMath.ApplyEasing. Lives here (not in OpenBoardAnim.Models alongside
    // CameraEffectModel) because it needs to be usable from this no-WPF-types math, and
    // OpenBoardAnim.Utilities has no project reference back to the WPF app to pull a type from
    // there instead.
    public enum CameraEasing
    {
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut
    }

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

        // Remaps a linear 0-1 progress fraction onto the given easing curve before it's used to
        // interpolate scale/translate. Deliberately hand-written quadratic formulas (not WPF's
        // QuadraticEase.Ease) so this stays usable from export's WPF-free per-frame evaluation -
        // they're the exact same curves WPF's QuadraticEase produces for each EasingMode
        // (verified against its EaseInCore(t) = t*t definition), so PreviewPlaybackHandler can
        // use an actual QuadraticEase with the matching EasingMode and the two stay pixel-identical.
        public static double ApplyEasing(double fraction, CameraEasing easing)
        {
            double t = Math.Clamp(fraction, 0, 1);
            return easing switch
            {
                CameraEasing.EaseIn => t * t,
                CameraEasing.EaseOut => 1 - (1 - t) * (1 - t),
                CameraEasing.EaseInOut => t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2,
                _ => t
            };
        }
    }
}
