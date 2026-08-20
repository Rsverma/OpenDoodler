using Microsoft.Win32;
using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using System;
using System.IO;
using System.Windows;

namespace OpenBoardAnim.Services
{
    // Swaps HandyControl's skin resource dictionary at runtime between the light and dark
    // palettes. Every HandyControl/MaterialDesign brush the app uses is DynamicResource-bound
    // to keys the skin dictionary defines, so this single swap re-skins the whole app without
    // touching individual views.
    public class ThemeService : ObservableObject
    {
        private static readonly string SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenBoardAnim.theme.txt");
        private static readonly Uri LightSkinUri = new("pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml");
        private static readonly Uri DarkSkinUri = new("pack://application:,,,/HandyControl;component/Themes/SkinDark.xaml");
        private static readonly Uri ThemeUri = new("pack://application:,,,/HandyControl;component/Themes/Theme.xaml");

        private AppTheme _currentTheme = AppTheme.Light;

        public ThemeService()
        {
            try
            {
                if (File.Exists(SettingsFilePath) &&
                    Enum.TryParse(File.ReadAllText(SettingsFilePath), out AppTheme saved))
                    _currentTheme = saved;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to load saved theme, defaulting to Light: {ex.Message}");
            }

            SetThemeCommand = new RelayCommand(
                execute: o => CurrentTheme = (AppTheme)o,
                canExecute: o => true);
        }

        public AppTheme CurrentTheme
        {
            get => _currentTheme;
            set
            {
                if (_currentTheme == value) return;
                _currentTheme = value;
                OnPropertyChanged();
                ApplySkin();
                Persist();
            }
        }

        public RelayCommand SetThemeCommand { get; }

        // Called once at startup (before the main window is shown) to apply the persisted
        // choice, and again whenever CurrentTheme changes afterward.
        public void ApplySkin()
        {
            bool useDark = _currentTheme == AppTheme.Dark ||
                (_currentTheme == AppTheme.System && IsSystemInDarkMode());
            Uri skinUri = useDark ? DarkSkinUri : LightSkinUri;

            // Replace both the color dictionary AND Theme.xaml with brand-new instances, not
            // just the colors. HandyControl's brushes (RegionBrush etc.) are SolidColorBrush
            // objects defined once in Theme.xaml with Color="{DynamicResource RegionColor}" -
            // once such a Freezable brush is shared across multiple consumers (which happens
            // immediately, e.g. every panel using RegionBrush shares the same object), its own
            // resource-lookup context can get pinned and stop tracking further dictionary
            // swaps, even for brand-new consumers, since they all resolve to that same
            // already-stuck object. Rebuilding Theme.xaml fresh forces every derived brush to
            // be a new object resolved against the new colors from the start.
            var dictionaries = Application.Current.Resources.MergedDictionaries;
            for (int i = 0; i < dictionaries.Count; i++)
            {
                Uri source = dictionaries[i].Source;
                if (source == null) continue;
                if (source.OriginalString.Contains("/Themes/Skin"))
                    dictionaries[i] = new ResourceDictionary { Source = skinUri };
                else if (source.OriginalString.EndsWith("/Themes/Theme.xaml"))
                    dictionaries[i] = new ResourceDictionary { Source = ThemeUri };
            }
        }

        private void Persist()
        {
            try
            {
                File.WriteAllText(SettingsFilePath, _currentTheme.ToString());
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to persist theme choice: {ex.Message}");
            }
        }

        // Read once (not live-tracked) - System resolves to whatever Windows' light/dark app
        // theme is at the moment the app starts or the user picks "Match System".
        private static bool IsSystemInDarkMode()
        {
            try
            {
                using RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to read system theme preference, defaulting to light: {ex.Message}");
                return false;
            }
        }
    }
}
