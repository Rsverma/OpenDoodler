using OpenBoardAnim.Models;
using OpenBoardAnim.Utils;
using Xunit;

namespace OpenBoardAnim.Tests
{
    public class HandStyleOptionsTests
    {
        [Fact]
        public void All_HasExactlyOneEntryPerHandStyleValue()
        {
            List<HandStyle> optionStyles = HandStyleOptions.All.Select(o => o.Style).ToList();
            List<HandStyle> allStyles = Enum.GetValues<HandStyle>().ToList();

            Assert.Equal(allStyles.OrderBy(s => s), optionStyles.OrderBy(s => s));
        }

        [Fact]
        public void None_HasNoThumbnail()
        {
            HandStyleOption none = HandStyleOptions.All.Single(o => o.Style == HandStyle.None);

            Assert.Null(none.ThumbnailUri);
        }

        [Fact]
        public void Custom_HasNoThumbnail()
        {
            // Custom's image is a per-project file path (ProjectSettings.CustomHandImagePath),
            // not a bundled resource, so there's no fixed ThumbnailUri for it here - see
            // SceneRenderHelpers.ResolveHandImage.
            HandStyleOption custom = HandStyleOptions.All.Single(o => o.Style == HandStyle.Custom);

            Assert.Null(custom.ThumbnailUri);
        }

        [Theory]
        [MemberData(nameof(NonNoneOptions))]
        public void EveryOtherOption_HasAThumbnail(HandStyleOption option)
        {
            Assert.False(string.IsNullOrWhiteSpace(option.ThumbnailUri));
        }

        public static IEnumerable<object[]> NonNoneOptions() =>
            HandStyleOptions.All.Where(o => o.Style != HandStyle.None && o.Style != HandStyle.Custom).Select(o => new object[] { o });
    }
}
