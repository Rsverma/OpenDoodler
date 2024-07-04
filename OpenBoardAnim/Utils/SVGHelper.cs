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

    public class SVGHelper
    {
        
        public static DrawingGroup GetPathGeometryFromSVG(string filePath)
        {
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
                            clone.Children.Add(geometry.Transform);
                        }
                        geometry.Transform = clone;
                        geometrylist.Add(new GeometryWithFill(geometry,geometryDrawing.Brush));
                    }
                    else if (drawing is DrawingGroup innerGroup)
                    {
                        if (innerGroup.Transform != null)
                            clone.Children.Add(innerGroup.Transform);
                        geometrylist.AddRange(ConvertToGeometry(innerGroup, clone));
                    }
                }
            }

            return geometrylist;
        }

    }
}
