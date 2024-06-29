using Microsoft.Win32;
using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using System.Windows.Input;

namespace OpenBoardAnim.ViewModels
{
    public class EditorActionsViewModel : ViewModel
    {
        private IPubSubService _pubSub;
        private readonly INavigationService _navigation;
        private readonly CacheService _cache;

        public EditorActionsViewModel(IPubSubService pubSub, INavigationService navigation, CacheService Cache)
        {
            _pubSub = pubSub;
            _navigation = navigation;
            _cache = Cache;
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
                    _cache.SaveNewProject(Project, saveFileDialog.FileName);
                }
                else
                    return;
            }
            _cache.UpdateExistingProject(Project);
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
