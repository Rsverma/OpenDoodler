using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Media;
using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;

namespace OpenBoardAnim.Controls
{
    public class ResizeThumb : Thumb
    {
        private double originalRatio = -1;
        private double originalHeight = -1;

        // A newly-added (never-resized, ResizeRatio == 1) graphic's Image/Path content has no
        // explicit Width/Height in EditorCanvasView's DataTemplates, so it naturally measures
        // to its own intrinsic size below - an SVG authored at a large native size (or a large
        // font-size text graphic) could then bake in dimensions bigger than the board itself,
        // visually covering the whole scene the instant it's added. Capped to this fraction of
        // the current board size (preserving aspect ratio) instead.
        private const double MaxSizeFractionOfBoard = 0.5;

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
                        double height = designerItem.ActualHeight;
                        double width = designerItem.ActualWidth;
                        // ActualHeight/ActualWidth so far just reflect the container's natural,
                        // unconstrained size (nothing binds it to the model yet at this point) -
                        // clamping only the model here would leave the on-screen container at
                        // its full, unclamped intrinsic size until something else happened to
                        // resize it. Explicitly assigning the clamped values back onto
                        // designerItem (same as the else branch below does for an
                        // already-resized graphic) is what actually shrinks it immediately.
                        ClampToMaxBoardFraction(ref width, ref height);
                        model.Height = height;
                        model.Width = width;
                        designerItem.Height = height;
                        designerItem.Width = width;
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

        private void ClampToMaxBoardFraction(ref double width, ref double height)
        {
            if (width <= 0 || height <= 0) return;
            if (FindAncestor<Canvas>(this) is not Canvas boardCanvas) return;
            if (boardCanvas.ActualWidth <= 0 || boardCanvas.ActualHeight <= 0) return;

            double maxWidth = boardCanvas.ActualWidth * MaxSizeFractionOfBoard;
            double maxHeight = boardCanvas.ActualHeight * MaxSizeFractionOfBoard;
            double scale = Math.Min(1, Math.Min(maxWidth / width, maxHeight / height));
            if (scale >= 1) return;

            width *= scale;
            height *= scale;
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match)
                    return match;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
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
