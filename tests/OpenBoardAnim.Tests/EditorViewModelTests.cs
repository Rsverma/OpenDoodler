using Moq;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using OpenBoardAnim.ViewModels;
using System.ComponentModel;
using Xunit;

namespace OpenBoardAnim.Tests
{
    public class EditorViewModelTests
    {
        // A hand-rolled fake rather than a Moq mock - tests need to manually raise Tick
        // (simulating the timer elapsing) and inspect whether Start/Stop were called, which is
        // simpler as plain state than as Mock setups/verifies for an event-based interface.
        private class FakeAppTimer : IAppTimer
        {
            public TimeSpan Interval { get; set; }
            public bool IsRunning { get; private set; }
            public event EventHandler Tick;
            public void Start() => IsRunning = true;
            public void Stop() => IsRunning = false;
            public void RaiseTick() => Tick?.Invoke(this, EventArgs.Empty);
        }

        private readonly Mock<INavigationService> _navigation = new();
        private readonly Mock<IPubSubService> _pubSub = new();
        private readonly Mock<ICacheService> _cache = new();
        private readonly StateSnapshotService _stateSnapshotService = new();
        private readonly EditorActionsViewModel _actions;
        private readonly EditorCanvasViewModel _canvas;
        private readonly EditorLibraryViewModel _library;
        private readonly EditorTimelineViewModel _timeline;
        private readonly FakeAppTimer _snapshotTimer = new();
        private readonly FakeAppTimer _backupTimer = new();
        private Action<object> _projectLaunchedHandler;
        private Action<object> _projectStateRestoredHandler;

        public EditorViewModelTests()
        {
            _cache.SetupGet(c => c.LoadedGraphics).Returns(new BindingList<DrawingModel>());
            _cache.SetupGet(c => c.AllShapes).Returns(new BindingList<DrawingModel>());
            _cache.SetupGet(c => c.LoadedSceneTemplates).Returns(new BindingList<SceneTemplateModel>());

            _actions = new EditorActionsViewModel(_pubSub.Object, _navigation.Object, _cache.Object,
                Mock.Of<IDialogService>(), Mock.Of<IFileDialogService>(), Mock.Of<IMessageBoxService>());
            _canvas = new EditorCanvasViewModel(_pubSub.Object);
            _library = new EditorLibraryViewModel(_pubSub.Object, _cache.Object, Mock.Of<IDialogService>(),
                Mock.Of<IOpenFileDialogService>(), Mock.Of<IMessageBoxService>());
            _timeline = new EditorTimelineViewModel(_pubSub.Object, Mock.Of<IDialogService>());

            _pubSub.Setup(p => p.Subscribe(SubTopic.ProjectLaunched, It.IsAny<Action<object>>()))
                .Callback<SubTopic, Action<object>>((_, h) => _projectLaunchedHandler = h);
            _pubSub.Setup(p => p.Subscribe(SubTopic.ProjectStateRestored, It.IsAny<Action<object>>()))
                .Callback<SubTopic, Action<object>>((_, h) => _projectStateRestoredHandler = h);
        }

        private EditorViewModel CreateSut()
        {
            bool firstCall = true;
            Func<IAppTimer> timerFactory = () =>
            {
                IAppTimer timer = firstCall ? _snapshotTimer : _backupTimer;
                firstCall = false;
                return timer;
            };
            return new EditorViewModel(_navigation.Object, _pubSub.Object, _cache.Object, _stateSnapshotService,
                _actions, _canvas, _library, _timeline, timerFactory);
        }

        [Fact]
        public void Constructor_ConfiguresSnapshotAndBackupTimersWithDistinctIntervals()
        {
            CreateSut();

            Assert.Equal(TimeSpan.FromSeconds(2), _snapshotTimer.Interval);
            Assert.Equal(TimeSpan.FromSeconds(30), _backupTimer.Interval);
        }

        [Fact]
        public void ProjectLaunchedHandler_LoadsProjectIntoEveryPanel_AndStartsBothTimers()
        {
            CreateSut();
            ProjectDetails project = new();

            _projectLaunchedHandler(project);

            Assert.Same(project, _actions.Project);
            Assert.Same(project, _timeline.Project);
            Assert.Equal(project.Scenes.Count, _timeline.Scenes.Count);
            Assert.False(_actions.HasUnsavedChanges); // MarkProjectSaved was called
            Assert.True(_snapshotTimer.IsRunning);
            Assert.True(_backupTimer.IsRunning);
        }

        [Fact]
        public void ProjectLaunchedHandler_ClearsPriorUndoRedoHistory()
        {
            CreateSut();
            _stateSnapshotService.SaveState(new ProjectDetails { Title = "Old" });
            _stateSnapshotService.SaveState(new ProjectDetails { Title = "Older" });
            Assert.True(_stateSnapshotService.CanUndo);

            _projectLaunchedHandler(new ProjectDetails());

            Assert.False(_stateSnapshotService.CanUndo);
        }

        [Fact]
        public void SwitchToLaunchCommand_StopsBothTimers_AndNavigatesToLaunch()
        {
            EditorViewModel sut = CreateSut();
            _projectLaunchedHandler(new ProjectDetails()); // starts the timers

            sut.SwitchToLaunchCommand.Execute(null);

            Assert.False(_snapshotTimer.IsRunning);
            Assert.False(_backupTimer.IsRunning);
            _navigation.Verify(n => n.NavigateTo<LaunchViewModel>(), Times.Once);
        }

        [Fact]
        public void SnapshotTimerTick_SavesStateAndRefreshesUnsavedStatus()
        {
            CreateSut();
            _projectLaunchedHandler(new ProjectDetails());
            _snapshotTimer.RaiseTick(); // establishes the baseline snapshot (first SaveState call never enables undo by itself)
            _actions.Project.Title = "Edited";

            _snapshotTimer.RaiseTick();

            Assert.True(_stateSnapshotService.CanUndo);
        }

        [Fact]
        public void SnapshotTimerTick_NoOp_WhenNoProjectIsOpen()
        {
            CreateSut();

            _snapshotTimer.RaiseTick();

            Assert.False(_stateSnapshotService.CanUndo);
        }

        [Fact]
        public void BackupTimerTick_SavesBackup_WhenThereAreUnsavedChanges()
        {
            CreateSut();
            _projectLaunchedHandler(new ProjectDetails());
            _actions.Project.Title = "Edited";

            _backupTimer.RaiseTick();

            _cache.Verify(c => c.SaveBackup(_actions.Project), Times.Once);
        }

        [Fact]
        public void BackupTimerTick_SkipsSave_WhenNothingChangedSinceLastSave()
        {
            CreateSut();
            _projectLaunchedHandler(new ProjectDetails()); // MarkProjectSaved runs as part of this

            _backupTimer.RaiseTick();

            _cache.Verify(c => c.SaveBackup(It.IsAny<ProjectDetails>()), Times.Never);
        }

        [Fact]
        public void ProjectStateRestoredHandler_ReselectsThePreviouslyViewedScene()
        {
            CreateSut();
            ProjectDetails project = new();
            project.Scenes[0].Index = 1;
            project.Scenes[1].Index = 2;
            _projectLaunchedHandler(project);
            _timeline.SelectedScene = _timeline.Scenes.First(s => s.Index == 2);

            _projectStateRestoredHandler(project);

            Assert.Equal(2, _timeline.SelectedScene.Index);
        }
    }
}
