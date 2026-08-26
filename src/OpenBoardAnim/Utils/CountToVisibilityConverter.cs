using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OpenBoardAnim.Utils
{
    // Shows an element when the bound collection count (or int) is greater than zero, hides it
    // otherwise. Used for indicators that only appear once a list has at least one item. Pass
    // ConverterParameter="Invert" to flip it (e.g. an "empty list" hint shown only at zero).
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int count = value is int i ? i : 0;
            bool hasItems = count > 0;
            if (string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase))
                hasItems = !hasItems;
            return hasItems ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
