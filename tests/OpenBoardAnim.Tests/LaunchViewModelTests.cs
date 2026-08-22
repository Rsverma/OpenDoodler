using Moq;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using OpenBoardAnim.ViewModels;
using System.ComponentModel;
using System.Windows;
using Xunit;

namespace OpenBoardAnim.Tests
{
    public class LaunchViewModelTests
    {
        private readonly Mock<INavigationService> _navigation = new();
        private readonly Mock<IPubSubService> _pubSub = new();
        private readonly Mock<ICacheService> _cache = new();
        private readonly Mock<IMessageBoxService> _messageBox = new();
        private readonly Mock<IDispatcherService> _dispatcher = new();
        private Action _deferredRecoveryCheck;

        public LaunchViewModelTests()
        {
            _cache.SetupGet(c => c.RecentProjects).Returns(new BindingList<RecentProjectModel>());
            // OfferBackupRecovery is posted via IDispatcherService.BeginInvoke rather than run
            // synchronously in the constructor (see LaunchViewModel's comment on why) - capture
            // it here so tests can invoke it deliberately instead of relying on a real dispatcher.
            _dispatcher.Setup(d => d.BeginInvoke(It.IsAny<Action>()))
                .Callback<Action>(a => _deferredRecoveryCheck = a);
        }

        private LaunchViewModel CreateSut()
            => new(_navigation.Object, _pubSub.Object, _cache.Object, _messageBox.Object, _dispatcher.Object);

        [Fact]
        public void Constructor_LoadsRecentProjectsFromCache_AndWiresPerItemActions()
        {
            RecentProjectModel project = new() { ProjectID = 1 };
            _cache.SetupGet(c => c.RecentProjects).Returns(new BindingList<RecentProjectModel>([project]));

            LaunchViewModel sut = CreateSut();

            Assert.Same(_cache.Object.RecentProjects, sut.RecentProjects);
            Assert.NotNull(project.EditProject);
            Assert.NotNull(project.DeleteProject);
        }

        [Fact]
        public void Constructor_DefersBackupRecoveryCheckToTheDispatcher()
        {
            CreateSut();

            Assert.NotNull(_deferredRecoveryCheck);
            _cache.Verify(c => c.BackupExists(), Times.Never); // not yet - only once the deferred action runs
        }

        [Fact]
        public void OfferBackupRecovery_NoBackup_NeverPrompts()
        {
            CreateSut();
            _cache.Setup(c => c.BackupExists()).Returns(false);

            _deferredRecoveryCheck();

            _messageBox.Verify(m => m.Show(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>()), Times.Never);
        }

        [Fact]
        public void OfferBackupRecovery_UserDeclines_ClearsBackupWithoutNavigating()
        {
            CreateSut();
            _cache.Setup(c => c.BackupExists()).Returns(true);
            _messageBox.Setup(m => m.Show(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>()))
                .Returns(MessageBoxResult.No);

            _deferredRecoveryCheck();

            _cache.Verify(c => c.ClearBackup(), Times.Once);
            _navigation.Verify(n => n.NavigateTo<EditorViewModel>(), Times.Never);
        }

        [Fact]
        public void OfferBackupRecovery_UserAccepts_LoadsAndLaunchesTheBackup()
        {
            CreateSut();
            _cache.Setup(c => c.BackupExists()).Returns(true);
            _messageBox.Setup(m => m.Show(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>()))
                .Returns(MessageBoxResult.Yes);
            ProjectDetails backup = new() { Title = "Recovered" };
            _cache.Setup(c => c.LoadBackup()).Returns(backup);

            _deferredRecoveryCheck();

            _navigation.Verify(n => n.NavigateTo<EditorViewModel>(), Times.Once);
            _pubSub.Verify(p => p.Publish(SubTopic.ProjectLaunched, backup), Times.Once);
            _cache.Verify(c => c.ClearBackup(), Times.Once);
        }

        [Fact]
        public void OfferBackupRecovery_UnreadableBackup_DiscardsItWithoutCrashing()
        {
            CreateSut();
            _cache.Setup(c => c.BackupExists()).Returns(true);
            _messageBox.Setup(m => m.Show(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>()))
                .Returns(MessageBoxResult.Yes);
            _cache.Setup(c => c.LoadBackup()).Throws(new InvalidOperationException("corrupt"));

            _deferredRecoveryCheck();

            _navigation.Verify(n => n.NavigateTo<EditorViewModel>(), Times.Never);
            _cache.Verify(c => c.ClearBackup(), Times.Once);
        }

        [Fact]
        public void DeleteProjectHandler_DeletesThroughCache()
        {
            RecentProjectModel project = new() { ProjectID = 3 };
            _cache.SetupGet(c => c.RecentProjects).Returns(new BindingList<RecentProjectModel>([project]));
            CreateSut();

            project.DeleteProjectCommand.Execute(null);

            _cache.Verify(c => c.DeleteProject(project), Times.Once);
        }

        [Fact]
        public void EditProjectHandler_LoadsProjectAndNavigatesToEditor()
        {
            RecentProjectModel project = new() { ProjectID = 4 };
            _cache.SetupGet(c => c.RecentProjects).Returns(new BindingList<RecentProjectModel>([project]));
            ProjectDetails loaded = new() { Title = "Opened" };
            _cache.Setup(c => c.LoadProjectFromFile(project)).Returns(loaded);
            CreateSut();

            project.EditProjectCommand.Execute(null);

            _navigation.Verify(n => n.NavigateTo<EditorViewModel>(), Times.Once);
            _pubSub.Verify(p => p.Publish(SubTopic.ProjectLaunched, loaded), Times.Once);
        }

        [Fact]
        public void CreateNewWindowCommand_NavigatesAndPublishesANewProject()
        {
            LaunchViewModel sut = CreateSut();

            sut.CreateNewWindowCommand.Execute(null);

            _navigation.Verify(n => n.NavigateTo<EditorViewModel>(), Times.Once);
            _pubSub.Verify(p => p.Publish(SubTopic.ProjectLaunched, It.IsAny<ProjectDetails>()), Times.Once);
        }
    }
}
