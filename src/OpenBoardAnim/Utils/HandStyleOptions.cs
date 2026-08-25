using OpenBoardAnim.Models;

namespace OpenBoardAnim.Utils
{
    // ThumbnailUri is null for HandStyle.None (no image, hand cursor is hidden entirely) -
    // ProjectSettingsView's picker, PreviewPlaybackHandler, and ExportRenderHandler all read from
    // this single list so the HandStyle-to-file mapping only lives in one place.
    public record HandStyleOption(HandStyle Style, string Name, string ThumbnailUri);

    public static class HandStyleOptions
    {
        public static IReadOnlyList<HandStyleOption> All { get; } = new List<HandStyleOption>
        {
            new(HandStyle.LightSkin, "Light Skin", "pack://application:,,,/Resources/Light-Skin.png"),
            new(HandStyle.DarkSkin, "Dark Skin", "pack://application:,,,/Resources/Dark-Skin.png"),
            new(HandStyle.Cartoon, "Cartoon", "pack://application:,,,/Resources/Cartoon.png"),
            new(HandStyle.ScrapBook, "Scrap Book", "pack://application:,,,/Resources/ScrapBook.png"),
            new(HandStyle.None, "None", null),
        };
    }
}
