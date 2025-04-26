using OpenBoardAnim.Core;
using OpenBoardAnim.Services;
using OpenBoardAnim.Utilities;

namespace OpenBoardAnim.ViewModels
{
    public class MainViewModel : ViewModel
    {
        private INavigationService _navigation;
        private string _title;
        private string _userName;

        public MainViewModel(INavigationService navService)
        {
            try
            {
                Title = "Open Board Animator";
                UserName = "RSV";
                Navigation = navService;
                NavigateToLaunchCommand = new RelayCommand(
                    execute: o => { Navigation.NavigateTo<LaunchViewModel>(); },
                    canExecute: o => true);
                NavigateToEditorCommand = new RelayCommand(
                    execute: o => { Navigation.NavigateTo<EditorViewModel>(); },
                    canExecute: o => true);
                NavigateToLaunchCommand.Execute(this);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }
        public RelayCommand NavigateToLaunchCommand { get; set; }
        public RelayCommand NavigateToEditorCommand { get; set; }
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
