using Microsoft.Win32;
using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using OpenBoardAnim.Utilities;
using System.Windows;

namespace OpenBoardAnim.ViewModels
{
    public class MainViewModel : ViewModel
    {
        private readonly IPubSubService _pubSub;
        private readonly StateSnapshotService _stateSnapshotService;
        private readonly CacheService _cache;
        private readonly EditorActionsViewModel _actions;
        private INavigationService _navigation;
        private string _title;
        private string _userName;

        public MainViewModel(INavigationService navService, IPubSubService pubSub, StateSnapshotService stateSnapshotService, CacheService cache, EditorActionsViewModel actions, ThemeService theme)
        {
            try
            {
                _pubSub = pubSub;
                _stateSnapshotService = stateSnapshotService;
                _cache = cache;
                _actions = actions;
                Theme = theme;
                Title = "Open Board Animator";
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
                ExitCommand = new RelayCommand(execute: o => Application.Current.MainWindow?.Close(), canExecute: o => true);
                NavigateToLaunchCommand.Execute(this);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
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

                OpenFileDialog openFileDialog = new()
                {
                    Filter = "Project file (*.obap)|*.obap"
                };
                if (openFileDialog.ShowDialog() != true)
                    return;

                ProjectDetails project = _cache.LoadProjectFromFile(openFileDialog.FileName);
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
        public ThemeService Theme { get; private set; }
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
