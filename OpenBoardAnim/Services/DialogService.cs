using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using OpenBoardAnim.Views;
using System.Windows;

namespace OpenBoardAnim.Services
{
    public enum DialogType
    {
        SceneSettings,
        ProjectSettings,
        AboutUs,
        PreviewProject,
        SaveSceneTemplate,
        LibraryManager
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
                // dedicated view yet (AboutUs) fall back to showing the raw model.
                object content = dialogType switch
                {
                    DialogType.PreviewProject => new ProjectPreviewView { DataContext = model },
                    DialogType.ProjectSettings => new ProjectSettingsView { DataContext = model },
                    DialogType.SceneSettings => new SceneSettingsView { DataContext = model },
                    DialogType.SaveSceneTemplate => new SaveSceneTemplateView { DataContext = model },
                    DialogType.LibraryManager => new LibraryManagerView { DataContext = model },
                    _ => (object)model
                };

                DialogWindow dialog = new DialogWindow
                {
                    DataContext = content,
                    Owner = Application.Current.MainWindow,
                    Title = dialogType switch
                    {
                        DialogType.PreviewProject => "Preview",
                        DialogType.ProjectSettings => "Project Settings",
                        DialogType.SceneSettings => "Scene Settings",
                        DialogType.AboutUs => "About",
                        DialogType.SaveSceneTemplate => "Save Scene as Template",
                        DialogType.LibraryManager => "Library Manager",
                        _ => "OpenDoodler"
                    }
                };

                if (dialogType == DialogType.PreviewProject)
                {
                    // The preview canvas resizes to the project's aspect ratio (see
                    // ProjectSettings.EditorWidth/Height); size the window to match instead
                    // of leaving it at a fixed size the user could resize independently.
                    dialog.Width = double.NaN;
                    dialog.Height = double.NaN;
                    dialog.SizeToContent = SizeToContent.WidthAndHeight;
                    dialog.ResizeMode = ResizeMode.NoResize;
                }
                else if (dialogType == DialogType.ProjectSettings || dialogType == DialogType.SceneSettings ||
                         dialogType == DialogType.SaveSceneTemplate)
                {
                    // Settings content is a fixed-width, compact stack of cards (one of
                    // which - Stroke - can disappear entirely) - size to it directly
                    // rather than the dialog's default fixed 650x1050.
                    dialog.Width = double.NaN;
                    dialog.Height = double.NaN;
                    dialog.SizeToContent = SizeToContent.WidthAndHeight;
                }

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
