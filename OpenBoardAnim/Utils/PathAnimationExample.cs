using OpenBoardAnim.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace OpenBoardAnim.Utils
{
    public class PathAnimationExample
    {
        private Canvas _canvas;
        private List<Path> _paths;
        private GraphicModelBase _graphic;
        private UIElement _hand;
        List<double> _lengths = new List<double>();
        private int i = 0;
        public TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        public PathAnimationExample(Canvas canvas, List<Path> paths, GraphicModelBase graphic, UIElement hand)
        {
            _canvas = canvas;
            _paths = paths;
            _graphic = graphic;
            foreach (var path in _paths)
            {
                double item = GetTotalLength((PathGeometry)path.Data);
                _lengths.Add(item);
                // Set up the dash array and offset
                path.StrokeDashArray = new DoubleCollection(new double[] { item });
                path.StrokeDashOffset = item;
            }

            _hand = hand;
        }
        public async void AnimatePathOnCanvas()
        {
            Canvas.SetLeft(_hand, _graphic.X);
            Canvas.SetTop(_hand, _graphic.Y);
            // Create a MatrixTransform for the ellipse
            MatrixTransform matrixTransform = new MatrixTransform();
            _hand.RenderTransform = matrixTransform;

            Path path = _paths[i];
            // Add the path to the canvas
            _canvas.Children.Insert(_canvas.Children.Count - 1, path);
            Canvas.SetLeft(path, _graphic.X);
            Canvas.SetTop(path, _graphic.Y);
            double ratio = _lengths[i] / _lengths.Sum();
            // Create the animation
            DoubleAnimation dashOffsetAnimation = new DoubleAnimation
            {
                From = _lengths[i],
                To = 0,
                Duration = TimeSpan.FromSeconds(_graphic.Duration * ratio),

            };
            dashOffsetAnimation.Completed += DashOffsetAnimation_Completed;
            // Start the animation
            path.BeginAnimation(Shape.StrokeDashOffsetProperty, dashOffsetAnimation);


            // Create the animation
            MatrixAnimationUsingPath matrixAnimation = new MatrixAnimationUsingPath
            {
                PathGeometry = (PathGeometry)path.Data,
                Duration = TimeSpan.FromSeconds(_graphic.Duration * ratio),
                RepeatBehavior = new RepeatBehavior(1)
            };
            // Start the animation
            matrixTransform.BeginAnimation(MatrixTransform.MatrixProperty, matrixAnimation);
            await Task.CompletedTask;
        }

        private void DashOffsetAnimation_Completed(object sender, EventArgs e)
        {
            i++;
            if (i < _paths.Count)
                AnimatePathOnCanvas();
            else
            {
                Canvas.SetTop(_hand, 2000);
                tcs?.TrySetResult(true);
            }
        }

        // Helper method to calculate total length of the geometry
        private static double GetTotalLength(PathGeometry geometry)
        {
            double length = 0;
            foreach (PathFigure figure in geometry.Figures)
            {
                Point start = figure.StartPoint;
                foreach (PathSegment segment in figure.Segments)
                {
                    if (segment is LineSegment line)
                    {
                        length += (line.Point - start).Length;
                        start = line.Point;
                    }
                    else if (segment is PolyLineSegment polyLine)
                    {
                        foreach (Point point in polyLine.Points)
                        {
                            length += (point - start).Length;
                            start = point;
                        }
                    }
                    // Add cases for other segment types if needed
                }
            }
            return length;
        }
    }
}
