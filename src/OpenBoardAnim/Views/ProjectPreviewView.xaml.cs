using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using OpenBoardAnim.Utils;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OpenBoardAnim.Views
{
    /// <summary>
    /// Interaction logic for ProjectPreviewView.xaml
    /// </summary>
    public partial class ProjectPreviewView : UserControl
    {
        private PreviewPlaybackHandler _controller;
        // Guards TimelineSlider.Value assignments made from OnControllerTimeChanged (the
        // controller ticking or a resulting Seek) so they don't loop back into
        // TimelineSlider_ValueChanged as if the user had dragged it.
        private bool _syncingSlider;
        private bool _wasPlayingBeforeScrub;

        public ProjectPreviewView()
        {
            InitializeComponent();
            Loaded += ProjectPreviewView_Loaded;
            Unloaded += (s, e) => _controller?.Dispose();
        }

        // Built once per view instance (DataContext is already set by the time this fires - see
        // DialogService) rather than per Play click, since scrubbing needs a persistent
        // "where we currently are" that both dragging and auto-play read/write.
        private void ProjectPreviewView_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is not ProjectDetails project) return;
                _controller = new PreviewPlaybackHandler(project, PreviewCanvas);
                _controller.TimeChanged += OnControllerTimeChanged;
                _controller.PlaybackEnded += OnPlaybackEnded;
                TimelineSlider.Maximum = Math.Max(0.01, _controller.TotalDuration);
                UpdateTimeLabel(0);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_controller == null) return;
                if (_controller.IsPlaying)
                {
                    _controller.Pause();
                    PlayPauseButton.Content = "Play";
                }
                else
                {
                    _controller.Play();
                    PlayPauseButton.Content = "Pause";
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void OnPlaybackEnded()
        {
            PlayPauseButton.Content = "Play";
        }

        private void OnControllerTimeChanged(double time)
        {
            _syncingSlider = true;
            try
            {
                TimelineSlider.Value = time;
            }
            finally
            {
                _syncingSlider = false;
            }
            UpdateTimeLabel(time);
        }

        private void UpdateTimeLabel(double time)
        {
            TimeLabel.Text = $"{FormatTime(time)} / {FormatTime(_controller?.TotalDuration ?? 0)}";
        }

        private static string FormatTime(double seconds)
        {
            TimeSpan span = TimeSpan.FromSeconds(Math.Max(0, seconds));
            return span.Hours > 0 ? span.ToString(@"h\:mm\:ss") : span.ToString(@"m\:ss");
        }

        // A plain click on the slider's track (not a drag) also starts with
        // PreviewMouseLeftButtonDown, so this covers both "click to jump" and "drag to scrub"
        // with one pair of handlers.
        private void TimelineSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_controller == null) return;
            _wasPlayingBeforeScrub = _controller.IsPlaying;
            _controller.Pause();
            PlayPauseButton.Content = "Play";
        }

        private void TimelineSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_controller == null || !_wasPlayingBeforeScrub) return;
            _wasPlayingBeforeScrub = false;
            _controller.Play();
            PlayPauseButton.Content = "Pause";
        }

        private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_syncingSlider || _controller == null) return;
            _controller.Seek(e.NewValue);
        }
    }
}
