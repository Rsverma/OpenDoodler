using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OpenBoardAnim.Utils
{
    // Inverse of EnumEqualsVisibilityConverter - shows the element when the bound enum
    // value's name does NOT match the ConverterParameter string, e.g. hiding a
    // transition-duration control only when SceneTransition is "None".
    public class EnumNotEqualsVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isMatch = string.Equals(value?.ToString(), parameter as string, StringComparison.OrdinalIgnoreCase);
            return isMatch ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
