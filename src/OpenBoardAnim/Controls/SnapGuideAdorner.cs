using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace OpenBoardAnim.Controls
{
    // Draws the canvas-drag snap guide lines directly on top of AdornedElement (the item-hosting
    // Canvas in EditorCanvasView). An Adorner's OnRender coordinate space is, by WPF design,
    // exactly AdornedElement's own local coordinate space - the same one graphics' X/Y already
    // live in - so this can't drift out of alignment the way a second, independently-sized
    // overlay Canvas did (two separately-measured/arranged elements landed on slightly
    // different rounded pixel sizes under WPF's layout rounding, most visible far from the
    // top-left origin).
    public class SnapGuideAdorner : Adorner
    {
        private static readonly Pen GuidePen = CreateGuidePen();

        public bool ShowX { get; private set; }
        public double GuideX { get; private set; }
        public bool ShowY { get; private set; }
        public double GuideY { get; private set; }

        public SnapGuideAdorner(UIElement adornedElement) : base(adornedElement)
        {
            IsHitTestVisible = false;
            // A 1px line centered on a non-integer device-pixel coordinate anti-aliases into a
            // soft ~2px band that visually reads as sitting to one side rather than a crisp
            // line on it - disabling anti-aliasing here snaps the stroke to the device pixel
            // grid instead.
            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
        }

        public void UpdateGuides(bool showX, double guideX, bool showY, double guideY)
        {
            ShowX = showX;
            GuideX = guideX;
            ShowY = showY;
            GuideY = guideY;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Size size = AdornedElement.RenderSize;
            if (ShowX)
                drawingContext.DrawLine(GuidePen, new Point(GuideX, 0), new Point(GuideX, size.Height));
            if (ShowY)
                drawingContext.DrawLine(GuidePen, new Point(0, GuideY), new Point(size.Width, GuideY));
        }

        private static Pen CreateGuidePen()
        {
            SolidColorBrush brush = new(Color.FromRgb(0x21, 0x96, 0xF3));
            brush.Freeze();
            Pen pen = new(brush, 1);
            pen.Freeze();
            return pen;
        }
    }
}
