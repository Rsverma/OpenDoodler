﻿using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace OpenBoardAnim.Utils
{
    public class PathAnimationHelper
    {
        private Canvas _canvas;
        private List<Path> _paths;
        private GraphicModelBase _graphic;
        private UIElement _hand;
        List<double> _lengths = new List<double>();
        public TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        public PathAnimationHelper(Canvas canvas,
                                   List<Path> paths,
                                   GraphicModelBase graphic,
                                   UIElement hand)
        {
            try
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
                Canvas.SetLeft(_hand, _graphic.X);
                Canvas.SetTop(_hand, _graphic.Y);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }
        public async void AnimatePathOnCanvas()
        {
            try
            {
                Storyboard storyboard = new();
                TimeSpan beginTime = TimeSpan.Zero;
                for (int i = 0; i < _paths.Count; i++)
                {
                    Path path = _paths[i];
                    // Add the path to the canvas
                    _canvas.Children.Add(path);
                    Canvas.SetLeft(path, _graphic.X);
                    Canvas.SetTop(path, _graphic.Y);
                    double ratio = _lengths[i] / _lengths.Sum();
                    // Create the animation
                    TimeSpan timeSpan = TimeSpan.FromSeconds(_graphic.Duration * ratio);
                    DoubleAnimation dashOffsetAnimation = new DoubleAnimation
                    {
                        From = _lengths[i],
                        To = 0,
                        Duration = timeSpan,
                        BeginTime = beginTime
                    };

                    // Create the animation
                    MatrixAnimationUsingPath matrixAnimation = new MatrixAnimationUsingPath
                    {
                        PathGeometry = (PathGeometry)path.Data,
                        Duration = timeSpan,
                        BeginTime = beginTime
                    };
                    beginTime += timeSpan;
                    // Create a MatrixTransform for the ellipse
                    MatrixTransform matrixTransform = new MatrixTransform();
                    _hand.RenderTransform = matrixTransform;
                    DependencyProperty[] propertyChain = [UIElement.RenderTransformProperty, MatrixTransform.MatrixProperty];
                    string thePath = "(0).(1)";
                    PropertyPath myPropertyPath = new(thePath, propertyChain);
                    // Start the animation
                    Storyboard.SetTarget(matrixAnimation, _hand);
                    Storyboard.SetTargetProperty(matrixAnimation, myPropertyPath);
                    Storyboard.SetTarget(dashOffsetAnimation, path);
                    Storyboard.SetTargetProperty(dashOffsetAnimation, new PropertyPath(Shape.StrokeDashOffsetProperty));
                    storyboard.Children.Add(matrixAnimation);
                    storyboard.Children.Add(dashOffsetAnimation);
                }
                storyboard.Completed += DashOffsetAnimation_Completed;
                storyboard.Begin();

            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
            await Task.CompletedTask;
        }
        private void DashOffsetAnimation_Completed(object sender, EventArgs e)
        {
            try
            {
                Canvas.SetLeft(_hand, 2000);
                Canvas.SetTop(_hand, 1100);
                tcs?.TrySetResult(true);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // Helper method to calculate total length of the geometry
        private static double GetTotalLength(PathGeometry geometry)
        {
            double length = 0;
            try
            {
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
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
            return length;
        }
    }
}
