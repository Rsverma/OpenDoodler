namespace OpenBoardAnim.Services
{
    public interface IOpenFileDialogService
    {
        // Returns the chosen file path(s), or an empty array if the user canceled.
        string[] ShowOpenFileDialog(string filter, bool multiselect = false);
    }
}
