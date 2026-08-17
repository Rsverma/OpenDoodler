using OpenBoardAnim.Core;
using OpenBoardAnim.Services;
using OpenBoardAnim.Utilities;

namespace OpenBoardAnim.ViewModels
{
    public class MainViewModel : ViewModel
    {
        private readonly IPubSubService _pubSub;
        private readonly StateSnapshotService _stateSnapshotService;
        private INavigationService _navigation;
        private string _title;
        private string _userName;

        public MainViewModel(INavigationService navService, IPubSubService pubSub, StateSnapshotService stateSnapshotService)
        {
            try
            {
                _pubSub = pubSub;
                _stateSnapshotService = stateSnapshotService;
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
                NavigateToLaunchCommand.Execute(this);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void RestoreState(Models.ProjectDetails project)
        {
            if (project != null)
                _pubSub.Publish(SubTopic.ProjectStateRestored, project);
        }

        public RelayCommand NavigateToLaunchCommand { get; set; }
        public RelayCommand NavigateToEditorCommand { get; set; }
        public RelayCommand UndoCommand { get; set; }
        public RelayCommand RedoCommand { get; set; }
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
