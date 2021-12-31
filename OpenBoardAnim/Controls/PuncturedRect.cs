using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace OpenBoardAnim.Controls
{
    /// <summary>
    /// PuncturedRectangle class.
    /// </summary>
    public class PuncturedRect : Shape
	{
		#region Dependency properties

		public static readonly DependencyProperty InteriorProperty =
			DependencyProperty.Register("Interior", typeof(Rect), typeof(FrameworkElement),
				new FrameworkPropertyMetadata(new Rect(0, 0, 0, 0), FrameworkPropertyMetadataOptions.AffectsRender, null, CoerceRectInterior, false), null);

		private static object CoerceRectInterior(DependencyObject d, object value)
		{
			PuncturedRect pr = (PuncturedRect)d;
			Rect rcExterior = pr.Exterior;
			Rect rcProposed = (Rect)value;

		    if (rcExterior.Width <= 0 || rcExterior.Height <= 0)
		        return rcExterior;

			double left = Math.Max(rcProposed.Left, rcExterior.Left);
			double top = Math.Max(rcProposed.Top, rcExterior.Top);
			double width = Math.Min(rcProposed.Right, rcExterior.Right) - left;
			double height = Math.Min(rcProposed.Bottom, rcExterior.Bottom) - top;

			return new Rect(left, top, width, height);
		}

		public Rect Interior
		{
			get { return (Rect)GetValue(InteriorProperty); }
			set { SetValue(InteriorProperty, value); }
		}

        public static readonly DependencyProperty ExteriorProperty =
			DependencyProperty.Register("Exterior", typeof(Rect), typeof(FrameworkElement),
				new FrameworkPropertyMetadata(new Rect(0, 0, double.MaxValue, double.MaxValue),
					FrameworkPropertyMetadataOptions.AffectsMeasure |
					FrameworkPropertyMetadataOptions.AffectsArrange |
					FrameworkPropertyMetadataOptions.AffectsParentMeasure |
					FrameworkPropertyMetadataOptions.AffectsParentArrange |
					FrameworkPropertyMetadataOptions.AffectsRender,
					null, null, false), null);

		public Rect Exterior
		{
			get { return (Rect)GetValue(ExteriorProperty); }
			set { SetValue(ExteriorProperty, value); }
		}

		#endregion

		#region Override

		protected override Geometry DefiningGeometry
		{
			get
			{
				var pthfExt = new PathFigure {StartPoint = Exterior.TopLeft};
			    pthfExt.Segments.Add(new LineSegment(Exterior.TopRight, false));
				pthfExt.Segments.Add(new LineSegment(Exterior.BottomRight, false));
				pthfExt.Segments.Add(new LineSegment(Exterior.BottomLeft, false));
				pthfExt.Segments.Add(new LineSegment(Exterior.TopLeft, false));

                var pthgExt = new PathGeometry();
                pthgExt.Figures.Add(pthfExt);

				var rectIntSect = Rect.Intersect(Exterior, Interior);
				
			    var pthfInt = new PathFigure {StartPoint = rectIntSect.TopLeft};
			    pthfInt.Segments.Add(new LineSegment(rectIntSect.TopRight, false));
				pthfInt.Segments.Add(new LineSegment(rectIntSect.BottomRight, false));
				pthfInt.Segments.Add(new LineSegment(rectIntSect.BottomLeft, false));
				pthfInt.Segments.Add(new LineSegment(rectIntSect.TopLeft, false));

                var pthgInt = new PathGeometry();
                pthgInt.Figures.Add(pthfInt);
                
                CombinedGeometry cmbg = new CombinedGeometry(GeometryCombineMode.Exclude, pthgExt, pthgInt);
                return cmbg;
			}
		}

		#endregion
	}
}
