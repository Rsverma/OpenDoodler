using OpenBoardAnim.Models;
using OpenBoardAnim.Utils;
using Xunit;

namespace OpenBoardAnim.Tests
{
    public class ExportPresetsTests
    {
        [Theory]
        [MemberData(nameof(Presets))]
        public void Preset_WidthAndHeight_MatchProjectSettingsDerivedResolution(ExportPresetOption preset)
        {
            ProjectSettings settings = new() { AspectRatio = preset.AspectRatio };

            Assert.Equal(preset.Width, settings.ExportWidth);
            Assert.Equal(preset.Height, settings.ExportHeight);
        }

        [Fact]
        public void All_HasNoDuplicateNames()
        {
            List<string> names = ExportPresets.All.Select(p => p.Name).ToList();

            Assert.Equal(names.Distinct().Count(), names.Count);
        }

        public static IEnumerable<object[]> Presets() => ExportPresets.All.Select(p => new object[] { p });
    }
}
