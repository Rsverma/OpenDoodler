using Microsoft.Win32;
using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using OpenBoardAnim.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace OpenBoardAnim.Views
{
    /// <summary>
    /// Interaction logic for ProjectSettingsView.xaml
    /// </summary>
    public partial class ProjectSettingsView : UserControl
    {
        // Built once per view instance (not bound straight to the shared ExportPresets.All) so
        // IsSelected reflects only this view - see ExportPresetPickerItem.
        private readonly List<ExportPresetPickerItem> _presetItems;

        public ProjectSettingsView()
        {
            InitializeComponent();
            EntranceStyleComboBox.ItemsSource = EnumHelper.EnumerateEnum<EntranceStyle>();
            AspectRatioComboBox.ItemsSource = EnumHelper.EnumerateEnum<AspectRatioPreset>();
            SceneTransitionComboBox.ItemsSource = EnumHelper.EnumerateEnum<SceneTransition>();

            _presetItems = ExportPresets.All.Select(p => new ExportPresetPickerItem { Name = p.Name, Preset = p }).ToList();
            _presetItems.Add(new ExportPresetPickerItem { Name = "Custom" });
            ExportPresetsItemsControl.ItemsSource = _presetItems;

            DataContextChanged += ProjectSettingsView_DataContextChanged;
        }

        // Highlights whichever preset's aspect ratio matches the bound project's current one (the
        // first match wins - several presets can share an aspect ratio, e.g. TikTok/Instagram
        // Reel/YouTube Shorts are all 9:16), or falls back to Custom if somehow none match. A
        // brand-new project defaults to Widescreen16x9 (see ProjectSettings.AspectRatio), whose
        // first matching preset is YouTube - satisfying "YouTube selected by default" without
        // hardcoding an index, while still reflecting a reopened project's actual saved setting.
        private void ProjectSettingsView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (DataContext is not ProjectDetails project) return;
            ExportPresetPickerItem match = _presetItems.FirstOrDefault(p => p.Preset?.AspectRatio == project.Settings.AspectRatio);
            SelectPreset(match ?? _presetItems.Last());
        }

        private void SelectPreset(ExportPresetPickerItem item)
        {
            foreach (ExportPresetPickerItem p in _presetItems)
                p.IsSelected = false;
            item.IsSelected = true;
            AspectRatioComboBox.Visibility = item.IsCustom ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ExportPreset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is not ProjectDetails project) return;
                if (sender is not Button button || button.Tag is not ExportPresetPickerItem item) return;
                SelectPreset(item);
                if (item.Preset != null)
                    project.Settings.AspectRatio = item.Preset.AspectRatio;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
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
