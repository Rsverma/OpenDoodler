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
using OpenBoardAnim.ViewModels;

namespace OpenBoardAnim.Controls
{
    public class MoveThumb : Thumb
    {
        // Screen-pixel snap distance, converted to canvas units by dividing by the current
        // zoom level so the feel stays consistent whether zoomed in or out.
        private const double SnapThresholdPixels = 8;

        // Found lazily on first drag and reused for the rest of this Thumb's lifetime, rather
        // than walking the ListBox's visual tree on every DragDelta (which fires continuously
        // while dragging).
        private Canvas _itemsCanvas;

        public MoveThumb()
        {
            DragDelta += new DragDeltaEventHandler(this.MoveThumb_DragDelta);
            DragCompleted += MoveThumb_DragCompleted;
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

                GraphicModelBase model = listBoxItem.DataContext as GraphicModelBase;
                bool multiSelectModifier = Keyboard.Modifiers.HasFlag(ModifierKeys.Control) || Keyboard.Modifiers.HasFlag(ModifierKeys.Shift);
                if (multiSelectModifier)
                {
                    // Ctrl/Shift toggles this item in or out of the existing selection - a
                    // grouped item toggles its whole group together so a group never ends up
                    // partially selected.
                    bool nowSelected = !listBoxItem.IsSelected;
                    SetGroupSelection(listBox, model, nowSelected);
                }
                else if (!listBoxItem.IsSelected)
                {
                    // Plain click on an unselected item replaces whatever multi-selection
                    // existed before, matching standard click-to-select semantics that
                    // Thumb's own MouseLeftButtonDown handling (see class comment above)
                    // prevents ListBoxItem from doing itself. A grouped item selects every
                    // graphic sharing its GroupId, so the whole group drags together.
                    listBox.SelectedItems.Clear();
                    SetGroupSelection(listBox, model, true);
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

        // Selects (or deselects) model plus every other graphic sharing its GroupId, if any -
        // ungrouped graphics (GroupId == null) just select themselves, same as before grouping
        // existed.
        private static void SetGroupSelection(ListBox listBox, GraphicModelBase model, bool select)
        {
            if (model == null) return;
            foreach (object item in listBox.Items)
            {
                if (item is not GraphicModelBase graphic) continue;
                bool sameItem = ReferenceEquals(graphic, model);
                bool sameGroup = model.GroupId.HasValue && graphic.GroupId == model.GroupId;
                if (!sameItem && !sameGroup) continue;

                if (select)
                {
                    if (!listBox.SelectedItems.Contains(graphic))
                        listBox.SelectedItems.Add(graphic);
                }
                else
                {
                    listBox.SelectedItems.Remove(graphic);
                }
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

                List<GraphicModelBase> movable = new();
                foreach (GraphicModelBase model in listBox.SelectedItems.Cast<GraphicModelBase>())
                {
                    if (model.IsLocked) continue;
                    model.X += e.HorizontalChange;
                    model.Y += e.VerticalChange;
                    movable.Add(model);
                }

                if (movable.Count > 0 && listBox.DataContext is EditorCanvasViewModel canvasViewModel)
                    ApplySnapping(listBox, canvasViewModel, movable);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // Snaps the dragged selection's combined bounding box to nearby graphic edges/centers
        // and the canvas edges/center once within SnapThresholdPixels, correcting the models'
        // position by the (small) remaining offset, and updates the guide-line properties on
        // EditorCanvasViewModel so EditorCanvasView can draw a line at whatever coordinate was
        // snapped to. Runs after the raw drag delta has already been applied above, rather than
        // intercepting it beforehand, so this only ever needs to nudge the already-applied move.
        private void ApplySnapping(ListBox listBox, EditorCanvasViewModel canvasViewModel, List<GraphicModelBase> dragging)
        {
            Canvas itemsCanvas = _itemsCanvas ??= FindDescendant<Canvas>(listBox);
            if (itemsCanvas == null) return;

            double threshold = SnapThresholdPixels / Math.Max(canvasViewModel.ZoomLevel, 0.01);

            double dragLeft = double.MaxValue, dragTop = double.MaxValue, dragRight = double.MinValue, dragBottom = double.MinValue;
            foreach (GraphicModelBase model in dragging)
            {
                (double width, double height) = GetRenderedSize(listBox, model);
                dragLeft = Math.Min(dragLeft, model.X);
                dragTop = Math.Min(dragTop, model.Y);
                dragRight = Math.Max(dragRight, model.X + width);
                dragBottom = Math.Max(dragBottom, model.Y + height);
            }
            double dragCenterX = (dragLeft + dragRight) / 2;
            double dragCenterY = (dragTop + dragBottom) / 2;

            List<double> xTargets = new() { 0, itemsCanvas.ActualWidth / 2, itemsCanvas.ActualWidth };
            List<double> yTargets = new() { 0, itemsCanvas.ActualHeight / 2, itemsCanvas.ActualHeight };
            foreach (GraphicModelBase other in listBox.Items.Cast<GraphicModelBase>())
            {
                if (!other.IsVisible || dragging.Contains(other)) continue;
                (double otherWidth, double otherHeight) = GetRenderedSize(listBox, other);
                xTargets.Add(other.X);
                xTargets.Add(other.X + otherWidth / 2);
                xTargets.Add(other.X + otherWidth);
                yTargets.Add(other.Y);
                yTargets.Add(other.Y + otherHeight / 2);
                yTargets.Add(other.Y + otherHeight);
            }

            bool snappedX = FindClosestSnap(new[] { dragLeft, dragCenterX, dragRight }, xTargets, threshold, out double snapX, out double correctionX);
            bool snappedY = FindClosestSnap(new[] { dragTop, dragCenterY, dragBottom }, yTargets, threshold, out double snapY, out double correctionY);

            if (snappedX)
            {
                foreach (GraphicModelBase model in dragging)
                    model.X += correctionX;
            }
            if (snappedY)
            {
                foreach (GraphicModelBase model in dragging)
                    model.Y += correctionY;
            }

            canvasViewModel.IsSnapGuideXVisible = snappedX;
            canvasViewModel.SnapGuideX = snapX;
            canvasViewModel.IsSnapGuideYVisible = snappedY;
            canvasViewModel.SnapGuideY = snapY;
        }

        // model.Width/Height get auto-populated from the container's own ActualWidth/
        // ActualHeight the first time a never-resized graphic loads (see
        // ResizeThumb.ResizeThumb_Loaded's ResizeRatio == 1 branch), which can end up not
        // quite matching what's actually rendered on screen after a scene reload - reading the
        // real container's current ActualWidth/ActualHeight directly sidesteps any such drift,
        // since that's the one number guaranteed to match the pixels on screen.
        private static (double Width, double Height) GetRenderedSize(ListBox listBox, GraphicModelBase model)
        {
            if (listBox.ItemContainerGenerator.ContainerFromItem(model) is FrameworkElement container
                && container.ActualWidth > 0 && container.ActualHeight > 0)
                return (container.ActualWidth, container.ActualHeight);
            return (model.Width, model.Height);
        }

        // Picks whichever (edge, target) pair is closest across all combinations, so e.g. the
        // dragged selection's right edge can snap to one graphic while its top independently
        // snaps to a different one.
        private static bool FindClosestSnap(double[] edges, List<double> targets, double threshold, out double snapValue, out double correction)
        {
            double bestDiff = threshold;
            snapValue = 0;
            correction = 0;
            bool found = false;
            foreach (double edge in edges)
            {
                foreach (double target in targets)
                {
                    double diff = Math.Abs(edge - target);
                    if (diff < bestDiff)
                    {
                        bestDiff = diff;
                        snapValue = target;
                        correction = target - edge;
                        found = true;
                    }
                }
            }
            return found;
        }

        private void MoveThumb_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            try
            {
                if (FindAncestor<ListBox>(this) is not ListBox listBox) return;
                if (listBox.DataContext is not EditorCanvasViewModel canvasViewModel) return;

                canvasViewModel.IsSnapGuideXVisible = false;
                canvasViewModel.IsSnapGuideYVisible = false;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private static T FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);
                if (child is T match) return match;
                T found = FindDescendant<T>(child);
                if (found != null) return found;
            }
            return null;
        }
    }
}
