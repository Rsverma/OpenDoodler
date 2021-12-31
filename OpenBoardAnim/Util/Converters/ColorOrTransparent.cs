﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace OpenBoardAnim.Util.Converters
{
    public class ColorOrTransparent : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var selection = value as bool?;

            if (!selection.HasValue)
                return new SolidColorBrush(Colors.Transparent);

            return new SolidColorBrush(selection.Value ? Colors.Transparent : Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
