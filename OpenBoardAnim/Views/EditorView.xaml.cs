
using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using OpenBoardAnim.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenBoardAnim.Views
{
    /// <summary>
    /// Interaction logic for EditorView.xaml
    /// </summary>
    public partial class EditorView : UserControl
    {
        // Extra room (on top of the ScrollViewer's own viewport size) kept around CanvasSlack
        // so there's always real scrollable extent to pan into, regardless of canvas/zoom size.
        private const double CanvasPanPadding = 400;

        // Tracks an in-progress middle-mouse-button pan gesture on CanvasScrollViewer.
        private Point? _panStart;
        private double _panStartHorizontalOffset;
        private double _panStartVerticalOffset;

        // CanvasSlack is always exactly CanvasPanPadding bigger than the viewport (see
        // CanvasScrollViewer_SizeChanged), split evenly by its centering alignment - so the
        // offset that puts CanvasBoard (centered within CanvasSlack) back in the middle of the
        // viewport is always exactly half of that padding, regardless of viewport size.
        private bool _hasCenteredCanvas;

        public EditorView()
        {
            InitializeComponent();
        }

        private void CanvasScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                if (sender is not ScrollViewer scrollViewer) return;
                CanvasSlack.MinWidth = e.NewSize.Width + CanvasPanPadding;
                CanvasSlack.MinHeight = e.NewSize.Height + CanvasPanPadding;

                // Only ever auto-center once, on first layout - if the user has since panned
                // around and then resizes the window, a resize shouldn't yank their view back
                // to center.
                if (!_hasCenteredCanvas && e.NewSize.Width > 0 && e.NewSize.Height > 0)
                {
                    CenterCanvasView(scrollViewer);
                    _hasCenteredCanvas = true;
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // Only valid while CanvasSlack is sized purely from its MinWidth/MinHeight (i.e. the
        // board isn't currently zoomed in far enough to outgrow that guaranteed slack) - true
        // right after a zoom reset back to 100%, which is the only place this is called from.
        private void CenterCanvasView(ScrollViewer scrollViewer)
        {
            scrollViewer.UpdateLayout();
            scrollViewer.ScrollToHorizontalOffset(CanvasPanPadding / 2);
            scrollViewer.ScrollToVerticalOffset(CanvasPanPadding / 2);
        }

        private void ResetZoomButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CenterCanvasView(CanvasScrollViewer);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void ZoomToFit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is not EditorViewModel viewModel) return;
                double boardWidth = viewModel.Actions.Project?.Settings?.EditorWidth ?? 0;
                double boardHeight = viewModel.Actions.Project?.Settings?.EditorHeight ?? 0;
                ZoomToFitBounds(viewModel, new Rect(0, 0, boardWidth, boardHeight));
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void ZoomToSelection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is not EditorViewModel viewModel) return;
                List<GraphicModelBase> selected = viewModel.Actions.GetSelectedGraphicsOrFallback();
                if (selected.Count == 0) return;

                double left = selected.Min(g => g.X);
                double top = selected.Min(g => g.Y);
                double right = selected.Max(g => g.X + g.Width);
                double bottom = selected.Max(g => g.Y + g.Height);
                ZoomToFitBounds(viewModel, new Rect(left, top, right - left, bottom - top));
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // Shared by Zoom to Fit (whole board) and Zoom to Selection (just the selected
        // graphics' bounding box) - computes a zoom level that fits canvasBounds (in unscaled
        // canvas units) into the current viewport, then re-centers on it using the same
        // TranslatePoint-based delta-correction approach as the cursor-anchored wheel zoom
        // above, just targeting the viewport's center instead of the cursor position.
        private void ZoomToFitBounds(EditorViewModel viewModel, Rect canvasBounds)
        {
            if (canvasBounds.Width <= 0 || canvasBounds.Height <= 0) return;

            double viewportWidth = CanvasScrollViewer.ViewportWidth;
            double viewportHeight = CanvasScrollViewer.ViewportHeight;
            if (viewportWidth <= 0 || viewportHeight <= 0) return;

            // A little breathing room around the fitted area instead of flush to the viewport edges.
            const double marginFactor = 0.9;
            double fitZoom = Math.Min(viewportWidth / canvasBounds.Width, viewportHeight / canvasBounds.Height) * marginFactor;
            viewModel.Canvas.ZoomLevel = fitZoom;

            CanvasScrollViewer.UpdateLayout();

            Point centerCanvasPos = new(canvasBounds.X + canvasBounds.Width / 2, canvasBounds.Y + canvasBounds.Height / 2);
            Point centerViewportPosAfterZoom = CanvasBoard.TranslatePoint(centerCanvasPos, CanvasScrollViewer);
            Point viewportCenter = new(viewportWidth / 2, viewportHeight / 2);
            Vector correction = centerViewportPosAfterZoom - viewportCenter;
            CanvasScrollViewer.ScrollToHorizontalOffset(CanvasScrollViewer.HorizontalOffset + correction.X);
            CanvasScrollViewer.ScrollToVerticalOffset(CanvasScrollViewer.VerticalOffset + correction.Y);
        }

        private void CanvasScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            try
            {
                if (sender is not ScrollViewer scrollViewer) return;
                if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) return;
                if (DataContext is not EditorViewModel viewModel) return;

                ICommand zoomCommand = e.Delta > 0 ? viewModel.Canvas.ZoomInCommand : viewModel.Canvas.ZoomOutCommand;
                if (zoomCommand.CanExecute(null))
                {
                    // Keep whatever canvas point is currently under the cursor anchored at the
                    // same spot in the viewport after the zoom changes, instead of zooming
                    // toward the content/viewport center. CanvasBoard's own coordinate space is
                    // always in unscaled canvas units regardless of the current zoom/scroll/
                    // centering state - GetPosition resolves that through the full visual
                    // transform chain (LayoutTransform included).
                    Point mouseViewportPos = e.GetPosition(scrollViewer);
                    Point anchorCanvasPos = e.GetPosition(CanvasBoard);

                    zoomCommand.Execute(null);

                    // The new ScaleTransform value only invalidates layout asynchronously -
                    // force a synchronous pass so the geometry read below (and the resulting
                    // scroll clamp) reflects the new zoom rather than the stale pre-zoom one.
                    scrollViewer.UpdateLayout();

                    // Where does the anchor point render in the viewport now that the zoom has
                    // changed, before any compensating scroll? TranslatePoint walks the actual
                    // visual transform chain (LayoutTransform, CanvasBoard's own Margin, content
                    // centering when smaller than the viewport, everything) instead of
                    // re-deriving that geometry by hand, so the correction below stays exact
                    // regardless of those details.
                    Point anchorViewportPosAfterZoom = CanvasBoard.TranslatePoint(anchorCanvasPos, scrollViewer);
                    Vector correction = anchorViewportPosAfterZoom - mouseViewportPos;
                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + correction.X);
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + correction.Y);
                }
                e.Handled = true;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
            // Without Ctrl, leave the event unhandled - the ScrollViewer's own default wheel
            // handling scrolls it vertically, giving free panning without any extra code here.
        }

        private void CanvasScrollViewer_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton != MouseButton.Middle) return;
                if (sender is not ScrollViewer scrollViewer) return;

                _panStart = e.GetPosition(scrollViewer);
                _panStartHorizontalOffset = scrollViewer.HorizontalOffset;
                _panStartVerticalOffset = scrollViewer.VerticalOffset;
                scrollViewer.CaptureMouse();
                e.Handled = true;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void CanvasScrollViewer_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                if (_panStart is not Point start) return;
                if (sender is not ScrollViewer scrollViewer) return;

                Point current = e.GetPosition(scrollViewer);
                scrollViewer.ScrollToHorizontalOffset(_panStartHorizontalOffset - (current.X - start.X));
                scrollViewer.ScrollToVerticalOffset(_panStartVerticalOffset - (current.Y - start.Y));
                e.Handled = true;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void CanvasScrollViewer_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (e.ChangedButton != MouseButton.Middle) return;
                if (sender is not ScrollViewer scrollViewer) return;

                _panStart = null;
                scrollViewer.ReleaseMouseCapture();
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
