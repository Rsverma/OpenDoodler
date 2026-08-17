using Microsoft.Win32;
using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using System;
using System.Windows;
using System.Windows.Controls;

namespace OpenBoardAnim.Views
{
    /// <summary>
    /// Interaction logic for ProjectSettingsView.xaml
    /// </summary>
    public partial class ProjectSettingsView : UserControl
    {
        public ProjectSettingsView()
        {
            InitializeComponent();
            EntranceStyleComboBox.ItemsSource = EnumHelper.EnumerateEnum<EntranceStyle>();
            AspectRatioComboBox.ItemsSource = EnumHelper.EnumerateEnum<AspectRatioPreset>();
            SceneTransitionComboBox.ItemsSource = EnumHelper.EnumerateEnum<SceneTransition>();
        }

        private void StrokeColor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is not ProjectDetails project) return;
                if (sender is Button button && button.Tag is string hex)
                    project.Settings.StrokeColorHex = hex;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void BrowseAudio_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is not ProjectDetails project) return;
                OpenFileDialog openFileDialog = new()
                {
                    Filter = "Audio files (*.mp3;*.wav;*.wma;*.m4a;*.aac)|*.mp3;*.wav;*.wma;*.m4a;*.aac"
                };
                if (openFileDialog.ShowDialog() == true)
                    project.AudioPath = openFileDialog.FileName;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void ClearAudio_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is ProjectDetails project)
                    project.AudioPath = null;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }
    }
}
