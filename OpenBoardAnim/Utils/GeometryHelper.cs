using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace OpenBoardAnim.Utils
{
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

        public static DrawingGroup GetPathGeometryFromSVG(string svgText)
        {
            if (string.IsNullOrEmpty(svgText)) return null;
            var svgFileReader = new FileSvgReader(new WpfDrawingSettings());
            using (TextReader sr = new StringReader(svgText))
            {
                DrawingGroup drawingGroup = svgFileReader.Read(sr);
                return drawingGroup;
            }
        }

        public static Geometry ConvertToGeometry(DrawingGroup drawingGroup)
        {
            var geometryGroup = new GeometryGroup();

            foreach (var drawing in drawingGroup.Children)
            {
                if (drawing is GeometryDrawing geometryDrawing && geometryDrawing.Geometry != null)
                {
                    geometryGroup.Children.Add(geometryDrawing.Geometry);
                }
                else if (drawing is DrawingGroup innerGroup)
                {
                    geometryGroup.Children.Add(ConvertToGeometry(innerGroup));
                }
            }
            geometryGroup.Transform = drawingGroup.Transform;
            return geometryGroup;
        }

    }
}
