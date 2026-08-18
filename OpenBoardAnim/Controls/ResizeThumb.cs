using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;

namespace OpenBoardAnim.Controls
{
    public class ResizeThumb : Thumb
    {
        private double originalRatio = -1;
        private double originalHeight = -1;
        public ResizeThumb()
        {
            DragDelta += new DragDeltaEventHandler(this.ResizeThumb_DragDelta);
            DragStarted += ResizeThumb_DragStarted;
            Loaded += ResizeThumb_Loaded;
        }

        private void ResizeThumb_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                Control designerItem = this.DataContext as Control;

                if (designerItem != null)
                {
                    var model = designerItem.DataContext as GraphicModelBase;
                    if (model != null && model.ResizeRatio == 1)
                    {
                        model.Height = designerItem.ActualHeight;
                        model.Width = designerItem.ActualWidth;
                    }
                    else
                    {
                        designerItem.Height = model.Height;
                        designerItem.Width = model.Width;
                    }
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // Re-baselines originalRatio/originalHeight from the persisted model at the start of
        // EVERY drag gesture, not just once for the lifetime of this Thumb instance. Without
        // this, a second resize on the same container (or the first resize after a scene
        // switch recreates this container and ResizeThumb_Loaded restores the item's size
        // from the model) could clamp against a stale aspect ratio/height captured from
        // ActualHeight/ActualWidth before layout had caught up with the restored size -
        // visually the box would barely change size while its content appeared to rescale
        // inside it. Reading straight from the model sidesteps that layout-timing race.
        private void ResizeThumb_DragStarted(object sender, DragStartedEventArgs e)
        {
            try
            {
                Control designerItem = this.DataContext as Control;
                if (designerItem == null) return;
                var model = designerItem.DataContext as GraphicModelBase;
                if (model == null) return;

                designerItem.Height = model.Height;
                designerItem.Width = model.Width;
                originalHeight = model.Height;
                originalRatio = model.Height / model.Width;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            try
            {
                Control designerItem = this.DataContext as Control;

                if (designerItem != null)
                {
                    var model = designerItem.DataContext as GraphicModelBase;
                    if (model != null && !model.IsLocked)
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
                        double newRatio = designerItem.Height / designerItem.Width;
                        if (newRatio > originalRatio) designerItem.Height = originalRatio * designerItem.Width;
                        else designerItem.Width = designerItem.Height / originalRatio;
                        model.ResizeRatio = designerItem.Height / originalHeight;
                        model.Height = designerItem.Height;
                        model.Width = designerItem.Width;
                    }
                }

                e.Handled = true;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }
    }
}
