using System.Windows;

namespace OpenBoardAnim.Services
{
    public class MessageBoxService : IMessageBoxService
    {
        public MessageBoxResult Show(string message, string caption, MessageBoxButton button, MessageBoxImage icon)
            => MessageBox.Show(message, caption, button, icon);
    }
}
