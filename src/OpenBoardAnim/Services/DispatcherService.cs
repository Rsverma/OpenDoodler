using System.Windows;

namespace OpenBoardAnim.Services
{
    public class DispatcherService : IDispatcherService
    {
        public void BeginInvoke(Action action) => Application.Current.Dispatcher.BeginInvoke(action);
    }
}
