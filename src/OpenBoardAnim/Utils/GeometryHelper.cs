using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
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
        public static PathGeometry ConvertTextToGeometry(string text, FontFamily fontFamily, FontStyle fontStyle, FontWeight fontWeight, double fontSize, bool isUnderline = false, bool isStrikethrough = false, List<TextFormatRun> formatRuns = null)
        {
            try
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

                // Baking the underline/strikethrough into the outlined geometry itself (rather
                // than drawing it separately) keeps it consistent with how text is rendered
                // everywhere else in the app - as plain vector paths, both for the settled look
                // and for the hand-drawn stroke animation, which reuses this same geometry.
                TextDecorationCollection baseDecorations = BuildDecorations(isUnderline, isStrikethrough);
                if (baseDecorations.Count > 0)
                    formattedText.SetTextDecorations(baseDecorations);

                // Per-block bold/italic/underline/strikethrough overrides on top of the base
                // typeface/decorations above - each run covers a slice of the raw text by
                // character index, set explicitly (rather than only when true) so it also
                // correctly clears a slice's formatting when the base itself has it set.
                if (formatRuns != null)
                {
                    foreach (TextFormatRun run in formatRuns)
                    {
                        if (run.Length <= 0) continue;
                        formattedText.SetFontWeight(run.IsBold ? FontWeights.Bold : FontWeights.Normal, run.Start, run.Length);
                        formattedText.SetFontStyle(run.IsItalic ? FontStyles.Italic : FontStyles.Normal, run.Start, run.Length);
                        formattedText.SetTextDecorations(BuildDecorations(run.IsUnderline, run.IsStrikethrough), run.Start, run.Length);
                    }
                }

                // Create a geometry from the formatted text
                Geometry textGeometry = formattedText.BuildGeometry(new Point(0, 0));

                // Convert to PathGeometry
                PathGeometry pathGeometry = PathGeometry.CreateFromGeometry(textGeometry);

                return pathGeometry;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
                return null;
            }
        }

        // Underline and Strikethrough both live on the one TextDecorations collection, so
        // combining them into a single collection (rather than two separate Set calls, which
        // would just overwrite each other) is what lets a run carry both at once. Public since
        // SceneRenderHelpers.BuildTextBlock's settled-state TextBlock/Run rendering needs the
        // exact same combined collection to match this geometry.
        public static TextDecorationCollection BuildDecorations(bool isUnderline, bool isStrikethrough)
        {
            TextDecorationCollection decorations = new();
            if (isUnderline)
                decorations.Add(TextDecorations.Underline[0]);
            if (isStrikethrough)
                decorations.Add(TextDecorations.Strikethrough[0]);
            return decorations;
        }

        public static DrawingGroup GetPathGeometryFromSVG(string svgText)
        {
            try
            {
                if (!string.IsNullOrEmpty(svgText))
                {
                    var svgFileReader = new FileSvgReader(new WpfDrawingSettings());
                    using (TextReader sr = new StringReader(svgText))
                    {
                        DrawingGroup drawingGroup = svgFileReader.Read(sr);
                        return drawingGroup;
                    }
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
            return null;
        }

        public static List<PathGeometry> GenerateMultiplePaths(PathGeometry pathGeometry, bool isGraphic)
        {
            List<PathGeometry> paths = new List<PathGeometry>();
            try
            {
                // Iterate through each PathFigure in the PathGeometry
                foreach (PathFigure figure in pathGeometry.Figures)
                {
                    PathFigure[] arr = [figure.Clone()];
                    PathGeometry geometry = new PathGeometry(arr);
                    if (isGraphic)
                        geometry.Transform = new TranslateTransform(-pathGeometry.Bounds.Left, -pathGeometry.Bounds.Top);
                    paths.Add(geometry);
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
            return paths;
        }
        public static Geometry ConvertToGeometry(DrawingGroup drawingGroup)
        {
            var geometryGroup = new GeometryGroup();
            try
            {

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
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
            return geometryGroup;
        }

    }
}
