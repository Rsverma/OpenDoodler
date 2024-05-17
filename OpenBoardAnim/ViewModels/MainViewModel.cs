using OpenBoardAnim.Core;
using OpenBoardAnim.Services;

namespace OpenBoardAnim.ViewModels
{
    public class MainViewModel : ViewModel
    {
        private INavigationService _navigation;

        public MainViewModel(INavigationService navService) 
        {
            Navigation = navService;
            NavigateToLaunchCommand = new RelayCommand(
                execute: o => { Navigation.NavigateTo<LaunchViewModel>(); },
                canExecute: o => true);
            NavigateToEditorCommand = new RelayCommand(
                execute: o => { Navigation.NavigateTo<EditorViewModel>(); },
                canExecute: o => true);
            NavigateToLaunchCommand.Execute(this);
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
    }

}
