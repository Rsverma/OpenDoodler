using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using System.ComponentModel;
using System.Windows.Input;

namespace OpenBoardAnim.ViewModels
{
    public class LaunchViewModel : ViewModel
    {
        private BindingList<RecentProjectModel> _recentProjects;
        private INavigationService _navigation;
        private readonly IPubSubService _pubSub;
        private readonly CacheService _cache;

        public LaunchViewModel(INavigationService navigation, IPubSubService pubSub,CacheService cache)
        {
            Navigation = navigation;
            _pubSub = pubSub;
            _cache = cache;
            CreateNewWindowCommand = new RelayCommand(
                execute: o => CreateAndLaunchNewProject(),
                canExecute: o => true);
            RecentProjects = cache.RecentProjects;
            foreach (var proj in RecentProjects)
            {
                proj.EditProject = EditProjectHandler;
                proj.DeleteProject = DeleteProjectHandler;
            }
        }

        private void DeleteProjectHandler(RecentProjectModel model)
        {
            _cache.DeleteProject(model);
        }

        private void EditProjectHandler(RecentProjectModel model)
        {
            ProjectDetails project = _cache.LoadProjectFromFile(model);
            Navigation.NavigateTo<EditorViewModel>();
            _pubSub.Publish(SubTopic.ProjectLaunched, project);
        }

        private void CreateAndLaunchNewProject()
        {
            Navigation.NavigateTo<EditorViewModel>();
            _pubSub.Publish(SubTopic.ProjectLaunched, new ProjectDetails());
        }

        public ICommand CreateNewWindowCommand { get; set; }
        public BindingList<RecentProjectModel> RecentProjects {
            get => _recentProjects;
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