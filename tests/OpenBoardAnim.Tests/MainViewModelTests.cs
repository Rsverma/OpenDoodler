using Moq;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using OpenBoardAnim.ViewModels;
using System.Windows;
using Xunit;

namespace OpenBoardAnim.Tests
{
    public class MainViewModelTests
    {
        private readonly Mock<INavigationService> _navigation = new();
        private readonly Mock<IPubSubService> _pubSub = new();
        private readonly Mock<ICacheService> _cache = new();
        private readonly Mock<IThemeService> _theme = new();
        private readonly Mock<IOpenFileDialogService> _openFileDialog = new();
        private readonly Mock<IApplicationService> _application = new();
        private readonly StateSnapshotService _stateSnapshotService = new();

        // A real EditorActionsViewModel (built from mocked services, same as
        // EditorActionsViewModelTests) rather than a Moq mock - it has no virtual members to
        // override and MainViewModel only needs its real Project/HasUnsavedChanges/
        // ConfirmDiscardUnsavedChanges behavior, which the concrete class already provides.
        private readonly Mock<IDialogService> _actionsDialog = new();
        private readonly Mock<IFileDialogService> _actionsFileDialog = new();
        private readonly Mock<IMessageBoxService> _actionsMessageBox = new();
        private readonly EditorActionsViewModel _actions;

        public MainViewModelTests()
        {
            _actions = new EditorActionsViewModel(_pubSub.Object, _navigation.Object, _cache.Object,
                _actionsDialog.Object, _actionsFileDialog.Object, _actionsMessageBox.Object);
        }

        private MainViewModel CreateSut()
            => new(_navigation.Object, _pubSub.Object, _stateSnapshotService, _cache.Object, _actions, _theme.Object,
                _openFileDialog.Object, _application.Object);

        [Fact]
        public void Constructor_NavigatesToLaunchImmediately()
        {
            CreateSut();

            _navigation.Verify(n => n.NavigateTo<LaunchViewModel>(), Times.Once);
        }

        [Fact]
        public void UpdateWindowTitle_ReflectsOpenProjectTitle()
        {
            MainViewModel sut = CreateSut();

            _actions.Project = new ProjectDetails { Title = "My Project" };
            _actions.MarkProjectSaved();

            Assert.Equal("My Project", sut.ProjectStatusText);
        }

        [Fact]
        public void UpdateWindowTitle_ShowsUnsavedMarker_WhenThereAreUnsavedChanges()
        {
            MainViewModel sut = CreateSut();
            _actions.Project = new ProjectDetails { Title = "My Project" };
            _actions.MarkProjectSaved();
            _actions.Project.Title = "Changed";

            // ProjectDetails.Title is a plain property with no OnPropertyChanged of its own -
            // mutating it alone doesn't refresh the title bar. In the real app,
            // EditorViewModel's periodic snapshot timer calls RefreshUnsavedStatus() on every
            // tick, which is what actually re-raises HasUnsavedChanges and reaches
            // MainViewModel's Actions_PropertyChanged subscription.
            _actions.RefreshUnsavedStatus();

            Assert.Equal("* Changed", sut.ProjectStatusText);
        }

        [Fact]
        public void UpdateWindowTitle_Blank_WhenNoProjectIsOpen()
        {
            MainViewModel sut = CreateSut();

            Assert.Equal("", sut.ProjectStatusText);
        }

        [Fact]
        public void UndoCommand_RestoresStateAndPublishesProjectStateRestored()
        {
            MainViewModel sut = CreateSut();
            _stateSnapshotService.SaveState(new ProjectDetails { Title = "First" });
            _stateSnapshotService.SaveState(new ProjectDetails { Title = "Second" });

            Assert.True(sut.UndoCommand.CanExecute(null));
            sut.UndoCommand.Execute(null);

            _pubSub.Verify(p => p.Publish(SubTopic.ProjectStateRestored, It.Is<ProjectDetails>(p => p.Title == "First")), Times.Once);
        }

