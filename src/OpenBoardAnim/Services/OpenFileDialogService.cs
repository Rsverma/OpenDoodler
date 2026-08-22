using Microsoft.Win32;

namespace OpenBoardAnim.Services
{
    public class OpenFileDialogService : IOpenFileDialogService
    {
        public string[] ShowOpenFileDialog(string filter, bool multiselect = false)
        {
            OpenFileDialog dialog = new()
            {
                Filter = filter,
                Multiselect = multiselect
            };
            return dialog.ShowDialog() == true ? dialog.FileNames : [];
        }
    }
}
