using System.Windows;
using System.Windows.Media;

namespace OpenBoardAnim.Helpers
{
    public static class UserSettings
    {
        public static string LogsFolder { get; set; }
        public static string VersionText { get; set; }
        public static string FfmpegLocation { get; set; }
        public static string SharpDxLocationFolder { get; set; }
        public static bool FixedFrameRate { get; set; }
        public static bool UseDefaultOutput { get;  set; }
        public static string DefaultOutput { get;  set; }
        public static System.Windows.Media.Color InsertFillColor { get; set; } = System.Windows.Media.Colors.White;
        public static Color ClickColor { get;  set; }
        public static Color GridColor1 { get; set; } = Colors.WhiteSmoke;
        public static Color GridColor2 { get; set; } = Color.FromRgb(240, 240, 240);

        public static Rect GridSize { get; set; } = new Rect(0, 0, 20, 20);
        public static Color BoardGridColor1 { get; set; } = Colors.White;
        public static Color BoardGridColor2 { get; set; } = Colors.White;

        public static Rect BoardGridSize { get; set; } = new Rect(0, 0, 20, 20);

    }
}