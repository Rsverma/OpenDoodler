using OpenBoardAnim.Models;

namespace OpenBoardAnim.Utils
{
    // One-click export presets shown above the raw Aspect Ratio combo in ProjectSettingsView - each
    // just sets ProjectSettings.AspectRatio (the sole source of ExportWidth/Height, see
    // ProjectDetails.cs) under a name the target platform actually uses, so users don't need to know
    // e.g. TikTok/Instagram Reel/YouTube Shorts all share the same 9:16 aspect ratio. Width/Height
    // here are for display only (button subtitle) and must stay in sync with
    // ProjectSettings.ExportWidth/Height - see ExportPresetsTests for a guard against drift.
    public record ExportPresetOption(string Name, AspectRatioPreset AspectRatio, int Width, int Height);

    public static class ExportPresets
    {
        public static IReadOnlyList<ExportPresetOption> All { get; } = new List<ExportPresetOption>
        {
            new("YouTube", AspectRatioPreset.Widescreen16x9, 1920, 1080),
            new("YouTube Shorts", AspectRatioPreset.Vertical9x16, 1080, 1920),
            new("Instagram Reel", AspectRatioPreset.Vertical9x16, 1080, 1920),
            new("Instagram Post", AspectRatioPreset.Square1x1, 1080, 1080),
            new("TikTok", AspectRatioPreset.Vertical9x16, 1080, 1920),
            new("Facebook Feed", AspectRatioPreset.Square1x1, 1080, 1080),
        };
    }
}
