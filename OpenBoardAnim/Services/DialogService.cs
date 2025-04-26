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
                switch (dialogType)
                {
                    case DialogType.PreviewProject:
                    case DialogType.ProjectSettings:
                    case DialogType.SceneSettings:
                    case DialogType.AboutUs:
                        {
                            DialogWindow dialog = new DialogWindow
                            {
                                DataContext = model
                            };
                            return dialog.ShowDialog();
                        }
                }
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
