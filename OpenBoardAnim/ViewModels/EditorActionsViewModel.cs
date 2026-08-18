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
        // In-memory clipboard for graphics - survives switching scenes, so pasting can
        // target a different scene than the one the graphic was copied from.
        private GraphicModelBase _copiedGraphic;

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
                DeleteItemCommand = new RelayCommand(execute: o => DeleteItem(), canExecute: o => HasSelection);
                MoveUpCommand = new RelayCommand(execute: o => MoveUp(), canExecute: o => SelectedGraphic != null);
                MoveDownCommand = new RelayCommand(execute: o => MoveDown(), canExecute: o => SelectedGraphic != null);
                NudgeSelectedGraphicCommand = new RelayCommand(execute: o => NudgeSelectedGraphic((string)o), canExecute: o => HasSelection);
                CopyGraphicCommand = new RelayCommand(execute: o => CopySelectedGraphic(), canExecute: o => SelectedGraphic != null);
                PasteGraphicCommand = new RelayCommand(execute: o => PasteGraphic(), canExecute: o => _copiedGraphic != null && CurrentScene != null);
                CutGraphicCommand = new RelayCommand(execute: o => CutSelectedGraphic(), canExecute: o => SelectedGraphic != null);
                DuplicateGraphicCommand = new RelayCommand(execute: o => DuplicateSelectedGraphic(), canExecute: o => SelectedGraphic != null);
                ToggleLockCommand = new RelayCommand(execute: o => ToggleLock(), canExecute: o => HasSelection);
                HideGraphicCommand = new RelayCommand(execute: o => HideSelectedGraphics(), canExecute: o => HasSelection);
                GroupGraphicsCommand = new RelayCommand(execute: o => GroupSelectedGraphics(), canExecute: o => GetSelectedGraphicsOrFallback().Count >= 2);
                UngroupGraphicsCommand = new RelayCommand(execute: o => UngroupSelectedGraphics(), canExecute: o => GetSelectedGraphicsOrFallback().Any(g => g.GroupId.HasValue));
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

        // Canvas z-order is unchanged: later index still paints on top (WPF's natural
        // last-in-list-renders-in-front rule). What changed is which direction "Move Up"/"Move
        // Down" walk that index in - see the layers panel's LayoutTransform flip in
        // EditorActionsView.xaml, which now shows the highest index (frontmost on canvas) at
        // the top of the list, matching the usual layers-panel convention. So "Move Up" (toward
        // the front, toward the top of the panel) now increases the index, and "Move Down" now
        // decreases it - the reverse of before.
        private void MoveUp()
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

        private void MoveDown()
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

        // The active selection for group operations (Delete, Nudge) - synced from the
        // canvas ListBox's SelectedItems by EditorCanvasView's SelectionChanged handler.
        // Falls back to SelectedGraphic (single-select commands like Copy/MoveUp/MoveDown
        // stay single-item on purpose) so these still work before any multi-select sync
        // has happened.
        public List<GraphicModelBase> SelectedGraphics { get; set; } = new();

        private List<GraphicModelBase> GetSelectedGraphicsOrFallback()
        {
            if (SelectedGraphics != null && SelectedGraphics.Count > 0)
                return SelectedGraphics;
            return SelectedGraphic != null ? new List<GraphicModelBase> { SelectedGraphic } : new List<GraphicModelBase>();
        }

        private bool HasSelection => GetSelectedGraphicsOrFallback().Count > 0;

        private void DeleteItem()
        {
            try
            {
                if (CurrentScene == null) return;
                foreach (GraphicModelBase graphic in GetSelectedGraphicsOrFallback())
                    CurrentScene.Graphics.Remove(graphic);
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
                List<GraphicModelBase> targets = GetSelectedGraphicsOrFallback();
                if (targets.Count == 0) return;
                double step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 10 : 1;
                foreach (GraphicModelBase graphic in targets)
                {
                    if (graphic.IsLocked) continue;
                    switch (direction)
                    {
                        case "Left": graphic.X -= step; break;
                        case "Right": graphic.X += step; break;
                        case "Up": graphic.Y -= step; break;
                        case "Down": graphic.Y += step; break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void CopySelectedGraphic()
        {
            try
            {
                if (SelectedGraphic == null) return;
                _copiedGraphic = SelectedGraphic.Clone();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void PasteGraphic()
        {
            try
            {
                if (_copiedGraphic == null || CurrentScene == null) return;
                GraphicModelBase pasted = _copiedGraphic.Clone();
                // Offset so a paste never lands exactly on top of the source graphic,
                // whether pasting into the same scene or a different one.
                pasted.X += 20;
                pasted.Y += 20;
                _pubSub.Publish(SubTopic.GraphicAdded, pasted);
                SelectedGraphic = pasted;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void CutSelectedGraphic()
        {
            try
            {
                if (SelectedGraphic == null || CurrentScene == null) return;
                _copiedGraphic = SelectedGraphic.Clone();
                CurrentScene.Graphics.Remove(SelectedGraphic);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void DuplicateSelectedGraphic()
        {
            try
            {
                if (SelectedGraphic == null || CurrentScene == null) return;
                GraphicModelBase duplicate = SelectedGraphic.Clone();
                // Offset so the duplicate never lands exactly on top of the source graphic.
                duplicate.X += 20;
                duplicate.Y += 20;
                _pubSub.Publish(SubTopic.GraphicAdded, duplicate);
                SelectedGraphic = duplicate;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void ToggleLock()
        {
            try
            {
                List<GraphicModelBase> targets = GetSelectedGraphicsOrFallback();
                if (targets.Count == 0) return;
                // A group selection should end up in one consistent state rather than each
                // member flipping its own IsLocked independently - lock everything if any
                // member is currently unlocked, otherwise unlock the whole selection.
                bool lockAll = targets.Any(g => !g.IsLocked);
                foreach (GraphicModelBase graphic in targets)
                    graphic.IsLocked = lockAll;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // One-way hide, not a toggle - a hidden graphic is Collapsed on the canvas (see
        // EditorCanvasView), so it can never be the thing right-clicked to re-show it there;
        // re-showing only happens from the layers panel, which toggles each row individually.
        private void HideSelectedGraphics()
        {
            try
            {
                List<GraphicModelBase> targets = GetSelectedGraphicsOrFallback();
                foreach (GraphicModelBase graphic in targets)
                    graphic.IsVisible = false;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void GroupSelectedGraphics()
        {
            try
            {
                List<GraphicModelBase> targets = GetSelectedGraphicsOrFallback();
                if (targets.Count < 2) return;
                Guid groupId = Guid.NewGuid();
                foreach (GraphicModelBase graphic in targets)
                    graphic.GroupId = groupId;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void UngroupSelectedGraphics()
        {
            try
            {
                foreach (GraphicModelBase graphic in GetSelectedGraphicsOrFallback())
                    graphic.GroupId = null;
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
                    canvas.Height = Project.Settings?.ExportHeight ?? 1080;
                    canvas.Width = Project.Settings?.ExportWidth ?? 1920;
                    // Export is always 2x the editor's coordinate space, regardless of aspect
                    // ratio - see ProjectSettings.ExportWidth/Height - so this stays fixed.
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
                _cache.ClearBackup();
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

                // Closing back to Launch immediately re-triggers LaunchViewModel's recovery
                // check (it's DI-transient, so a fresh instance runs OfferBackupRecovery on
                // every navigation there) - a lingering backup from this session's autosave
                // timer would otherwise pop the "recover unsaved project?" prompt right after
                // an intentional close, even though the user just chose to discard/save.
                _cache.ClearBackup();
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

        // Used after recovering an autosave backup: the recovered content is newer than
        // whatever's on disk at Project.Path (or was never saved at all), so it must not be
        // treated as already "saved" even though it was just loaded, the way a fresh/opened
        // project normally would be.
        public void MarkProjectUnsaved()
        {
            _savedProjectJson = null;
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
        public ICommand CopyGraphicCommand { get; set; }
        public ICommand PasteGraphicCommand { get; set; }
        public ICommand CutGraphicCommand { get; set; }
        public ICommand DuplicateGraphicCommand { get; set; }
        public ICommand ToggleLockCommand { get; set; }
        public ICommand HideGraphicCommand { get; set; }
        public ICommand GroupGraphicsCommand { get; set; }
        public ICommand UngroupGraphicsCommand { get; set; }
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

        // Both EditorCanvasView's canvas ListBox and EditorActionsView's layers panel ListBox
        // bind SelectedItem (TwoWay) to this same property, so selecting in either one is
        // supposed to select in the other - but a plain auto-property never raises
        // PropertyChanged, so writes from one binding's Target->Source direction never notify
        // the other binding's Source->Target direction. A full property fixes that; this was
        // the actual bug behind "select graphic from scene and from layer should be bound".
        private GraphicModelBase _selectedGraphic;
        public GraphicModelBase SelectedGraphic
        {
            get { return _selectedGraphic; }
            set
            {
                _selectedGraphic = value;
                OnPropertyChanged();
            }
        }

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
