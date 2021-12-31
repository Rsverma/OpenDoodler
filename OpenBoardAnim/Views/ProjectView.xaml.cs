using System.Windows;
using System.Linq;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;
using Shapes = System.Windows.Shapes;

namespace OpenBoardAnim.Views
{
    /// <summary>
    /// Interaction logic for ProjectView.xaml
    /// </summary>
    public partial class ProjectView : UserControl
    {
        public ProjectView()
        {
            InitializeComponent();
        }

        private void Rectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.Source is Shapes.Shape shape)
            {
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    //Point p = e.GetPosition(canvas);
                    //Canvas.SetLeft(shape, p.X - shape.ActualWidth / 2);
                    //Canvas.SetTop(shape, p.Y - shape.ActualHeight / 2);
                    //shape.CaptureMouse();
                }
                else
                {
                    shape.ReleaseMouseCapture();
                }
            }
        }


    }
}