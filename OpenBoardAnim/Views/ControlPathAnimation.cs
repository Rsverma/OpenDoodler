using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace OpenBoardAnim.Views
{
    public class ControlPathAnimation
    {
        public async void AnimateControlAlongPath(Canvas myCanvas, PathGeometry geometry, Models.GraphicModel graphic, UIElement hand)
        {
            if(!myCanvas.Children.Contains(hand))
                myCanvas.Children.Add(hand);
            Canvas.SetLeft(hand, graphic.X);
            Canvas.SetTop(hand, graphic.Y);
            // Create a MatrixTransform for the ellipse
            MatrixTransform matrixTransform = new MatrixTransform();
            hand.RenderTransform = matrixTransform;

            // Create the animation
            MatrixAnimationUsingPath matrixAnimation = new MatrixAnimationUsingPath
            {
                PathGeometry = geometry,
                Duration = TimeSpan.FromSeconds(graphic.Duration),
                RepeatBehavior = new RepeatBehavior(1)
            };
            matrixAnimation.Completed += MatrixAnimation_Completed;
            // Start the animation
            matrixTransform.BeginAnimation(MatrixTransform.MatrixProperty, matrixAnimation);
            await Task.CompletedTask;
        }

        private void MatrixAnimation_Completed(object sender, EventArgs e)
        {
        }
    }
}
