
using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Windows.Input;

namespace OpenBoardAnim.ViewModels
{
    public class LaunchViewModel : ViewModel
    {
        private BindingList<ProjectModel> _recentProjects;
        private INavigationService _navigation;

        public LaunchViewModel(INavigationService navigation)
        {
            RecentProjects = new BindingList<ProjectModel>();
            RecentProjects.Add(new ProjectModel { Title="test"});
            RecentProjects.Add(new ProjectModel { Title = "test2" });
            RecentProjects.Add(new ProjectModel { Title = "test" });
            RecentProjects.Add(new ProjectModel { Title = "test2" });
            RecentProjects.Add(new ProjectModel { Title = "test" });

            Navigation = navigation;

            CreateNewWindowCommand = new RelayCommand(
                execute: o => { Navigation.NavigateTo<EditorViewModel>(); },
                canExecute: o => true);
        }

        public ICommand CreateNewWindowCommand { get; set; }
        public BindingList<ProjectModel> RecentProjects { get => _recentProjects;
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