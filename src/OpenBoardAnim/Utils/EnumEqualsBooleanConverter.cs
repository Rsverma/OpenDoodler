using System;
using System.Globalization;
using System.Windows.Data;

namespace OpenBoardAnim.Utils
{
    // Bool counterpart to EnumEqualsVisibilityConverter, for driving a checkable MenuItem's
    // IsChecked from whether a bound enum value's name matches the ConverterParameter string.
    public class EnumEqualsBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.Equals(value?.ToString(), parameter as string, StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
