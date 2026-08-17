using Microsoft.Win32;
using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using OpenBoardAnim.Utilities;
using OpenBoardAnim.Utils;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenBoardAnim.ViewModels
{
    public class EditorActionsViewModel : ViewModel
    {
        private readonly IPubSubService _pubSub;
        private readonly INavigationService _navigation;
        private readonly CacheService _cache;
        private readonly IDialogService _dialog;
        private CancellationTokenSource _exportCts;
        private string _savedProjectJson;

        public EditorActionsViewModel(IPubSubService pubSub, INavigationService navigation, CacheService Cache,
            IDialogService dialog)
        {
            try
            {
                _pubSub = pubSub;
                pubSub.Subscribe(SubTopic.SceneChanged, SceneChangedHandler);
                _navigation = navigation;
                _cache = Cache;
                _dialog = dialog;
                CloseProjectCommand = new RelayCommand(execute: o => CloseProject(), canExecute: o => true);
                SaveProjectCommand = new RelayCommand(execute: o => SaveProject(), canExecute: o => Project != null);
                ExportProjectCommand = new RelayCommand(execute: o => ExportProject(), canExecute: o => !IsExporting);
                CancelExportCommand = new RelayCommand(execute: o => _exportCts?.Cancel(), canExecute: o => IsExporting);
                PreviewProjectCommand = new RelayCommand(execute: o => PreviewProject(), canExecute: o => true);
                DeleteItemCommand = new RelayCommand(execute: o => DeleteItem(), canExecute: o => SelectedGraphic != null);
                MoveUpCommand = new RelayCommand(execute: o => MoveUp(), canExecute: o => SelectedGraphic != null);
                MoveDownCommand = new RelayCommand(execute: o => MoveDown(), canExecute: o => SelectedGraphic != null);
                NudgeSelectedGraphicCommand = new RelayCommand(execute: o => NudgeSelectedGraphic((string)o), canExecute: o => SelectedGraphic != null);
                LaunchSceneSettingsCommand = new RelayCommand(execute: o => LaunchSceneSettings(), canExecute: o => CurrentScene != null);
                LaunchProjectSettingsCommand = new RelayCommand(execute: o => LaunchProjectSettings(), canExecute: o => true);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void LaunchSceneSettings()
        {
            try
            {
                _ = _dialog.ShowDialog(DialogType.SceneSettings, CurrentScene);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void LaunchProjectSettings()
        {
            try
            {

                _ = _dialog.ShowDialog(DialogType.ProjectSettings, Project);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }

        }

        private void MoveUp()
        {
            try
            {
                if (SelectedGraphic == null || CurrentScene == null) return;
                var model = SelectedGraphic;
                int index = CurrentScene.Graphics.IndexOf(model);
                if (index < 1) return;
                CurrentScene.Graphics.RemoveAt(index);
                CurrentScene.Graphics.Insert(index - 1, model); SelectedGraphic = model;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void MoveDown()
        {
            try
            {
                if (SelectedGraphic == null || CurrentScene == null) return;
                var model = SelectedGraphic;
                int index = CurrentScene.Graphics.IndexOf(model);
                if (index < 0 || index == CurrentScene.Graphics.Count - 1) return;
                CurrentScene.Graphics.RemoveAt(index);
                CurrentScene.Graphics.Insert(index + 1, model); SelectedGraphic = model;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void DeleteItem()
        {
            try
            {
                if (SelectedGraphic != null)
                    CurrentScene?.Graphics.Remove(SelectedGraphic);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void NudgeSelectedGraphic(string direction)
        {
            try
            {
                if (SelectedGraphic == null) return;
                double step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 10 : 1;
                switch (direction)
                {
                    case "Left": SelectedGraphic.X -= step; break;
                    case "Right": SelectedGraphic.X += step; break;
                    case "Up": SelectedGraphic.Y -= step; break;
                    case "Down": SelectedGraphic.Y += step; break;
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void PreviewProject()
        {
            try
            {
                _ = _dialog.ShowDialog(DialogType.PreviewProject, Project);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private async void ExportProject()
        {
            try
            {
                SaveFileDialog saveFileDialog = new()
                {
                    Filter = "MP4 Video (*.mp4)|*.mp4",
                    DefaultExt = "mp4",
                    FileName = string.IsNullOrWhiteSpace(Project?.Title) ? "output.mp4" : $"{Project.Title}.mp4"
                };
                if (saveFileDialog.ShowDialog() != true)
                    return;

                IsExporting = true;
                ExportProgressPercentage = 0;
                ExportStatusText = "Starting export...";
                _pubSub.Publish(SubTopic.ProjectExporting, true);
                _exportCts = new CancellationTokenSource();

                var progress = new Progress<ExportProgressInfo>(p =>
                {
                    ExportProgressPercentage = p.Percentage;
                    ExportStatusText = p.Status;
                });

                using (var host = new HwndSource(new HwndSourceParameters
                {
                    WindowStyle = 0x800000, // WS_POPUP (invisible window)
                    Width = 1,
                    Height = 1,
                    PositionX = -10000,    // Position off-screen
                    PositionY = -10000,
                }))
                {
                    System.Windows.Controls.Canvas canvas = new();
                    canvas.Background = Brushes.White;
                    canvas.Height = 1080;
                    canvas.Width = 1920;
                    canvas.LayoutTransform = new ScaleTransform(2, 2);
                    host.RootVisual = canvas;

                    // Force layout and render passes
                    canvas.Measure(new Size(canvas.Width, canvas.Height));
                    canvas.Arrange(new Rect(0, 0, canvas.Width, canvas.Height));
                    canvas.UpdateLayout();
                    //window.Show();
                    await PreviewAndExportHandler.RunAnimationsOnCanvas(Project, canvas, true, progress, saveFileDialog.FileName, _exportCts.Token);
                }

                Logger.LogMessage("Export complete", LogAction.LogAndShow);
            }
            catch (OperationCanceledException)
            {
                Logger.LogMessage("Export canceled", LogAction.LogAndShow);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
            finally
            {
                IsExporting = false;
                _pubSub.Publish(SubTopic.ProjectExporting, false);
                _exportCts?.Dispose();
                _exportCts = null;
            }
        }

        private void SceneChangedHandler(object obj)
        {
            try
            {
                SceneModel scene = (SceneModel)obj;
                CurrentScene = scene;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void SaveProject()
        {
            try
            {
                if (Project == null) return;
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
                MarkProjectSaved();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void CloseProject()
        {
            try
            {
                if (!ConfirmDiscardUnsavedChanges())
                    return;

                Project = null;
                _navigation.NavigateTo<LaunchViewModel>();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // Baseline used to detect unsaved changes: set whenever a project is freshly
        // loaded/created or successfully saved, left untouched across undo/redo so a
        // restored state is only considered "clean" if it actually matches disk again.
        public void MarkProjectSaved()
        {
            _savedProjectJson = Project == null ? null : JsonSerializer.Serialize(Project);
        }

        public bool HasUnsavedChanges => Project != null && JsonSerializer.Serialize(Project) != _savedProjectJson;

        // Prompts to save/discard/cancel if there are unsaved changes. Returns true if
        // it's safe to proceed (nothing to lose, changes were saved, or the user chose
        // to discard), false if the caller should abort (user canceled, or chose to
        // save but the save didn't actually complete, e.g. they canceled the file picker).
        public bool ConfirmDiscardUnsavedChanges()
        {
            if (!HasUnsavedChanges)
                return true;

            MessageBoxResult result = MessageBox.Show(
                "This project has unsaved changes. Save before continuing?",
                "Unsaved Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    SaveProject();
                    return !HasUnsavedChanges;
                case MessageBoxResult.No:
                    return true;
                default:
                    return false;
            }
        }

        private ProjectDetails _project;
        public ProjectDetails Project
        {
            get => _project;
            set
            {
                _project = value;
                OnPropertyChanged();
            }
        }
        public ICommand CloseProjectCommand { get; set; }
        public ICommand DeleteItemCommand { get; set; }
        public ICommand MoveUpCommand { get; set; }
        public ICommand MoveDownCommand { get; set; }
        public ICommand NudgeSelectedGraphicCommand { get; set; }
        public ICommand SaveProjectCommand { get; set; }
        public ICommand ExportProjectCommand { get; set; }
        public ICommand CancelExportCommand { get; set; }
        public ICommand PreviewProjectCommand { get; set; }
        public ICommand LaunchSceneSettingsCommand { get; set; }
        public ICommand LaunchProjectSettingsCommand { get; set; }
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

        private bool _isExporting;
        public bool IsExporting
        {
            get => _isExporting;
            set
            {
                _isExporting = value;
                OnPropertyChanged();
            }
        }

        private double _exportProgressPercentage;
        public double ExportProgressPercentage
        {
            get => _exportProgressPercentage;
            set
            {
                _exportProgressPercentage = value;
                OnPropertyChanged();
            }
        }

        private string _exportStatusText;
        public string ExportStatusText
        {
            get => _exportStatusText;
            set
            {
                _exportStatusText = value;
                OnPropertyChanged();
            }
        }
    }
}
