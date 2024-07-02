using OpenBoardAnim.Models;
using OpenBoardAnim.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static MaterialDesignThemes.Wpf.Theme;

namespace OpenBoardAnim.Views
{
    /// <summary>
    /// Interaction logic for ProjectPreviewView.xaml
    /// </summary>
    public partial class ProjectPreviewView : UserControl
    {
        public ProjectPreviewView()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            ProjectDetails project = this.DataContext as ProjectDetails;
            NameScope.SetNameScope(this, new NameScope());
            if (project != null)
            {
                for (int i = 0; i < project.Scenes.Count-1; i++)
                {
                    SceneModel scene = project.Scenes[i];
                    if (scene != null)
                    {
                        for (int j = 0; j < scene.Graphics.Count; j++)
                        {
                            GraphicModel graphic = scene.Graphics[j];
                            await Task.Delay((int)graphic.Delay * 1000);
                            GeometryGroup geo = SVGHelper.ConvertToGeometry(graphic.ImgGeometry);
                            PathGeometry path = geo.GetFlattenedPathGeometry();
                            path.Freeze();

                            MoveHandAlongPath(j, graphic, path);
                            await Task.Delay((int)graphic.Duration * 1000);
                            var image = new Image
                            {
                                Source = new DrawingImage(graphic.ImgGeometry)
                            };
                            image.Height = graphic.Height;
                            image.Width = graphic.Width;
                            PreviewCanvas.Children.Add(image);
                            Canvas.SetLeft(image, graphic.X);
                            Canvas.SetTop(image, graphic.Y);
                        }
                    }
                }
            }
        }

        private void MoveHandAlongPath(int j, GraphicModel graphic, PathGeometry path)
        {
            MatrixTransform buttonMatrixTransform = new MatrixTransform();
            hand.RenderTransform = buttonMatrixTransform;
            this.RegisterName("ButtonMatrixTransform" + j, buttonMatrixTransform);

            MatrixAnimationUsingPath matrixAnimation = new MatrixAnimationUsingPath();
            matrixAnimation.PathGeometry = path;
            matrixAnimation.Duration = TimeSpan.FromSeconds(graphic.Duration);
            Storyboard.SetTargetName(matrixAnimation, "ButtonMatrixTransform" + j);
            Storyboard.SetTargetProperty(matrixAnimation, new PropertyPath(MatrixTransform.MatrixProperty));

            // Create a Storyboard to contain and apply the animation.
            Storyboard pathAnimationStoryboard = new Storyboard();
            pathAnimationStoryboard.Children.Add(matrixAnimation);
            pathAnimationStoryboard.Begin(this);
        }
    }
}
