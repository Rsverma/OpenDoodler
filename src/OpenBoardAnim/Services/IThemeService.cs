using OpenBoardAnim.Models;
using System.Windows.Input;

namespace OpenBoardAnim.Services
{
    public interface IThemeService
    {
        AppTheme CurrentTheme { get; set; }
        ICommand SetThemeCommand { get; }
        void ApplySkin();
    }
}
