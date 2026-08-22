using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using OpenBoardAnim.Utilities;
using System.ComponentModel;

namespace OpenBoardAnim.ViewModels
{
    public class MainViewModel : ViewModel
    {
        // Base app name shown in the window header when no project is open, and appended
        // after the project name/unsaved marker once one is.
        private const string AppName = "Open Board Animator";

        private readonly IPubSubService _pubSub;
        private readonly StateSnapshotService _stateSnapshotService;
        private readonly ICacheService _cache;
        private readonly EditorActionsViewModel _actions;
        private readonly IOpenFileDialogService _openFileDialog;
        private readonly IApplicationService _application;
        private INavigationService _navigation;
        private string _title;
        private string _userName;

        public MainViewModel(INavigationService navService, IPubSubService pubSub, StateSnapshotService stateSnapshotService, ICacheService cache, EditorActionsViewModel actions, IThemeService theme,
            IOpenFileDialogService openFileDialog, IApplicationService application)
        {
            try
            {
                _pubSub = pubSub;
                _stateSnapshotService = stateSnapshotService;
                _cache = cache;
                _actions = actions;
                _openFileDialog = openFileDialog;
                _application = application;
                _actions.PropertyChanged += Actions_PropertyChanged;
                Theme = theme;
                Title = AppName;
                UpdateWindowTitle();
                UserName = "RSV";
                Navigation = navService;
                NavigateToLaunchCommand = new RelayCommand(
                    execute: o => { Navigation.NavigateTo<LaunchViewModel>(); },
                    canExecute: o => true);
                NavigateToEditorCommand = new RelayCommand(
                    execute: o => { Navigation.NavigateTo<EditorViewModel>(); },
                    canExecute: o => true);
                UndoCommand = new RelayCommand(
                    execute: o => RestoreState(_stateSnapshotService.Undo()),
                    canExecute: o => _stateSnapshotService.CanUndo);
                RedoCommand = new RelayCommand(
                    execute: o => RestoreState(_stateSnapshotService.Redo()),
                    canExecute: o => _stateSnapshotService.CanRedo);
                NewProjectCommand = new RelayCommand(execute: o => NewProject(), canExecute: o => true);
                OpenProjectCommand = new RelayCommand(execute: o => OpenProject(), canExecute: o => true);
                ExitCommand = new RelayCommand(execute: o => _application.CloseMainWindow(), canExecute: o => true);
                NavigateToLaunchCommand.Execute(this);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // Keeps the window header in sync with whichever project is currently open and
        // whether it has unsaved changes - Project reassignment (open/new/close) and
        // HasUnsavedChanges both notify through here (see EditorActionsViewModel).
        private void Actions_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EditorActionsViewModel.Project) || e.PropertyName == nameof(EditorActionsViewModel.HasUnsavedChanges))
                UpdateWindowTitle();
        }

        private void UpdateWindowTitle()
        {
            string projectTitle = _actions.Project?.Title;
            if (string.IsNullOrWhiteSpace(projectTitle))
            {
                ProjectStatusText = "";
                return;
            }

            string unsavedMarker = _actions.HasUnsavedChanges ? "* " : "";
            ProjectStatusText = $"{unsavedMarker}{projectTitle}";
        }

        private void RestoreState(ProjectDetails project)
        {
            if (project != null)
                _pubSub.Publish(SubTopic.ProjectStateRestored, project);
        }

        private void NewProject()
        {
            try
            {
                if (!_actions.ConfirmDiscardUnsavedChanges())
                    return;

                Navigation.NavigateTo<EditorViewModel>();
                _pubSub.Publish(SubTopic.ProjectLaunched, new ProjectDetails());
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void OpenProject()
        {
            try
            {
                if (!_actions.ConfirmDiscardUnsavedChanges())
                    return;

                string[] paths = _openFileDialog.ShowOpenFileDialog("Project file (*.obap)|*.obap");
                if (paths.Length == 0)
                    return;

                ProjectDetails project = _cache.LoadProjectFromFile(paths[0]);
                if (project == null)
                    return;

                Navigation.NavigateTo<EditorViewModel>();
                _pubSub.Publish(SubTopic.ProjectLaunched, project);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // Called from MainWindow's Closing event so both the File > Exit menu item
        // (which just closes the window) and the native close button/Alt+F4 go
        // through the same unsaved-changes check.
        public bool ConfirmExit()
        {
            return _actions.ConfirmDiscardUnsavedChanges();
        }

        public RelayCommand NavigateToLaunchCommand { get; set; }
        public RelayCommand NavigateToEditorCommand { get; set; }
        public RelayCommand UndoCommand { get; set; }
        public RelayCommand RedoCommand { get; set; }
        public RelayCommand NewProjectCommand { get; set; }
        public RelayCommand OpenProjectCommand { get; set; }
        public RelayCommand ExitCommand { get; set; }
        public IThemeService Theme { get; private set; }
        public INavigationService Navigation
        {
            get => _navigation;
            set
            {
                _navigation = value;
                OnPropertyChanged();
            }
        }
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }
        private string _projectStatusText = "";
        // Shown centered in the title bar: the open project's name (with an unsaved-changes
        // marker) in place of the old static "Editor" label, or blank when no project is open.
        public string ProjectStatusText
        {
            get => _projectStatusText;
            set
            {
                _projectStatusText = value;
                OnPropertyChanged();
            }
        }
        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                OnPropertyChanged();
            }
        }
    }

}
