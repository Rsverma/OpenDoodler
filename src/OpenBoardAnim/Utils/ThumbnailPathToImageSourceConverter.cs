using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace OpenBoardAnim.Utils
{
    // Loads a thumbnail PNG from disk for display, or null (leaving the Image blank) if the
    // path is empty or the file doesn't exist yet - e.g. a project saved before this feature
    // existed, or one whose capture failed. BitmapCacheOption.OnLoad reads the file fully and
    // releases the handle immediately, so a later re-save that overwrites the same thumbnail
    // path isn't blocked by this still holding it open.
    public class ThumbnailPathToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string path || string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;
            try
            {
                BitmapImage bitmap = new();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
