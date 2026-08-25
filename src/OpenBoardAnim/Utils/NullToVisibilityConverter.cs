using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OpenBoardAnim.Utils
{
    // Shows an element when the bound value is a non-empty string (or any non-null value),
    // hides it otherwise. Used for indicators that only appear once a value has been set.
    // ConverterParameter="Invert" flips that (shows only when null/empty) - for a placeholder
    // shown until a value is set, e.g. ProjectSettingsView's "Browse..." custom hand tile.
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasValue = value is string s ? !string.IsNullOrWhiteSpace(s) : value != null;
            bool invert = string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase);
            return hasValue != invert ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
