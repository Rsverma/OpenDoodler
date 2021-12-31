﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace OpenBoardAnim.Util.Converters
{
    /// <summary>
    /// Converts a Double value to a String representation of a percentage. 100 %
    /// </summary>
    public class DoubleToPercentage : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var doubleValue = value as double?;

            if (!doubleValue.HasValue)
                return DependencyProperty.UnsetValue;

            return doubleValue.Value + " %";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
