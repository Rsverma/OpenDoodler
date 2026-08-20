using System;
using System.Globalization;
using System.Windows.Data;

namespace OpenBoardAnim.Utils
{
    // Offsets a bound coordinate left by half of ConverterParameter (a pixel size), so a
    // center-anchored value (e.g. the playhead's X) can position an element by its own width.
    public class SubtractHalfConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double center = System.Convert.ToDouble(value, culture);
            double size = parameter != null ? System.Convert.ToDouble(parameter, culture) : 0;
            return center - size / 2;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
