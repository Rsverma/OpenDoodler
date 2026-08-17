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
