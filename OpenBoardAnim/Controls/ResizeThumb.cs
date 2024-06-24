using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using OpenBoardAnim.Models;

namespace OpenBoardAnim.Controls
{
    public class ResizeThumb : Thumb
    {
        public ResizeThumb()
        {
            DragDelta += new DragDeltaEventHandler(this.ResizeThumb_DragDelta);
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            Control designerItem = this.DataContext as Control;

            if (designerItem != null)
            {
                var model = designerItem.DataContext as GraphicModel;
                if (model != null)
                {
                    double deltaVertical, deltaHorizontal;

                    switch (VerticalAlignment)
                    {
                        case System.Windows.VerticalAlignment.Bottom:
                            deltaVertical = Math.Min(-e.VerticalChange, designerItem.ActualHeight - designerItem.MinHeight);
                            designerItem.Height -= deltaVertical;
                            break;
                        case System.Windows.VerticalAlignment.Top:
                            deltaVertical = Math.Min(e.VerticalChange, designerItem.ActualHeight - designerItem.MinHeight);
                            model.Y += deltaVertical;
                            designerItem.Height -= deltaVertical;
                            break;
                        default:
                            break;
                    }

                    switch (HorizontalAlignment)
                    {
                        case System.Windows.HorizontalAlignment.Left:
                            deltaHorizontal = Math.Min(e.HorizontalChange, designerItem.ActualWidth - designerItem.MinWidth);                            
                            model.X += deltaHorizontal;
                            designerItem.Width -= deltaHorizontal;
                            break;
                        case System.Windows.HorizontalAlignment.Right:
                            deltaHorizontal = Math.Min(-e.HorizontalChange, designerItem.ActualWidth - designerItem.MinWidth);
                            designerItem.Width -= deltaHorizontal;
                            break;
                        default:
                            break;
                    }
                }
            }
            e.Handled = true;
        }
    }
}
