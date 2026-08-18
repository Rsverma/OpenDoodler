using OpenBoardAnim.Utilities;
using OpenBoardAnim.Utils;
using System;
using System.Windows;
using System.Windows.Media;

namespace OpenBoardAnim.Controls
{
    // Lightweight FrameworkElement (not a full Control - no template/chrome needed) that draws
    // a small bar-style waveform for an audio file, sized to whatever it's placed in. Decoding
    // happens off the UI thread via WaveformCache and is fire-and-forget; a stale in-flight load
    // is discarded if AudioPath changes again before it completes.
    public class WaveformControl : FrameworkElement
    {
        public static readonly DependencyProperty AudioPathProperty = DependencyProperty.Register(
            nameof(AudioPath), typeof(string), typeof(WaveformControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnAudioPathChanged));

        public string AudioPath
        {
            get => (string)GetValue(AudioPathProperty);
            set => SetValue(AudioPathProperty, value);
        }

        public static readonly DependencyProperty WaveformBrushProperty = DependencyProperty.Register(
            nameof(WaveformBrush), typeof(Brush), typeof(WaveformControl),
            new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

        public Brush WaveformBrush
        {
            get => (Brush)GetValue(WaveformBrushProperty);
            set => SetValue(WaveformBrushProperty, value);
        }

        private float[] _peaks;

        private static void OnAudioPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not WaveformControl control) return;
            control._peaks = null;
            control.LoadPeaks((string)e.NewValue);
        }

        private async void LoadPeaks(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return;
                float[] peaks = await WaveformCache.GetPeaksAsync(path);
                if (AudioPath != path) return; // AudioPath changed again while decoding - discard
                _peaks = peaks;
                InvalidateVisual();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to load waveform: {ex.Message}");
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            float[] peaks = _peaks;
            double width = ActualWidth;
            double height = ActualHeight;
            if (peaks == null || peaks.Length == 0 || width <= 0 || height <= 0)
                return;

            Pen pen = new(WaveformBrush, 1);
            pen.Freeze();

            double midY = height / 2;
            int columns = Math.Max(1, (int)width);
            for (int x = 0; x < columns; x++)
            {
                int peakIndex = Math.Clamp((int)((double)x / columns * peaks.Length), 0, peaks.Length - 1);
                double barHeight = Math.Max(1, peaks[peakIndex] * height);
                drawingContext.DrawLine(pen, new Point(x, midY - barHeight / 2), new Point(x, midY + barHeight / 2));
            }
        }
    }
}
