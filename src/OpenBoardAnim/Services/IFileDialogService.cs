namespace OpenBoardAnim.Services
{
    public interface IFileDialogService
    {
        // Returns the chosen path, or null if the user canceled.
        string ShowSaveFileDialog(string filter, string defaultExt = null, string defaultFileName = null);
    }
}
