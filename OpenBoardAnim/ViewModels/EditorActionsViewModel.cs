using Microsoft.Win32;
using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using System.ComponentModel;
using System.Windows.Input;

namespace OpenBoardAnim.ViewModels
{
    public class EditorActionsViewModel : ViewModel
    {
        private IPubSubService _pubSub;
        private readonly INavigationService _navigation;
        private readonly CacheService _cache;
        private readonly IDialogService _dialog;

        public EditorActionsViewModel(IPubSubService pubSub, INavigationService navigation, CacheService Cache,
            IDialogService dialog)
        {
            _pubSub = pubSub;
            pubSub.Subscribe(SubTopic.SceneChanged, SceneChangedHandler);
            _navigation = navigation;
            _cache = Cache;
            _dialog = dialog;
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
                execute: o => PreviewProject(),
                canExecute: o => true);
            DeleteItemCommand = new RelayCommand(
                execute: o => DeleteItem(),
                canExecute: o => SelectedGraphic!=null);
        }

        private void DeleteItem()
        {
            if(SelectedGraphic!=null)
                CurrentScene?.Graphics.Remove(SelectedGraphic);
        }

        private void PreviewProject()
        {
            _ = _dialog.ShowDialog(DialogType.PreviewProject, Project);
        }

        private void SceneChangedHandler(object obj)
        {
            SceneModel scene = (SceneModel)obj;
            CurrentScene = scene;
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
        public ICommand DeleteItemCommand { get; set; }
        public ICommand SaveProjectCommand { get; set; }
        public ICommand ExportProjectCommand { get; set; }
        public ICommand PreviewProjectCommand { get; set; }
        private SceneModel _currentScene;

        public SceneModel CurrentScene
        {
            get { return _currentScene; }
            set
            {
                _currentScene = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SceneGraphics));
            }
        }
        public BindingList<GraphicModelBase> SceneGraphics => CurrentScene?.Graphics;
        public GraphicModelBase SelectedGraphic { get; set; }
    }
}
