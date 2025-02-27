using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBoardAnim.Library.Repositories
{
    public class ShapeRepository
    {
        public List<GraphicEntity> GetAllShapess()
        {
            List<GraphicEntity> shapes = [];
            shapes.Add(new GraphicEntity
            {
                Name = "Rectangle",
                SVGText = " <svg width=\"200\" height=\"100\" xmlns=\"http://www.w3.org/2000/svg\">\r\n  <rect width=\"200\" height=\"100\" style=\"stroke:black;stroke-width:3;fill-opacity:0;stroke-opacity:1\"/>\r\n</svg> "
            });
            shapes.Add(new GraphicEntity
            {
                Name = "Rectangle-rounded",
                SVGText = " <svg width=\"200\" height=\"100\" xmlns=\"http://www.w3.org/2000/svg\">\r\n  <rect width=\"200\" height=\"100\" rx=\"20\" ry=\"20\" style=\"stroke:black;stroke-width:3;fill-opacity:0;stroke-opacity:1\"/>\r\n</svg> "
            });
            shapes.Add(new GraphicEntity
            {
                Name = "Square",
                SVGText = " <svg width=\"200\" height=\"200\" xmlns=\"http://www.w3.org/2000/svg\">\r\n  <rect width=\"200\" height=\"200\" style=\"stroke:black;stroke-width:3;fill-opacity:0;stroke-opacity:1\"/>\r\n</svg> "
            });
            shapes.Add(new GraphicEntity
            {
                Name = "Square-rounded",
                SVGText = " <svg width=\"200\" height=\"200\" xmlns=\"http://www.w3.org/2000/svg\">\r\n  <rect width=\"200\" height=\"200\" rx=\"20\" ry=\"20\" style=\"stroke:black;stroke-width:3;fill-opacity:0;stroke-opacity:1\"/>\r\n</svg> "
            });
            shapes.Add(new GraphicEntity
            {
                Name = "Circle",
                SVGText = " <svg height=\"100\" width=\"100\" xmlns=\"http://www.w3.org/2000/svg\">\r\n  <circle r=\"45\" cx=\"50\" cy=\"50\" fill=\"none\" stroke=\"black\" stroke-width=\"3\" opacity=\"1\" />\r\n</svg> "
            });
            shapes.Add(new GraphicEntity
            {
                Name = "Ellipse-Vertical",
                SVGText = "<svg height=\"500\" width=\"140\" xmlns=\"http://www.w3.org/2000/svg\">\r\n  <ellipse rx=\"50\" ry=\"100\" cx=\"80\" cy=\"120\"\r\n  style=\"fill:none;stroke:black;stroke-width:3\" />\r\n</svg> "
            });
            shapes.Add(new GraphicEntity
            {
                Name = "Line-Horizontal",
                SVGText = "<svg height=\"25\" width=\"300\" xmlns=\"http://www.w3.org/2000/svg\">\r\n  <line x1=\"0\" y1=\"10\" x2=\"250\" y2=\"10\" style=\"stroke:black;stroke-width:3\" />\r\n</svg>"
            });
            shapes.Add(new GraphicEntity
            {
                Name = "Triangle",
                SVGText = " <svg height=\"210\" width=\"210\" xmlns=\"http://www.w3.org/2000/svg\">\r\n  <polygon points=\"100,0 200,200 0,200\" style=\"fill:none;stroke:black;stroke-width:3\" />\r\n</svg> "
            });
            shapes.Add(new GraphicEntity
            {
                Name = "Speech-Bubble",
                SVGText = "<svg fill=\"#000000\" width=\"100px\" height=\"100px\" viewBox=\"0 0 32 32\" version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\">\r\n    <path d=\"M16 4c7.72 0 14 4.486 14 10s-6.28 10-14 10l-0.829 0.003c-0.55 0-0.909-0.015-1.471-0.099l-1.12-0.16-0.719 0.87c-0.331 0.399-2.017 1.785-3.878 2.677 0.378-1.001 0.657-2.094 0.683-3.175l0.010-0.059v-1.395l-1.090-0.556c-3.55-1.816-5.585-4.77-5.585-8.106 0-5.514 6.28-10 14-10zM16 2c-8.838 0-16 5.373-16 12 0 4.127 2.446 7.724 6.675 9.886 0 0.026-0.008 0.044-0.008 0.073 0 1.793-1.005 3.765-1.594 4.779h0.002c-0.046 0.109-0.074 0.229-0.074 0.357 0 0.503 0.405 0.906 0.907 0.906 0.075 0 0.196-0.015 0.239-0.015 0.011 0 0.016 0 0.016 0.003 3.125-0.511 6.561-3.271 7.245-4.104 0.703 0.105 1.177 0.12 1.765 0.12 0.248 0 0.515-0.003 0.829-0.003 8.836 0 16-5.372 16-12 0-6.627-7.164-12-16-12z\"></path>\r\n</svg>"
            });
            shapes.Add(new GraphicEntity
            {
                Name = "Thought-Bubble",
                SVGText = "<svg width=\"100px\" height=\"100px\" viewBox=\"0 0 128 128\" xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\" aria-hidden=\"true\" role=\"img\" class=\"iconify iconify--noto\" preserveAspectRatio=\"xMidYMid meet\"><path d=\"M120.44 51.23a29.87 29.87 0 0 0 2.96-13.02c0-16.6-13.45-30.05-30.05-30.05c-3.89 0-7.61.75-11.03 2.1C77.95 6.45 72.22 4.1 66 4.1c-7.6 0-14.4 3.4-18.9 8.7c-3.5-1.9-7.5-3-11.7-3c-13.4.1-24.3 10.9-24.3 24.3c0 5 1.5 9.7 4.2 13.6c-5 4-8.5 9.9-9.2 16.8C4.8 77.9 14.7 90 28.3 91.3c3.2.3 6.2 0 9.1-.8c1.1 10.7 10.1 19 21.1 19c7 0 13.2-3.4 17-8.6c3.6 2.8 8.1 4.6 13.1 4.6c11 0 20.1-8.5 20.9-19.2C118 82.4 124 73.8 124 63.8c0-4.59-1.33-8.92-3.56-12.57z\" fill=\"#ffffff\" stroke=\"#000000\" stroke-width=\"6\" stroke-miterlimit=\"10\"></path><path d=\"M24.3 97.3c-4.5-.5-8.5 2.8-9 7.3s2.8 8.5 7.3 8.9c4.5.5 8.5-2.8 9-7.3s-2.8-8.5-7.3-8.9z\" fill=\"#ffffff\" stroke=\"#000000\" stroke-width=\"4.5\" stroke-miterlimit=\"10\"></path><path d=\"M9 114.3c-3-.3-5.7 1.9-6 4.9s1.9 5.6 4.9 5.9s5.7-1.9 6-4.9c.3-2.9-1.9-5.6-4.9-5.9z\" fill=\"#ffffff\" stroke=\"#000000\" stroke-width=\"3\" stroke-miterlimit=\"10\"></path></svg>"
            });
            return shapes;
        }
    }
}
