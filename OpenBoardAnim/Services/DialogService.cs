using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using OpenBoardAnim.Views;

namespace OpenBoardAnim.Services
{
    public enum DialogType
    {
        SceneSettings,
        ProjectSettings,
        AboutUs,
        PreviewProject
    }
    public interface IDialogService
    {
        bool? ShowDialog<T>(DialogType dialogType, T model) where T : ObservableObject;
    }
    public class DialogService : IDialogService
    {
        private readonly Func<Type, ViewModel> _viewModelFactory;

        public DialogService(Func<Type, ViewModel> viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
        }

        public bool? ShowDialog<T>(DialogType dialogType, T model = null) where T : ObservableObject
        {
            try
            {
                // Content is picked by DialogType, not by the model's CLR type - PreviewProject
                // and ProjectSettings both take a ProjectDetails, so type-based routing (e.g. a
                // DataTemplate keyed on ProjectDetails) can't tell them apart. Types with no
                // dedicated view yet (SceneSettings, AboutUs) fall back to showing the raw model.
                object content = dialogType switch
                {
                    DialogType.PreviewProject => new ProjectPreviewView { DataContext = model },
                    DialogType.ProjectSettings => new ProjectSettingsView { DataContext = model },
                    _ => (object)model
                };

                DialogWindow dialog = new DialogWindow
                {
                    DataContext = content
                };
                return dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
            return null;
        }
    }
}
