using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using OpenBoardAnim.ViewModels;
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

namespace OpenBoardAnim.Views
{
    /// <summary>
    /// Interaction logic for EditorActionsView.xaml
    /// </summary>
    public partial class EditorActionsView : UserControl
    {
        // A custom format string rather than typeof(GraphicModelBase) - DataObject registers a
        // payload under its own RUNTIME type (DrawingModel/TextModel), not the declared/base
        // type, so GetDataPresent(typeof(GraphicModelBase)) would never match either concrete
        // graphic type.
        private const string LayerDragFormat = "OpenDoodlerLayerReorder";

        private GraphicModelBase _dragCandidate;
        private Point _dragStartPoint;

        public EditorActionsView()
        {
            InitializeComponent();
        }

        private void ToggleLock_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not FrameworkElement element || element.DataContext is not GraphicModelBase model) return;
                model.IsLocked = !model.IsLocked;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void ToggleVisibility_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not FrameworkElement element || element.DataContext is not GraphicModelBase model) return;
                model.IsVisible = !model.IsVisible;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // Only the drag-handle icon (not the whole row) starts a drag gesture, so it doesn't
        // interfere with clicking the Delay/Duration editors, the lock/visibility buttons, or
        // plain row-click-to-select.
        private void DragHandle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is not FrameworkElement element || element.DataContext is not GraphicModelBase model) return;
                _dragCandidate = model;
                _dragStartPoint = e.GetPosition(null);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void DragHandle_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_dragCandidate == null || e.LeftButton != MouseButtonState.Pressed) return;
                if (sender is not DependencyObject dragSource) return;

                Point current = e.GetPosition(null);
                if (Math.Abs(current.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                    Math.Abs(current.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
                    return;

                GraphicModelBase dragged = _dragCandidate;
                // One DoDragDrop call per gesture - DoDragDrop pumps its own message loop until
                // the drag ends, so this guards against re-entering here before that returns.
                _dragCandidate = null;
                DragDrop.DoDragDrop(dragSource, new DataObject(LayerDragFormat, dragged), DragDropEffects.Move);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void LayersListBox_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(LayerDragFormat) ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private void LayersListBox_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (!e.Data.GetDataPresent(LayerDragFormat)) return;
                if (e.Data.GetData(LayerDragFormat) is not GraphicModelBase dragged) return;
                if (DataContext is not EditorActionsViewModel viewModel || viewModel.SceneGraphics == null) return;

                if (e.OriginalSource is not DependencyObject hit) return;
                if (FindAncestor<ListBoxItem>(hit) is not ListBoxItem targetContainer) return;
                if (targetContainer.DataContext is not GraphicModelBase target || ReferenceEquals(target, dragged)) return;

                var graphics = viewModel.SceneGraphics;
                int oldIndex = graphics.IndexOf(dragged);
                int newIndex = graphics.IndexOf(target);
                if (oldIndex < 0 || newIndex < 0) return;

                graphics.RemoveAt(oldIndex);
                graphics.Insert(newIndex, dragged);
                e.Handled = true;
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
    }
}
