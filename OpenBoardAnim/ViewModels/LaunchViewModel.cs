using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using OpenBoardAnim.Utilities;
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
            try
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
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void DeleteProjectHandler(RecentProjectModel model)
        {
            try
            {
                _cache.DeleteProject(model);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void EditProjectHandler(RecentProjectModel model)
        {
            try
            {
                ProjectDetails project = _cache.LoadProjectFromFile(model);
                Navigation.NavigateTo<EditorViewModel>();
                _pubSub.Publish(SubTopic.ProjectLaunched, project);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void CreateAndLaunchNewProject()
        {
            try
            {
                Navigation.NavigateTo<EditorViewModel>();
                _pubSub.Publish(SubTopic.ProjectLaunched, new ProjectDetails());
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
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