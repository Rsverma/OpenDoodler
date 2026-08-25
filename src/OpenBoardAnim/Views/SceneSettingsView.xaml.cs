using Microsoft.Win32;
using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using System;
using System.Windows;
using System.Windows.Controls;

namespace OpenBoardAnim.Views
{
    /// <summary>
    /// Interaction logic for SceneSettingsView.xaml
    /// </summary>
    public partial class SceneSettingsView : UserControl
    {
        public SceneSettingsView()
        {
            InitializeComponent();
            TransitionOverrideComboBox.ItemsSource = EnumHelper.EnumerateEnum<SceneTransitionOverride>();
        }

        private void BrowseVoiceover_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is not SceneModel scene) return;
                OpenFileDialog openFileDialog = new()
                {
                    Filter = "Audio files (*.mp3;*.wav;*.wma;*.m4a;*.aac)|*.mp3;*.wav;*.wma;*.m4a;*.aac"
                };
                if (openFileDialog.ShowDialog() == true)
                    scene.VoiceoverPath = openFileDialog.FileName;
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
                if (DataContext is SceneModel scene)
                    scene.VoiceoverPath = null;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }
    }
}
