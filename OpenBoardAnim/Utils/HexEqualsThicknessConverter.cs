using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OpenBoardAnim.Utils
{
    // Drives the selection ring around a color-swatch button: returns a visible
    // border thickness when the bound hex string matches the swatch's own hex
    // (passed as ConverterParameter), otherwise zero.
    public class HexEqualsThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isSelected = string.Equals(value as string, parameter as string, StringComparison.OrdinalIgnoreCase);
            return new Thickness(isSelected ? 2 : 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
