using System.Windows;

namespace OpenBoardAnim.Services
{
    public class ApplicationService : IApplicationService
    {
        public void CloseMainWindow() => Application.Current.MainWindow?.Close();
    }
}
