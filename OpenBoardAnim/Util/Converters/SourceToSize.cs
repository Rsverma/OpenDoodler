﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace OpenBoardAnim.Util.Converters
{
    public class SourceToSize : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var image = value as BitmapImage;
            var param = parameter as string;

            if (image == null || String.IsNullOrEmpty(param))
                return value;

            return param.Equals("width") ? image.Width : image.Height;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
