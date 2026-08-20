using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OpenBoardAnim.Utils
{
    // Shows/hides an element based on whether a bound enum value's name matches the
    // ConverterParameter string, e.g. ConverterParameter="HandDrawn" against EntranceStyle.
    public class EnumEqualsVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isMatch = string.Equals(value?.ToString(), parameter as string, StringComparison.OrdinalIgnoreCase);
            return isMatch ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
