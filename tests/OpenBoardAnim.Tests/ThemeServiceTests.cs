using OpenBoardAnim.Services;
using Xunit;

namespace OpenBoardAnim.Tests
{
    // ThemeService's CurrentTheme setter and ApplySkin() both reach into
    // Application.Current.Resources.MergedDictionaries, which requires a live WPF Application
    // that doesn't exist in a headless test host, and its persisted-theme file path is a
    // hardcoded %LocalAppData% location rather than an injectable one (same as
    // CacheService's autosave backup path) - so only the command-wiring surface that doesn't
    // touch either of those is covered here.
    public class ThemeServiceTests
    {
        [Fact]
        public void Constructor_DoesNotThrow_AndExposesAReadySetThemeCommand()
        {
            ThemeService sut = new();

            Assert.NotNull(sut.SetThemeCommand);
            Assert.True(sut.SetThemeCommand.CanExecute(null));
        }
    }
}
