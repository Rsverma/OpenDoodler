using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace OpenBoardAnim
{
    /// <summary>
    /// Interaction logic for Page1.xaml
    /// </summary>
    public partial class Page1 : Page
    {
        public Page1()
        {
            InitializeComponent();
            CaptureMyScreen();
        }

        public static readonly DependencyProperty PointProperty = DependencyProperty.Register(
            "Point", typeof(Point), typeof(Page1), new FrameworkPropertyMetadata(new Point(0, 0), OnPropertiesChanged));

        private static void OnPropertiesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Page1 path)
            {
                path.UpdatePath((Point)e.NewValue);
            }
        }

        private void UpdatePath(Point newValue)
        {
            this.arc1.Points.Add(newValue);
        }

        public Point Point
        {
            get => (Point)GetValue(PointProperty);
            set => SetValue(PointProperty, value);
        }

        private void CaptureMyScreen()
        {
            string data = "M50,150 c-25,-125 125,-125 125,-25 c0,-100 150,-100 125,25 c-25,75 -100,100 -125,175 c-25,-75 -100,-100 -125,-175";
            Geometry geo1 = Geometry.Parse(data);
            PathGeometry path = geo1.GetFlattenedPathGeometry();
            MatrixTransform buttonMatrixTransform = new MatrixTransform();
            aButton.RenderTransform = buttonMatrixTransform;

            // Register the transform's name with the page
            // so that it can be targeted by a Storyboard.
            this.RegisterName("ButtonMatrixTransform", buttonMatrixTransform);

            // Create a MatrixAnimationUsingPath to move the
            // button along the path by animating
            // its MatrixTransform.M50,150 c-25,-125 125,-125 125,-25 c0,-100 150,-100 125,25 c-25,75 -100,100 -125,175 c-25,-75 -100,-100 -125,-175
            MatrixAnimationUsingPath matrixAnimation = new MatrixAnimationUsingPath();
            matrixAnimation.PathGeometry = path;

            // Set IsOffsetCumulative to true so that the animation
            // values accumulate when its repeats.
            matrixAnimation.IsOffsetCumulative = false;
            matrixAnimation.Duration = TimeSpan.FromSeconds(3);
            matrixAnimation.RepeatBehavior = RepeatBehavior.Forever;

            // Set the animation to target the Matrix property
            // of the MatrixTransform named "ButtonMatrixTransform".
            Storyboard.SetTargetName(matrixAnimation, "ButtonMatrixTransform");
            Storyboard.SetTargetProperty(matrixAnimation, new PropertyPath(MatrixTransform.MatrixProperty));

            // Create a Storyboard to contain and apply the animation.
            Storyboard pathAnimationStoryboard = new Storyboard();
            pathAnimationStoryboard.Children.Add(matrixAnimation);
            pathAnimationStoryboard.Begin(this);
        }
    }
}