        [Fact]
        public void RedoCommand_CanExecute_FalseUntilAnUndoHappened()
        {
            MainViewModel sut = CreateSut();
            Assert.False(sut.RedoCommand.CanExecute(null));

            _stateSnapshotService.SaveState(new ProjectDetails { Title = "First" });
            _stateSnapshotService.SaveState(new ProjectDetails { Title = "Second" });
            sut.UndoCommand.Execute(null);

            Assert.True(sut.RedoCommand.CanExecute(null));
        }

        [Fact]
        public void NewProjectCommand_NavigatesAndPublishesProjectLaunched_WhenNothingUnsaved()
        {
            MainViewModel sut = CreateSut();

            sut.NewProjectCommand.Execute(null);

            _navigation.Verify(n => n.NavigateTo<EditorViewModel>(), Times.Once);
            _pubSub.Verify(p => p.Publish(SubTopic.ProjectLaunched, It.IsAny<ProjectDetails>()), Times.Once);
        }

        [Fact]
        public void NewProjectCommand_Blocked_WhenUserCancelsTheUnsavedChangesPrompt()
        {
            MainViewModel sut = CreateSut();
            _actions.Project = new ProjectDetails { Path = "C:\\p.obap" };
            _actions.MarkProjectSaved();
            _actions.Project.Title = "Changed";
            _actionsMessageBox.Setup(m => m.Show(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>()))
                .Returns(MessageBoxResult.Cancel);

            sut.NewProjectCommand.Execute(null);

            _navigation.Verify(n => n.NavigateTo<EditorViewModel>(), Times.Never);
        }

        [Fact]
        public void OpenProjectCommand_UserCancelsFileDialog_DoesNothing()
        {
            MainViewModel sut = CreateSut();
            _openFileDialog.Setup(f => f.ShowOpenFileDialog(It.IsAny<string>(), false)).Returns([]);

            sut.OpenProjectCommand.Execute(null);

            _cache.Verify(c => c.LoadProjectFromFile(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public void OpenProjectCommand_LoadsChosenFile_AndNavigatesToEditor()
        {
            MainViewModel sut = CreateSut();
            _openFileDialog.Setup(f => f.ShowOpenFileDialog(It.IsAny<string>(), false)).Returns(["C:\\p.obap"]);
            ProjectDetails loaded = new() { Title = "Loaded" };
            _cache.Setup(c => c.LoadProjectFromFile("C:\\p.obap")).Returns(loaded);

            sut.OpenProjectCommand.Execute(null);

            _navigation.Verify(n => n.NavigateTo<EditorViewModel>(), Times.Once);
            _pubSub.Verify(p => p.Publish(SubTopic.ProjectLaunched, loaded), Times.Once);
        }

        [Fact]
        public void OpenProjectCommand_LoadReturnsNull_DoesNotNavigate()
        {
            MainViewModel sut = CreateSut();
            _openFileDialog.Setup(f => f.ShowOpenFileDialog(It.IsAny<string>(), false)).Returns(["C:\\bad.obap"]);
            _cache.Setup(c => c.LoadProjectFromFile("C:\\bad.obap")).Returns((ProjectDetails)null);

            sut.OpenProjectCommand.Execute(null);

            _navigation.Verify(n => n.NavigateTo<EditorViewModel>(), Times.Never);
        }

        [Fact]
        public void ExitCommand_ClosesTheMainWindow()
        {
            MainViewModel sut = CreateSut();

            sut.ExitCommand.Execute(null);

            _application.Verify(a => a.CloseMainWindow(), Times.Once);
        }

        [Fact]
        public void ConfirmExit_DelegatesToActions()
        {
            MainViewModel sut = CreateSut();
            _actions.Project = new ProjectDetails();
            _actions.MarkProjectSaved();

            Assert.True(sut.ConfirmExit());
        }
    }
}
