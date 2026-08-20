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
                MoveTopCommand = new RelayCommand(execute: o => MoveTop(), canExecute: o => SelectedGraphic != null);
                MoveBottomCommand = new RelayCommand(execute: o => MoveBottom(), canExecute: o => SelectedGraphic != null);
                NudgeSelectedGraphicCommand = new RelayCommand(execute: o => NudgeSelectedGraphic((string)o), canExecute: o => HasSelection);
                CopyGraphicCommand = new RelayCommand(execute: o => CopySelectedGraphic(), canExecute: o => SelectedGraphic != null);
                PasteGraphicCommand = new RelayCommand(execute: o => PasteGraphic(), canExecute: o => _copiedGraphic != null && CurrentScene != null);
                CutGraphicCommand = new RelayCommand(execute: o => CutSelectedGraphic(), canExecute: o => SelectedGraphic != null);
                DuplicateGraphicCommand = new RelayCommand(execute: o => DuplicateSelectedGraphic(), canExecute: o => SelectedGraphic != null);
                ToggleLockCommand = new RelayCommand(execute: o => ToggleLock(), canExecute: o => HasSelection);
                HideGraphicCommand = new RelayCommand(execute: o => HideSelectedGraphics(), canExecute: o => HasSelection);
                GroupGraphicsCommand = new RelayCommand(execute: o => GroupSelectedGraphics(), canExecute: o => GetSelectedGraphicsOrFallback().Count >= 2);
                UngroupGraphicsCommand = new RelayCommand(execute: o => UngroupSelectedGraphics(), canExecute: o => GetSelectedGraphicsOrFallback().Any(g => g.GroupId.HasValue));
                AlignLeftCommand = new RelayCommand(execute: o => AlignLeft(), canExecute: o => GetSelectedGraphicsOrFallback().Count >= 2);
                AlignRightCommand = new RelayCommand(execute: o => AlignRight(), canExecute: o => GetSelectedGraphicsOrFallback().Count >= 2);
                AlignCenterCommand = new RelayCommand(execute: o => AlignCenter(), canExecute: o => GetSelectedGraphicsOrFallback().Count >= 2);
                AlignTopCommand = new RelayCommand(execute: o => AlignTop(), canExecute: o => GetSelectedGraphicsOrFallback().Count >= 2);
                AlignBottomCommand = new RelayCommand(execute: o => AlignBottom(), canExecute: o => GetSelectedGraphicsOrFallback().Count >= 2);
                AlignMiddleCommand = new RelayCommand(execute: o => AlignMiddle(), canExecute: o => GetSelectedGraphicsOrFallback().Count >= 2);
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

        // Same "later index paints on top" convention as MoveUp/MoveDown above - jumping
        // straight to the front/back of the stack instead of one step at a time.
        private void MoveTop()
        {
            try
            {
                if (SelectedGraphic == null || CurrentScene == null) return;
                var model = SelectedGraphic;
                int index = CurrentScene.Graphics.IndexOf(model);
                if (index < 0 || index == CurrentScene.Graphics.Count - 1) return;
                CurrentScene.Graphics.RemoveAt(index);
                CurrentScene.Graphics.Add(model);
                SelectedGraphic = model;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void MoveBottom()
        {
            try
            {
                if (SelectedGraphic == null || CurrentScene == null) return;
                var model = SelectedGraphic;
                int index = CurrentScene.Graphics.IndexOf(model);
                if (index <= 0) return;
                CurrentScene.Graphics.RemoveAt(index);
                CurrentScene.Graphics.Insert(0, model);
                SelectedGraphic = model;
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

        public List<GraphicModelBase> GetSelectedGraphicsOrFallback()
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

        // Align commands below all read the full selection (including any locked members) to
        // compute the shared reference line/bounds, but skip writing X/Y back onto a locked
        // graphic - same "locked blocks repositioning, not the operation itself" rule Nudge
        // already follows, so a locked item can still anchor where the others land.
        private void AlignLeft()
        {
            try
            {
                List<GraphicModelBase> targets = GetSelectedGraphicsOrFallback();
                if (targets.Count < 2) return;
                double left = targets.Min(g => g.X);
                foreach (GraphicModelBase graphic in targets)
                    if (!graphic.IsLocked) graphic.X = left;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void AlignRight()
        {
            try
            {
                List<GraphicModelBase> targets = GetSelectedGraphicsOrFallback();
                if (targets.Count < 2) return;
                double right = targets.Max(g => g.X + g.Width);
                foreach (GraphicModelBase graphic in targets)
                    if (!graphic.IsLocked) graphic.X = right - graphic.Width;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void AlignCenter()
        {
            try
            {
                List<GraphicModelBase> targets = GetSelectedGraphicsOrFallback();
                if (targets.Count < 2) return;
                double left = targets.Min(g => g.X);
                double right = targets.Max(g => g.X + g.Width);
                double centerX = (left + right) / 2;
                foreach (GraphicModelBase graphic in targets)
                    if (!graphic.IsLocked) graphic.X = centerX - graphic.Width / 2;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void AlignTop()
        {
            try
            {
                List<GraphicModelBase> targets = GetSelectedGraphicsOrFallback();
                if (targets.Count < 2) return;
                double top = targets.Min(g => g.Y);
                foreach (GraphicModelBase graphic in targets)
                    if (!graphic.IsLocked) graphic.Y = top;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void AlignBottom()
        {
            try
            {
                List<GraphicModelBase> targets = GetSelectedGraphicsOrFallback();
                if (targets.Count < 2) return;
                double bottom = targets.Max(g => g.Y + g.Height);
                foreach (GraphicModelBase graphic in targets)
                    if (!graphic.IsLocked) graphic.Y = bottom - graphic.Height;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void AlignMiddle()
        {
            try
            {
                List<GraphicModelBase> targets = GetSelectedGraphicsOrFallback();
                if (targets.Count < 2) return;
                double top = targets.Min(g => g.Y);
                double bottom = targets.Max(g => g.Y + g.Height);
                double centerY = (top + bottom) / 2;
                foreach (GraphicModelBase graphic in targets)
                    if (!graphic.IsLocked) graphic.Y = centerY - graphic.Height / 2;
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
                // The canvas itself is a View concern (EditorView owns it), so it's captured
                // over there in response to this event rather than reached into from here.
                ThumbnailCaptureRequested?.Invoke(Project);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // Raised after a successful save so EditorView can capture a fresh Launch-screen
        // thumbnail from the live canvas - a plain event rather than IPubSubService since
        // Views aren't DI-resolved in this app and so have no way to reach IPubSubService
        // themselves; they only ever get it indirectly through a bound ViewModel like this one.
        public event Action<ProjectDetails> ThumbnailCaptureRequested;

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
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }

        // Used after recovering an autosave backup: the recovered content is newer than
        // whatever's on disk at Project.Path (or was never saved at all), so it must not be
        // treated as already "saved" even though it was just loaded, the way a fresh/opened
        // project normally would be.
        public void MarkProjectUnsaved()
        {
            _savedProjectJson = null;
            OnPropertyChanged(nameof(HasUnsavedChanges));
        }

        public bool HasUnsavedChanges => Project != null && JsonSerializer.Serialize(Project) != _savedProjectJson;

        // HasUnsavedChanges has no backing field to observe - nested edits (graphic drags,
        // text changes, scene edits, etc.) never touch the Project property setter itself,
        // so nothing raises PropertyChanged for it on its own. EditorViewModel's periodic
        // snapshot timer already re-serializes the whole project every couple seconds for
        // undo/redo purposes; piggyback on that same tick to keep the window header's
        // unsaved indicator reasonably live instead of adding a second polling timer.
        public void RefreshUnsavedStatus() => OnPropertyChanged(nameof(HasUnsavedChanges));

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
                OnPropertyChanged(nameof(HasUnsavedChanges));
            }
        }
        public ICommand CloseProjectCommand { get; set; }
        public ICommand DeleteItemCommand { get; set; }
        public ICommand MoveUpCommand { get; set; }
        public ICommand MoveDownCommand { get; set; }
        public ICommand MoveTopCommand { get; set; }
        public ICommand MoveBottomCommand { get; set; }
        public ICommand NudgeSelectedGraphicCommand { get; set; }
        public ICommand CopyGraphicCommand { get; set; }
        public ICommand PasteGraphicCommand { get; set; }
        public ICommand CutGraphicCommand { get; set; }
        public ICommand DuplicateGraphicCommand { get; set; }
        public ICommand ToggleLockCommand { get; set; }
        public ICommand HideGraphicCommand { get; set; }
        public ICommand GroupGraphicsCommand { get; set; }
        public ICommand UngroupGraphicsCommand { get; set; }
        public ICommand AlignLeftCommand { get; set; }
        public ICommand AlignRightCommand { get; set; }
        public ICommand AlignCenterCommand { get; set; }
        public ICommand AlignTopCommand { get; set; }
        public ICommand AlignBottomCommand { get; set; }
        public ICommand AlignMiddleCommand { get; set; }
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
