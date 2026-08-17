using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;

namespace OpenBoardAnim.Controls
{
    public class MoveThumb : Thumb
    {
        public MoveThumb()
        {
            DragDelta += new DragDeltaEventHandler(this.MoveThumb_DragDelta);
            // Thumb handles MouseLeftButtonDown itself (to capture the mouse for
            // dragging) and marks it Handled, so it never reaches ListBoxItem's own
            // click-to-select logic. Select the item ourselves on the tunneling
            // preview event, which always runs first regardless of what Thumb does.
            PreviewMouseLeftButtonDown += MoveThumb_PreviewMouseLeftButtonDown;
        }

        private void MoveThumb_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (FindAncestor<ListBoxItem>(this) is ListBoxItem listBoxItem)
                    listBoxItem.IsSelected = true;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
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

        private void MoveThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            try
            {
                Control designerItem = this.DataContext as Control;

                if (designerItem != null)
                {
                    var model = designerItem.DataContext as GraphicModelBase;
                    if (model != null)
                    {

                        model.X += e.HorizontalChange;
                        model.Y += e.VerticalChange;
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
