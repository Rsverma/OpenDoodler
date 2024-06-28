using Microsoft.Win32;
using OpenBoardAnim.Core;
using OpenBoardAnim.Library.Repositories;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using System.IO;
using System.Text.Json;
using System.Windows.Input;

namespace OpenBoardAnim.ViewModels
{
    public class EditorActionsViewModel : ViewModel
    {
        private IPubSubService _pubSub;
        private readonly INavigationService _navigation;
        private readonly ProjectRepository _pRepo;

        public EditorActionsViewModel(IPubSubService pubSub, INavigationService navigation, ProjectRepository pRepo)
        {
            _pubSub = pubSub;
            _navigation = navigation;
            _pRepo = pRepo;
            CloseProjectCommand = new RelayCommand(
                execute: o => CloseProject(),
                canExecute: o => true);
            SaveProjectCommand = new RelayCommand(
                execute: o => SaveProject(),
                canExecute: o => true);
            ExportProjectCommand = new RelayCommand(
                execute: o => { },
                canExecute: o => false);
            PreviewProjectCommand = new RelayCommand(
                execute: o => { },
                canExecute: o => false);
        }

        private void SaveProject()
        {
            if (string.IsNullOrEmpty(Project.Path))
            {
                SaveFileDialog saveFileDialog = new()
                {
                    Filter = "Project file (*.obap)|*.obap",
                };
                if (saveFileDialog.ShowDialog() == true)
                {
                    Project.Path = saveFileDialog.FileName;
                    Project.Title = Path.GetFileNameWithoutExtension(Project.Path);
                    File.WriteAllText(saveFileDialog.FileName, JsonSerializer.Serialize(Project));
                    _pRepo.SaveNewProject(new Library.ProjectEntity
                    {
                        Title = Project.Title,
                        CreatedOn = Project.CreatedOn,
                        FilePath = Project.Path,
                        LatestLaunchTime = DateTime.Now,
                        SceneCount = Project.Scenes.Count,
                    });
                }
                else
                    return;
            }
                //File.WriteAllText(saveFileDialog.FileName, txtEditor.Text);
        }

        private void CloseProject()
        {
            _navigation.NavigateTo<LaunchViewModel>();
        }

        public ProjectDetails Project { get; set; }
        public ICommand CloseProjectCommand { get; set; }
        public ICommand SaveProjectCommand { get; set; }
        public ICommand ExportProjectCommand { get; set; }
        public ICommand PreviewProjectCommand { get; set; }
    }
}
