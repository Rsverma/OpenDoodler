using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OpenBoardAnim.Controls
{
    /// <summary>
    /// Interaction logic for DraggablePath.xaml
    /// </summary>
    public partial class DraggablePath : UserControl
    {
        protected bool isDragging;
        private Point mousePosition;
        private double prevX, prevY;
        public DraggablePath()
        {
            InitializeComponent();
        }
        private void Path_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                isDragging = true;
                var draggableControl = (sender as UserControl);
                mousePosition = e.GetPosition(Parent as UIElement);
                draggableControl.CaptureMouse();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void Path_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                isDragging = false;
                var draggable = (sender as UserControl);
                var transform = (draggable.RenderTransform as TranslateTransform);
                if (transform != null)
                {
                    prevX = transform.X;
                    prevY = transform.Y;
                }
                DrawingModel model = draggable.DataContext as DrawingModel;
                if (model != null)
                {
                    model.X = prevX;
                    model.Y = prevY;
                }
                draggable.RenderTransform = null;
                draggable.ReleaseMouseCapture();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void Path_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                var draggableControl = (sender as UserControl);
                if (!isDragging) return;
                if (isDragging && draggableControl != null)
                {
                    var currentPosition = e.GetPosition(Parent as UIElement);
                    var transform = (draggableControl.RenderTransform as TranslateTransform);
                    if (transform == null)
                    {
                        transform = new TranslateTransform();
                        draggableControl.RenderTransform = transform;
                    }
                    transform.X = (currentPosition.X - mousePosition.X);
                    transform.Y = (currentPosition.Y - mousePosition.Y);
                    if (prevX > 0)
                    {
                        transform.X += prevX;
                        transform.Y += prevY;
                    }

                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }
    }
}
