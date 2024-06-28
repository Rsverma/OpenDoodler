using OpenBoardAnim.Core;
using OpenBoardAnim.Library;
using OpenBoardAnim.Library.Repositories;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.Json;
using System.Windows.Input;

namespace OpenBoardAnim.ViewModels
{
    public class LaunchViewModel : ViewModel
    {
        private BindingList<ProjectModel> _recentProjects;
        private INavigationService _navigation;
        private readonly IPubSubService _pubSub;

        public LaunchViewModel(INavigationService navigation, IPubSubService pubSub, ProjectRepository project)
        {
            List<ProjectEntity> projects = project.GetRecentProjects();
            var models = projects.Select(x => new ProjectModel
            {
                CreatedOn = x.CreatedOn,
                ModifiedOn = x.LatestLaunchTime,
                Scenes = x.SceneCount,
                Title = x.Title,
                FilePath = x.FilePath,
                EditProject = EditProjectHandler
            }).ToList();
            RecentProjects = new BindingList<ProjectModel>(models);

            Navigation = navigation;
            _pubSub = pubSub;
            CreateNewWindowCommand = new RelayCommand(
                execute: o => CreateAndLaunchNewProject(),
                canExecute: o => true);
        }

        private void EditProjectHandler(ProjectModel model)
        {
            string json = File.ReadAllText(model.FilePath);
            ProjectDetails project = JsonSerializer.Deserialize<ProjectDetails>(json);
            Navigation.NavigateTo<EditorViewModel>();
            _pubSub.Publish(SubTopic.ProjectLaunched, project);
        }

        private void CreateAndLaunchNewProject()
        {
            Navigation.NavigateTo<EditorViewModel>();
            _pubSub.Publish(SubTopic.ProjectLaunched, new ProjectDetails());
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