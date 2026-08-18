using Microsoft.Win32;
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
using System.Windows.Controls.Primitives;
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
    /// Interaction logic for EditorTimelineView.xaml
    /// </summary>
    public partial class EditorTimelineView : UserControl
    {
        // A custom format string rather than typeof(SceneModel) - DataObject registers a payload
        // under its own runtime type, and using a distinct format also keeps this drag from being
        // confused with any other drag/drop format elsewhere in the app.
        private const string SceneDragFormat = "OpenDoodlerSceneReorder";

        private SceneModel _dragCandidate;
        private Point _dragStartPoint;

        public EditorTimelineView()
        {
            InitializeComponent();
        }

        private void SceneCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is not FrameworkElement element || element.DataContext is not SceneTimelineSegment segment) return;
                if (DataContext is not EditorTimelineViewModel viewModel) return;
                viewModel.SelectedScene = segment.Scene;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void SceneCard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (sender is not FrameworkElement element || element.DataContext is not SceneTimelineSegment segment) return;
                _dragCandidate = segment.Scene;
                _dragStartPoint = e.GetPosition(null);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void SceneCard_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_dragCandidate == null || e.LeftButton != MouseButtonState.Pressed) return;
                if (sender is not DependencyObject dragSource) return;

                Point current = e.GetPosition(null);
                if (Math.Abs(current.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
                    Math.Abs(current.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
                    return;

                SceneModel dragged = _dragCandidate;
                // One DoDragDrop call per gesture - it pumps its own message loop until the drag
                // ends, so this guards against re-entering here before that returns.
                _dragCandidate = null;
                DragDrop.DoDragDrop(dragSource, new DataObject(SceneDragFormat, dragged), DragDropEffects.Move);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void ScenesItemsControl_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(SceneDragFormat) ? DragDropEffects.Move : DragDropEffects.None;
            e.Handled = true;
        }

        private void ScenesItemsControl_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (!e.Data.GetDataPresent(SceneDragFormat)) return;
                if (e.Data.GetData(SceneDragFormat) is not SceneModel dragged) return;
                if (DataContext is not EditorTimelineViewModel viewModel) return;
                if (e.OriginalSource is not DependencyObject hit) return;

                SceneTimelineSegment targetSegment = FindSceneSegment(hit);
                if (targetSegment?.Scene is not SceneModel target || ReferenceEquals(target, dragged)) return;

                viewModel.MoveScene(dragged, target);
                e.Handled = true;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // Walks up from wherever the drop landed (could be any descendant inside a scene card's
        // Border - the graphics thumbnail, the name TextBlock, etc.) to find the card whose
        // DataContext is the SceneTimelineSegment it belongs to.
        private static SceneTimelineSegment FindSceneSegment(DependencyObject current)
        {
            while (current != null)
            {
                if (current is FrameworkElement element && element.DataContext is SceneTimelineSegment segment)
                    return segment;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private void Playhead_DragDelta(object sender, DragDeltaEventArgs e)
        {
            try
            {
                if (DataContext is not EditorTimelineViewModel viewModel) return;
                viewModel.MovePlayheadPreview(e.HorizontalChange);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void Playhead_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            try
            {
                if (DataContext is not EditorTimelineViewModel viewModel) return;
                viewModel.CommitPlayheadPosition();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // Ctrl+wheel zooms the timeline in/out; plain wheel scrolls it horizontally. Plain
        // MouseWheel over a ScrollViewer with vertical scrolling disabled doesn't scroll
        // anything by default in WPF (wheel input only drives vertical offset natively), so
        // both directions are handled here explicitly.
        private void TimelineScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                if (DataContext is not EditorTimelineViewModel viewModel) return;

                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    ICommand zoomCommand = e.Delta > 0 ? viewModel.ZoomInCommand : viewModel.ZoomOutCommand;
                    if (zoomCommand.CanExecute(null))
                        zoomCommand.Execute(null);
                }
                else
                {
                    TimelineScrollViewer.ScrollToHorizontalOffset(TimelineScrollViewer.HorizontalOffset - e.Delta);
                }
                e.Handled = true;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void SetVoiceover_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not MenuItem menuItem || menuItem.DataContext is not SceneTimelineSegment segment) return;
                OpenFileDialog openFileDialog = new()
                {
                    Filter = "Audio files (*.mp3;*.wav;*.wma;*.m4a;*.aac)|*.mp3;*.wav;*.wma;*.m4a;*.aac"
                };
                if (openFileDialog.ShowDialog() == true)
                    segment.Scene.VoiceoverPath = openFileDialog.FileName;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void ClearVoiceover_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not MenuItem menuItem || menuItem.DataContext is not SceneTimelineSegment segment) return;
                segment.Scene.VoiceoverPath = null;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }
    }
}
