using Microsoft.Win32;

namespace OpenBoardAnim.Services
{
    public class FileDialogService : IFileDialogService
    {
        public string ShowSaveFileDialog(string filter, string defaultExt = null, string defaultFileName = null)
        {
            SaveFileDialog dialog = new()
            {
                Filter = filter,
                DefaultExt = defaultExt,
                FileName = defaultFileName
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
