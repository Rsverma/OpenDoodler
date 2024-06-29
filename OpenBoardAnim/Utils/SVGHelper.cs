using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace OpenBoardAnim.Utils
{
    public class SVGHelper
    {

        public static DrawingGroup GetPathGeometryFromSVG(string filePath)
        {
            var svgFileReader = new FileSvgReader(new WpfDrawingSettings());
            return svgFileReader.Read(filePath);
        }

        public static GeometryGroup ConvertToGeometry(DrawingGroup drawingGroup)
        {
            var geometryGroup = new GeometryGroup();
            if (drawingGroup != null)
            {
                foreach (var drawing in drawingGroup.Children)
                {
                    if (drawing is GeometryDrawing geometryDrawing)
                    {
                        geometryGroup.Children.Add(geometryDrawing.Geometry);
                    }
                    else if (drawing is DrawingGroup innerGroup)
                    {
                        geometryGroup.Children.Add(ConvertToGeometry(innerGroup));
                    }
                }
            }

            geometryGroup.Transform = drawingGroup.Transform;
            return geometryGroup;
        }

    }
}
