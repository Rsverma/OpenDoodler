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

        [Theory]
        [MemberData(nameof(NonNoneOptions))]
        public void EveryOtherOption_HasAThumbnail(HandStyleOption option)
        {
            Assert.False(string.IsNullOrWhiteSpace(option.ThumbnailUri));
        }

        public static IEnumerable<object[]> NonNoneOptions() =>
            HandStyleOptions.All.Where(o => o.Style != HandStyle.None).Select(o => new object[] { o });
    }
}
