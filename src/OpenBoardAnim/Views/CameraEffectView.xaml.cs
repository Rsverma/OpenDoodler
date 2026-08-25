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
        // Longest on-screen dimension of the Camera Output panel - smaller than the Framing
        // canvas since it's just a supporting "what will this actually look like" preview, not
        // the primary editing surface.
        private const double OutputMaxDisplayDimension = 220;
        private const double MinZoom = 0.2;
        private const double MaxZoom = 8.0;
        // Smallest a rectangle's displayed side is allowed to shrink to, in display pixels -
        // keeps it grabbable/visible during a resize drag.
        private const double MinDisplayRectSize = 20;

        private double _displayScale = 1;
        private double _outputDisplayScale = 1;
        private double _editorWidth;
        private double _editorHeight;
        // Suppresses the corner NumericUpDowns' ValueChanged handlers while this view is writing
        // to them programmatically (from a drag, or from another corner's own edit reconciling
        // the aspect-locked pair), so those writes don't loop back into re-processing themselves.
        private bool _syncingCornerInputs;

        public CameraEffectView()
        {
            InitializeComponent();
            EasingComboBox.ItemsSource = EnumHelper.EnumerateEnum<CameraEasing>();
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

                _outputDisplayScale = OutputMaxDisplayDimension / Math.Max(_editorWidth, _editorHeight);
                double outputDisplayWidth = _editorWidth * _outputDisplayScale;
                double outputDisplayHeight = _editorHeight * _outputDisplayScale;
                OutputPreviewCanvas.Width = outputDisplayWidth;
                OutputPreviewCanvas.Height = outputDisplayHeight;
                OutputSnapshotImage.Width = outputDisplayWidth;
                OutputSnapshotImage.Height = outputDisplayHeight;

                // Any edit to the effect (drag, corner field, Start/End Time, Easing) should
                // immediately re-scrub the Camera Output panel at whatever progress is currently
                // shown, not just when the slider itself moves.
                model.Effect.PropertyChanged += (s, args) => RefreshOutputPreview();
                RefreshOutputPreview();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // Applies the Camera Output panel's crop/zoom for the current ProgressSlider value -
        // the exact same math ExportRenderHandler.ApplyCameraState uses (ComputeTransform per
        // endpoint, Lerp between them through CameraTransformMath.ApplyEasing), just recomputed
        // here in this panel's own display-pixel space instead of editor-space/export-frame
        // space. Scale is a unit-independent ratio so it needs no conversion; feeding
        // ComputeTransform display-scaled focus points and a display-sized viewport makes its
        // Translate output land directly in this panel's own pixel space with no extra step.
        private void RefreshOutputPreview()
        {
            if (DataContext is not CameraEffectPromptModel model) return;
            CameraEffectModel effect = model.Effect;
            double viewportWidth = _editorWidth * _outputDisplayScale;
            double viewportHeight = _editorHeight * _outputDisplayScale;

            CameraTransform start = CameraTransformMath.ComputeTransform(
                effect.StartFocusX * _outputDisplayScale, effect.StartFocusY * _outputDisplayScale, effect.StartZoom,
                viewportWidth, viewportHeight);
            CameraTransform end = CameraTransformMath.ComputeTransform(
                effect.EndFocusX * _outputDisplayScale, effect.EndFocusY * _outputDisplayScale, effect.EndZoom,
                viewportWidth, viewportHeight);

            double eased = CameraTransformMath.ApplyEasing(ProgressSlider.Value, effect.Easing);
            double scale = Lerp(start.Scale, end.Scale, eased);
            OutputScaleTransform.ScaleX = scale;
            OutputScaleTransform.ScaleY = scale;
            OutputTranslateTransform.X = Lerp(start.TranslateX, end.TranslateX, eased);
            OutputTranslateTransform.Y = Lerp(start.TranslateY, end.TranslateY, eased);

            ProgressPercentText.Text = ProgressSlider.Value.ToString("P0");
        }

        private static double Lerp(double from, double to, double fraction) => from + (to - from) * fraction;

        private void ProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => RefreshOutputPreview();

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

        // Reads one edited corner field back into Start/EndFocusX/Y, translating the box along
        // that field's own axis only - the box's size (Zoom) never changes here, only from
        // dragging a resize handle, so an edit only ever needs to shift the OTHER field on the
        // SAME axis (X1 <-> X2, Y1 <-> Y2) to keep that edge's distance from the moved one fixed.
        // An X edit therefore never touches a Y field or vice versa, and Zoom (hence the box's
        // aspect-locked size) is left exactly as it was.
        private void UpdateCornerAnchored(bool isStart, CornerField field)
        {
            try
            {
                if (_syncingCornerInputs || DataContext is not CameraEffectPromptModel model) return;

                NumericUpDown x1 = isStart ? StartX1Input : EndX1Input;
                NumericUpDown y1 = isStart ? StartY1Input : EndY1Input;
                NumericUpDown x2 = isStart ? StartX2Input : EndX2Input;
                NumericUpDown y2 = isStart ? StartY2Input : EndY2Input;
                double rawZoom = isStart ? model.Effect.StartZoom : model.Effect.EndZoom;
                double zoom = Math.Clamp(rawZoom <= 0 ? 1 : rawZoom, MinZoom, MaxZoom);
                double halfWidth = _editorWidth / zoom / 2;
                double halfHeight = _editorHeight / zoom / 2;

                double focusX = isStart ? model.Effect.StartFocusX : model.Effect.EndFocusX;
                double focusY = isStart ? model.Effect.StartFocusY : model.Effect.EndFocusY;
                NumericUpDown fieldToResync;
                switch (field)
                {
                    case CornerField.X1:
                        focusX = x1.Value + halfWidth;
                        fieldToResync = x2;
                        break;
                    case CornerField.X2:
                        focusX = x2.Value - halfWidth;
                        fieldToResync = x1;
                        break;
                    case CornerField.Y1:
                        focusY = y1.Value + halfHeight;
                        fieldToResync = y2;
                        break;
                    default: // Y2
                        focusY = y2.Value - halfHeight;
                        fieldToResync = y1;
                        break;
                }

                if (isStart)
                {
                    model.Effect.StartFocusX = focusX;
                    model.Effect.StartFocusY = focusY;
                    PositionRect(StartRect, focusX, focusY, zoom);
                }
                else
                {
                    model.Effect.EndFocusX = focusX;
                    model.Effect.EndFocusY = focusY;
                    PositionRect(EndRect, focusX, focusY, zoom);
                }

                // Only the same-axis partner of the edited field needs correcting - the
                // cross-axis pair was never touched.
                _syncingCornerInputs = true;
                try
                {
                    fieldToResync.Value = field switch
                    {
                        CornerField.X1 => focusX + halfWidth,
                        CornerField.X2 => focusX - halfWidth,
                        CornerField.Y1 => focusY + halfHeight,
                        _ => focusY - halfHeight
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
