using HandyControl.Controls;
using HandyControl.Data;
using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace OpenBoardAnim.Views
{
    /// <summary>
    /// Interaction logic for CameraEffectView.xaml
    /// </summary>
    public partial class CameraEffectView : UserControl
    {
        // Longest on-screen dimension the mini scene preview is scaled to fit within.
        private const double MaxDisplayDimension = 480;
        private const double MinZoom = 0.2;
        private const double MaxZoom = 8.0;
        // Smallest a rectangle's displayed side is allowed to shrink to, in display pixels -
        // keeps it grabbable/visible during a resize drag.
        private const double MinDisplayRectSize = 20;

        private double _displayScale = 1;
        private double _editorWidth;
        private double _editorHeight;
        // Suppresses the corner NumericUpDowns' ValueChanged handlers while this view is writing
        // to them programmatically (from a drag, or from another corner's own edit reconciling
        // the aspect-locked pair), so those writes don't loop back into re-processing themselves.
        private bool _syncingCornerInputs;

        public CameraEffectView()
        {
            InitializeComponent();
            Loaded += CameraEffectView_Loaded;
        }

        private void CameraEffectView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is not CameraEffectPromptModel model) return;

                _editorWidth = model.EditorWidth > 0 ? model.EditorWidth : 1920;
                _editorHeight = model.EditorHeight > 0 ? model.EditorHeight : 1080;
                _displayScale = MaxDisplayDimension / Math.Max(_editorWidth, _editorHeight);

                double displayWidth = _editorWidth * _displayScale;
                double displayHeight = _editorHeight * _displayScale;
                PreviewCanvas.Width = displayWidth;
                PreviewCanvas.Height = displayHeight;
                SnapshotImage.Width = displayWidth;
                SnapshotImage.Height = displayHeight;

                PositionRect(StartRect, model.Effect.StartFocusX, model.Effect.StartFocusY, model.Effect.StartZoom);
                PositionRect(EndRect, model.Effect.EndFocusX, model.Effect.EndFocusY, model.Effect.EndZoom);
                SyncCornerInputsFromModel(isStart: true);
                SyncCornerInputsFromModel(isStart: false);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // Sizes/positions a rectangle in display pixels from its editor-space focus point + zoom.
        private void PositionRect(Border rect, double focusX, double focusY, double zoom)
        {
            double zoomClamped = Math.Clamp(zoom <= 0 ? 1 : zoom, MinZoom, MaxZoom);
            double width = _editorWidth / zoomClamped * _displayScale;
            double height = _editorHeight / zoomClamped * _displayScale;
            double centerX = focusX * _displayScale;
            double centerY = focusY * _displayScale;

            rect.Width = width;
            rect.Height = height;
            Canvas.SetLeft(rect, centerX - width / 2);
            Canvas.SetTop(rect, centerY - height / 2);
        }

        // Writes a rectangle's current editor-space corner points (top-left/bottom-right) into
        // its four NumericUpDown fields, guarded so those writes don't re-trigger the
        // ValueChanged handlers that feed edits back the other way.
        private void SyncCornerInputsFromModel(bool isStart)
        {
            if (DataContext is not CameraEffectPromptModel model) return;

            double focusX = isStart ? model.Effect.StartFocusX : model.Effect.EndFocusX;
            double focusY = isStart ? model.Effect.StartFocusY : model.Effect.EndFocusY;
            double rawZoom = isStart ? model.Effect.StartZoom : model.Effect.EndZoom;
            double zoom = Math.Clamp(rawZoom <= 0 ? 1 : rawZoom, MinZoom, MaxZoom);
            double width = _editorWidth / zoom;
            double height = _editorHeight / zoom;

            _syncingCornerInputs = true;
            try
            {
                NumericUpDown x1 = isStart ? StartX1Input : EndX1Input;
                NumericUpDown y1 = isStart ? StartY1Input : EndY1Input;
                NumericUpDown x2 = isStart ? StartX2Input : EndX2Input;
                NumericUpDown y2 = isStart ? StartY2Input : EndY2Input;
                x1.Value = focusX - width / 2;
                y1.Value = focusY - height / 2;
                x2.Value = focusX + width / 2;
                y2.Value = focusY + height / 2;
            }
            finally
            {
                _syncingCornerInputs = false;
            }
        }

        // Which of a rectangle's four corner fields was just edited.
        private enum CornerField { X1, Y1, X2, Y2 }

        // Reads a rectangle's four corner fields back into Start/EndFocusX/Y/Zoom. Anchors the
        // OPPOSITE corner (kept exactly as typed/previously shown) and only ever recomputes the
        // OTHER coordinate of the SAME corner just edited, to satisfy the aspect lock (the camera
        // model only supports a single uniform scale, so the rectangle's aspect ratio always
        // matches the canvas's) - e.g. editing X1 can adjust Y1, but never X2 or Y2. This mirrors
        // a normal drag-a-corner-handle resize instead of every field changing at once.
        private void UpdateCornerAnchored(bool isStart, CornerField field)
        {
            try
            {
                if (_syncingCornerInputs || DataContext is not CameraEffectPromptModel model) return;

                NumericUpDown x1 = isStart ? StartX1Input : EndX1Input;
                NumericUpDown y1 = isStart ? StartY1Input : EndY1Input;
                NumericUpDown x2 = isStart ? StartX2Input : EndX2Input;
                NumericUpDown y2 = isStart ? StartY2Input : EndY2Input;
                double currentZoom = isStart ? model.Effect.StartZoom : model.Effect.EndZoom;

                double newX1 = x1.Value, newY1 = y1.Value, newX2 = x2.Value, newY2 = y2.Value;
                double zoom;
                NumericUpDown fieldToResync;
                switch (field)
                {
                    case CornerField.X1:
                        zoom = ClampZoom(newX2 - newX1, _editorWidth, currentZoom);
                        newY1 = newY2 - _editorHeight / zoom;
                        fieldToResync = y1;
                        break;
                    case CornerField.Y1:
                        zoom = ClampZoom(newY2 - newY1, _editorHeight, currentZoom);
                        newX1 = newX2 - _editorWidth / zoom;
                        fieldToResync = x1;
                        break;
                    case CornerField.X2:
                        zoom = ClampZoom(newX2 - newX1, _editorWidth, currentZoom);
                        newY2 = newY1 + _editorHeight / zoom;
                        fieldToResync = y2;
                        break;
                    default: // Y2
                        zoom = ClampZoom(newY2 - newY1, _editorHeight, currentZoom);
                        newX2 = newX1 + _editorWidth / zoom;
                        fieldToResync = x2;
                        break;
                }

                double focusX = (newX1 + newX2) / 2;
                double focusY = (newY1 + newY2) / 2;

                if (isStart)
                {
                    model.Effect.StartFocusX = focusX;
                    model.Effect.StartFocusY = focusY;
                    model.Effect.StartZoom = zoom;
                    PositionRect(StartRect, focusX, focusY, zoom);
                }
                else
                {
                    model.Effect.EndFocusX = focusX;
                    model.Effect.EndFocusY = focusY;
                    model.Effect.EndZoom = zoom;
                    PositionRect(EndRect, focusX, focusY, zoom);
                }

                // Only the perpendicular partner of the edited field needs correcting - the
                // opposite corner's pair was the anchor and was never touched.
                _syncingCornerInputs = true;
                try
                {
                    fieldToResync.Value = field switch
                    {
                        CornerField.X1 => newY1,
                        CornerField.Y1 => newX1,
                        CornerField.X2 => newY2,
                        _ => newX2
                    };
                }
                finally
                {
                    _syncingCornerInputs = false;
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private double ClampZoom(double newSize, double editorDimension, double fallbackZoom)
        {
            double zoom = newSize > 0 ? editorDimension / newSize : fallbackZoom;
            return Math.Clamp(zoom, MinZoom, MaxZoom);
        }

        private void StartX1_ValueChanged(object sender, FunctionEventArgs<double> e) => UpdateCornerAnchored(isStart: true, CornerField.X1);
        private void StartY1_ValueChanged(object sender, FunctionEventArgs<double> e) => UpdateCornerAnchored(isStart: true, CornerField.Y1);
        private void StartX2_ValueChanged(object sender, FunctionEventArgs<double> e) => UpdateCornerAnchored(isStart: true, CornerField.X2);
        private void StartY2_ValueChanged(object sender, FunctionEventArgs<double> e) => UpdateCornerAnchored(isStart: true, CornerField.Y2);
        private void EndX1_ValueChanged(object sender, FunctionEventArgs<double> e) => UpdateCornerAnchored(isStart: false, CornerField.X1);
        private void EndY1_ValueChanged(object sender, FunctionEventArgs<double> e) => UpdateCornerAnchored(isStart: false, CornerField.Y1);
        private void EndX2_ValueChanged(object sender, FunctionEventArgs<double> e) => UpdateCornerAnchored(isStart: false, CornerField.X2);
        private void EndY2_ValueChanged(object sender, FunctionEventArgs<double> e) => UpdateCornerAnchored(isStart: false, CornerField.Y2);

        private void StartMoveThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            try
            {
                if (DataContext is not CameraEffectPromptModel model) return;
                MoveRect(StartRect, e);
                model.Effect.StartFocusX += e.HorizontalChange / _displayScale;
                model.Effect.StartFocusY += e.VerticalChange / _displayScale;
                SyncCornerInputsFromModel(isStart: true);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void EndMoveThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            try
            {
                if (DataContext is not CameraEffectPromptModel model) return;
                MoveRect(EndRect, e);
                model.Effect.EndFocusX += e.HorizontalChange / _displayScale;
                model.Effect.EndFocusY += e.VerticalChange / _displayScale;
                SyncCornerInputsFromModel(isStart: false);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private static void MoveRect(Border rect, DragDeltaEventArgs e)
        {
            Canvas.SetLeft(rect, Canvas.GetLeft(rect) + e.HorizontalChange);
            Canvas.SetTop(rect, Canvas.GetTop(rect) + e.VerticalChange);
        }

        private void StartResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            try
            {
                if (DataContext is not CameraEffectPromptModel model) return;
                model.Effect.StartZoom = ResizeRect(StartRect, e);
                SyncCornerInputsFromModel(isStart: true);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void EndResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            try
            {
                if (DataContext is not CameraEffectPromptModel model) return;
                model.Effect.EndZoom = ResizeRect(EndRect, e);
                SyncCornerInputsFromModel(isStart: false);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // Grows/shrinks rect from its own center (never shifting the focus point) and returns the
        // Zoom value the resulting size represents - dragging the corner outward makes the box
        // bigger (more of the scene visible, lower zoom), inward makes it smaller (tighter
        // framing, higher zoom). Both edges move together (2x the raw delta) to keep the center fixed.
        private double ResizeRect(Border rect, DragDeltaEventArgs e)
        {
            double centerX = Canvas.GetLeft(rect) + rect.Width / 2;
            double centerY = Canvas.GetTop(rect) + rect.Height / 2;

            double newWidth = Math.Max(MinDisplayRectSize, rect.Width + e.HorizontalChange * 2);
            double newHeight = newWidth * _editorHeight / _editorWidth;

            rect.Width = newWidth;
            rect.Height = newHeight;
            Canvas.SetLeft(rect, centerX - newWidth / 2);
            Canvas.SetTop(rect, centerY - newHeight / 2);

            double widthInEditorUnits = newWidth / _displayScale;
            return Math.Clamp(_editorWidth / widthInEditorUnits, MinZoom, MaxZoom);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is not CameraEffectPromptModel model) return;
                model.SaveEffect?.Invoke(model.Effect);
                System.Windows.Window.GetWindow(this)?.Close();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Window.GetWindow(this)?.Close();
        }
    }
}
