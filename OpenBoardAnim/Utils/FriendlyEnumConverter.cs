using System;
using System.Globalization;
using System.Windows.Data;

namespace OpenBoardAnim.Utils
{
    // Turns PascalCase enum member names (EntranceStyle, AspectRatioPreset) into
    // display text for combo boxes, e.g. "Vertical9x16" -> "Vertical (9:16)".
    public class FriendlyEnumConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "HandDrawn" => "Hand Drawn",
                "FadeIn" => "Fade In",
                "PopIn" => "Pop In",
                "Widescreen16x9" => "Widescreen (16:9)",
                "Vertical9x16" => "Vertical (9:16)",
                "Square1x1" => "Square (1:1)",
                _ => value?.ToString()
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
