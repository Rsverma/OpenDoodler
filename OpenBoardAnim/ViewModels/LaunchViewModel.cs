
using OpenBoardAnim.Core;
using OpenBoardAnim.Services;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Input;

namespace OpenBoardAnim.ViewModels
{
    public class LaunchViewModel : ViewModel
    {
        private BindingList<string> _recentProjects;
        private INavigationService _navigation;

        public LaunchViewModel(INavigationService navigation)
        {
            RecentProjects = new BindingList<string>();
            RecentProjects.Add("test");
            RecentProjects.Add("test2");
            RecentProjects.Add("test");
            RecentProjects.Add("test2");
            RecentProjects.Add("test");

            Navigation = navigation;

            CreateNewWindowCommand = new RelayCommand(
                execute: o => { Navigation.NavigateTo<EditorViewModel>(); },
                canExecute: o => true);
        }

        public ICommand CreateNewWindowCommand { get; set; }
        public BindingList<string> RecentProjects { get => _recentProjects;
            set
            {
                _recentProjects = value;
                OnPropertyChanged();
            }
        }
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