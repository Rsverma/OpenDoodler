using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace OpenBoardAnim.Utils
{
    public class GeometryWithFill
    {
        public Geometry Geometry { get; set; }

        public GeometryWithFill(Geometry geometry, Brush brush)
        {
            Geometry = geometry;
            Brush = brush;
        }

        public Brush Brush { get; set; }
    }

    public class GeometryHelper
    {
        public static PathGeometry ConvertTextToGeometry(string text, FontFamily fontFamily, FontStyle fontStyle, FontWeight fontWeight, double fontSize)
        {
            Typeface typeface = new Typeface(fontFamily, fontStyle, fontWeight, FontStretches.Normal);
            // Create a formatted text
            FormattedText formattedText = new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSize,
                Brushes.Black,
                VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip);

            // Create a geometry from the formatted text
            Geometry textGeometry = formattedText.BuildGeometry(new Point(0, 0));

            // Convert to PathGeometry
            PathGeometry pathGeometry = PathGeometry.CreateFromGeometry(textGeometry);

            return pathGeometry;
        }

        public static DrawingGroup GetPathGeometryFromSVG(string filePath)
        {
            if(string.IsNullOrEmpty(filePath)) return null;
            var svgFileReader = new FileSvgReader(new WpfDrawingSettings());
            return svgFileReader.Read(filePath);
        }

        public static List<GeometryWithFill> ConvertToGeometry(DrawingGroup drawingGroup, TransformGroup group)
        {
            List<GeometryWithFill> geometrylist = new List<GeometryWithFill>();
            if (drawingGroup != null)
            {
                foreach (var drawing in drawingGroup.Children)
                { 
                    TransformGroup clone = group.Clone();
                    if (drawing is GeometryDrawing geometryDrawing)
                    {
                        Geometry geometry = geometryDrawing.Geometry;
                        if (geometry.Transform != null)
                        {
                            clone.Children.Insert(0,geometry.Transform);
                        }
                        geometry.Transform = clone;
                        geometrylist.Add(new GeometryWithFill(geometry,geometryDrawing.Brush));
                    }
                    else if (drawing is DrawingGroup innerGroup)
                    {
                        if (innerGroup.Transform != null)
                            clone.Children.Insert(0, innerGroup.Transform);
                        geometrylist.AddRange(ConvertToGeometry(innerGroup, clone));
                    }
                }
            }

            return geometrylist;
        }

    }
}
