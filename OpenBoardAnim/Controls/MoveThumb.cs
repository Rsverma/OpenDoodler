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
                if (FindAncestor<ListBoxItem>(this) is not ListBoxItem listBoxItem) return;
                if (FindAncestor<ListBox>(listBoxItem) is not ListBox listBox) return;

                bool multiSelectModifier = Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
                if (multiSelectModifier)
                {
                    // Ctrl/Shift toggles this item in or out of the existing selection.
                    listBoxItem.IsSelected = !listBoxItem.IsSelected;
                }
                else if (!listBoxItem.IsSelected)
                {
                    // Plain click on an unselected item replaces whatever multi-selection
                    // existed before, matching standard click-to-select semantics that
                    // Thumb's own MouseLeftButtonDown handling (see class comment above)
                    // prevents ListBoxItem from doing itself.
                    listBox.SelectedItems.Clear();
                    listBoxItem.IsSelected = true;
                }
                // else: a plain click on an item that's already part of a multi-selection
                // leaves the whole selection alone, so dragging it moves the whole group.
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
                // Move every selected graphic together, not just the one this Thumb is
                // attached to - PreviewMouseLeftButtonDown above guarantees this Thumb's own
                // item is always part of SelectedItems by the time a drag can start, so a
                // single-item drag (the common case) still works exactly as before.
                if (FindAncestor<ListBoxItem>(this) is not ListBoxItem listBoxItem) return;
                if (FindAncestor<ListBox>(listBoxItem) is not ListBox listBox) return;

                foreach (GraphicModelBase model in listBox.SelectedItems.Cast<GraphicModelBase>())
                {
                    if (model.IsLocked) continue;
                    model.X += e.HorizontalChange;
                    model.Y += e.VerticalChange;
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
