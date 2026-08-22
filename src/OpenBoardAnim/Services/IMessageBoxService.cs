using System.Windows;

namespace OpenBoardAnim.Services
{
    public interface IMessageBoxService
    {
        MessageBoxResult Show(string message, string caption, MessageBoxButton button, MessageBoxImage icon);
    }
}
