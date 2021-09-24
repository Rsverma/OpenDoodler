using OpenBoardAnim.AppConstants;
using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Data;

namespace OpenBoardAnim.Helpers
{
    public class BoardStyleBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            BoardStyle style = (BoardStyle)value;
            switch (style)
            {
                case BoardStyle.BlackBoard:
                    return Brushes.Black;

                case BoardStyle.GreenBoard:
                    return Brushes.Green;

                default:
                    return Brushes.White;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}