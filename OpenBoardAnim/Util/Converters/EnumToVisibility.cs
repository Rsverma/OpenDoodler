﻿using System;
using System.Windows;
using System.Windows.Data;

namespace OpenBoardAnim.Util.Converters
{
    public class EnumToVisibility: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.Equals(parameter) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.Equals(Visibility.Visible) ? parameter : Binding.DoNothing;
        }
    }
}
