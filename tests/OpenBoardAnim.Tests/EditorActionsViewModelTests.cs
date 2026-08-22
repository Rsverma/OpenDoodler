using Moq;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using OpenBoardAnim.ViewModels;
using System.Windows;
using Xunit;

namespace OpenBoardAnim.Tests
{
    public class EditorActionsViewModelTests
    {
        private readonly Mock<IPubSubService> _pubSub = new();
        private readonly Mock<INavigationService> _navigation = new();
        private readonly Mock<ICacheService> _cache = new();
        private readonly Mock<IDialogService> _dialog = new();
        private readonly Mock<IFileDialogService> _fileDialog = new();
        private readonly Mock<IMessageBoxService> _messageBox = new();

        private EditorActionsViewModel CreateSut()
            => new(_pubSub.Object, _navigation.Object, _cache.Object, _dialog.Object, _fileDialog.Object, _messageBox.Object);

        private static DrawingModel Graphic(double x = 0, double y = 0, double width = 100, double height = 100, bool isLocked = false)
            => new() { X = x, Y = y, Width = width, Height = height, IsLocked = isLocked };

        private static SceneModel SceneWith(params GraphicModelBase[] graphics)
        {
            SceneModel scene = new();
            foreach (GraphicModelBase g in graphics)
                scene.Graphics.Add(g);
            return scene;
        }

        // --- Move ordering (later index paints on top) ---

        [Fact]
        public void MoveUp_MovesGraphicOneIndexTowardTheFront()
        {
            EditorActionsViewModel sut = CreateSut();
            DrawingModel a = Graphic(), b = Graphic(), c = Graphic();
            sut.CurrentScene = SceneWith(a, b, c);
            sut.SelectedGraphic = a;

            sut.MoveUpCommand.Execute(null);

            Assert.Equal([b, a, c], sut.CurrentScene.Graphics);
            Assert.Same(a, sut.SelectedGraphic);
        }

        [Fact]
        public void MoveUp_NoOp_WhenAlreadyAtFront()
        {
            EditorActionsViewModel sut = CreateSut();
            DrawingModel a = Graphic(), b = Graphic();
            sut.CurrentScene = SceneWith(a, b);
            sut.SelectedGraphic = b;

            sut.MoveUpCommand.Execute(null);

            Assert.Equal([a, b], sut.CurrentScene.Graphics);
        }

        [Fact]
        public void MoveDown_MovesGraphicOneIndexTowardTheBack()
        {
            EditorActionsViewModel sut = CreateSut();
            DrawingModel a = Graphic(), b = Graphic(), c = Graphic();
            sut.CurrentScene = SceneWith(a, b, c);
            sut.SelectedGraphic = c;

            sut.MoveDownCommand.Execute(null);

            Assert.Equal([a, c, b], sut.CurrentScene.Graphics);
        }

        [Fact]
        public void MoveTop_JumpsGraphicToTheFront()
        {
            EditorActionsViewModel sut = CreateSut();
            DrawingModel a = Graphic(), b = Graphic(), c = Graphic();
            sut.CurrentScene = SceneWith(a, b, c);
            sut.SelectedGraphic = a;

            sut.MoveTopCommand.Execute(null);

            Assert.Equal([b, c, a], sut.CurrentScene.Graphics);
        }

        [Fact]
        public void MoveBottom_JumpsGraphicToTheBack()
        {
            EditorActionsViewModel sut = CreateSut();
            DrawingModel a = Graphic(), b = Graphic(), c = Graphic();
            sut.CurrentScene = SceneWith(a, b, c);
            sut.SelectedGraphic = c;

            sut.MoveBottomCommand.Execute(null);

            Assert.Equal([c, a, b], sut.CurrentScene.Graphics);
        }

        // --- Selection fallback ---

        [Fact]
        public void GetSelectedGraphicsOrFallback_PrefersMultiSelectListOverSingleSelection()
        {
            EditorActionsViewModel sut = CreateSut();
            DrawingModel single = Graphic();
            DrawingModel multiA = Graphic(), multiB = Graphic();
            sut.SelectedGraphic = single;
            sut.SelectedGraphics = [multiA, multiB];

            Assert.Equal([multiA, multiB], sut.GetSelectedGraphicsOrFallback());
        }

        [Fact]
        public void GetSelectedGraphicsOrFallback_FallsBackToSingleSelection_WhenMultiSelectEmpty()
        {
            EditorActionsViewModel sut = CreateSut();
            DrawingModel single = Graphic();
            sut.SelectedGraphic = single;

            Assert.Equal([single], sut.GetSelectedGraphicsOrFallback());
        }

        // --- Delete ---

        [Fact]
        public void DeleteItem_RemovesEverySelectedGraphicFromCurrentScene()
        {
            EditorActionsViewModel sut = CreateSut();
            DrawingModel a = Graphic(), b = Graphic(), c = Graphic();
            sut.CurrentScene = SceneWith(a, b, c);
            sut.SelectedGraphics = [a, c];

            sut.DeleteItemCommand.Execute(null);

            Assert.Equal([b], sut.CurrentScene.Graphics);
        }

        // --- Nudge ---
        // NudgeSelectedGraphic reads live Keyboard.Modifiers to pick a 1px/10px step - in this
        // headless test host that throws (no real input device/message loop), which the
        // method's catch-and-log swallows silently (LogAndShow doesn't rethrow - see
        // LogWriter.LogError), so it never reaches the switch that would move the graphic.
        // That's the same "live-interaction-only, not worth testing" case the refactor plan
        // called out for Keyboard.Modifiers - only the locked-skip invariant is verified here,
        // since it holds regardless of whether the method exits via IsLocked or via the swallowed
        // exception.

        [Fact]
        public void NudgeSelectedGraphic_SkipsLockedGraphics()
        {
            EditorActionsViewModel sut = CreateSut();
            DrawingModel locked = Graphic(x: 50, y: 50, isLocked: true);
            sut.CurrentScene = SceneWith(locked);
            sut.SelectedGraphic = locked;

            sut.NudgeSelectedGraphicCommand.Execute("Left");

            Assert.Equal(50, locked.X);
        }

        // --- Copy / Cut / Paste / Duplicate ---

        [Fact]
        public void CopyThenPaste_OffsetsPastedGraphicAndPublishesGraphicAdded()
        {
            EditorActionsViewModel sut = CreateSut();
            DrawingModel source = Graphic(x: 10, y: 20);
            sut.CurrentScene = SceneWith(source);
            sut.SelectedGraphic = source;

            sut.CopyGraphicCommand.Execute(null);
            sut.PasteGraphicCommand.Execute(null);

            _pubSub.Verify(p => p.Publish(SubTopic.GraphicAdded, It.Is<GraphicModelBase>(g => g.X == 30 && g.Y == 40)), Times.Once);
            Assert.NotSame(source, sut.SelectedGraphic);
        }

        [Fact]
        public void PasteGraphicCommand_CanExecute_RequiresClipboardAndCurrentScene()
        {
            EditorActionsViewModel sut = CreateSut();
            Assert.False(sut.PasteGraphicCommand.CanExecute(null));

            sut.CurrentScene = SceneWith(Graphic());
            sut.SelectedGraphic = sut.CurrentScene.Graphics[0];
            sut.CopyGraphicCommand.Execute(null);

            Assert.True(sut.PasteGraphicCommand.CanExecute(null));
        }

        [Fact]
        public void CutSelectedGraphic_RemovesFromSceneAndKeepsAClipboardCopy()
        {
            EditorActionsViewModel sut = CreateSut();
            DrawingModel g = Graphic();
            sut.CurrentScene = SceneWith(g);
            sut.SelectedGraphic = g;

            sut.CutGraphicCommand.Execute(null);

            Assert.Empty(sut.CurrentScene.Graphics);
            Assert.True(sut.PasteGraphicCommand.CanExecute(null));
        }

        [Fact]
        public void DuplicateSelectedGraphic_OffsetsCopyAndPublishesGraphicAdded()
        {
            EditorActionsViewModel sut = CreateSut();
            DrawingModel source = Graphic(x: 5, y: 5);
            sut.CurrentScene = SceneWith(source);
            sut.SelectedGraphic = source;

            sut.DuplicateGraphicCommand.Execute(null);

            _pubSub.Verify(p => p.Publish(SubTopic.GraphicAdded, It.Is<GraphicModelBase>(g => g.X == 25 && g.Y == 25)), Times.Once);
        }

        // --- Lock / hide / group ---

        [Fact]
        public void ToggleLock_LocksWholeSelection_WhenAnyMemberIsUnlocked()
        {
            EditorActionsViewModel sut = CreateSut();
            DrawingModel locked = Graphic(isLocked: true), unlocked = Graphic(isLocked: false);
            sut.SelectedGraphics = [locked, unlocked];

            sut.ToggleLockCommand.Execute(null);

            Assert.True(locked.IsLocked);
            Assert.True(unlocked.IsLocked);
        }

        [Fact]
        public void ToggleLock_UnlocksWholeSelection_WhenAllMembersAlreadyLocked()
        {
            EditorActionsViewModel sut = CreateSut();
            DrawingModel a = Graphic(isLocked: true), b = Graphic(isLocked: true);
            sut.SelectedGraphics = [a, b];

            sut.ToggleLockCommand.Execute(null);

            Assert.False(a.IsLocked);
            Assert.False(b.IsLocked);
        }

        [Fact]
        public void HideSelectedGraphics_SetsIsVisibleFalse()
        {
            EditorActionsViewModel sut = CreateSut();
            DrawingModel g = Graphic();
            sut.SelectedGraphics = [g];

            sut.HideGraphicCommand.Execute(null);

            Assert.False(g.IsVisible);
        }

        [Fact]
        public void GroupSelectedGraphics_AssignsSharedGroupId_ForTwoOrMore()
        {
            EditorActionsViewModel sut = CreateSut();
            DrawingModel a = Graphic(), b = Graphic();
            sut.SelectedGraphics = [a, b];

            sut.GroupGraphicsCommand.Execute(null);

            Assert.NotNull(a.GroupId);
            Assert.Equal(a.GroupId, b.GroupId);
        }

        [Fact]
        public void UngroupSelectedGraphics_ClearsGroupId()
        {
            EditorActionsViewModel sut = CreateSut();
            Guid groupId = Guid.NewGuid();
            DrawingModel a = Graphic(), b = Graphic();
            a.GroupId = groupId;
            b.GroupId = groupId;
            sut.SelectedGraphics = [a, b];

            sut.UngroupGraphicsCommand.Execute(null);

            Assert.Null(a.GroupId);
            Assert.Null(b.GroupId);
        }

        // --- Align ---

        [Fact]
        public void AlignLeft_MovesUnlockedGraphicsToLeftmostX()
        {
            EditorActionsViewModel sut = CreateSut();
            DrawingModel a = Graphic(x: 10), b = Graphic(x: 50), locked = Graphic(x: 90, isLocked: true);
            sut.SelectedGraphics = [a, b, locked];

            sut.AlignLeftCommand.Execute(null);

            Assert.Equal(10, a.X);
            Assert.Equal(10, b.X);
            Assert.Equal(90, locked.X); // locked graphics anchor the line but never move themselves
        }

        [Fact]
        public void AlignRight_MovesUnlockedGraphicsSoTheirRightEdgesAlign()
        {
            EditorActionsViewModel sut = CreateSut();
            DrawingModel a = Graphic(x: 0, width: 100), b = Graphic(x: 50, width: 30);
            sut.SelectedGraphics = [a, b];

            sut.AlignRightCommand.Execute(null);

            // right edges: a=100, b=80 -> shared right = 100
            Assert.Equal(0, a.X);  // already flush with the shared right edge
            Assert.Equal(70, b.X); // moved so its right edge (70+30) reaches 100
        }

        [Fact]
        public void AlignCenter_CentersUnlockedGraphicsOnSharedMidpoint()
        {
            EditorActionsViewModel sut = CreateSut();
            DrawingModel a = Graphic(x: 0, width: 100), b = Graphic(x: 200, width: 100);
            sut.SelectedGraphics = [a, b];

            sut.AlignCenterCommand.Execute(null);

            // left=0, right=300, centerX=150
            Assert.Equal(100, a.X);
            Assert.Equal(100, b.X);
        }

        [Fact]
        public void AlignTop_MovesUnlockedGraphicsToTopmostY()
        {
            EditorActionsViewModel sut = CreateSut();
            DrawingModel a = Graphic(y: 30), b = Graphic(y: 80);
            sut.SelectedGraphics = [a, b];

            sut.AlignTopCommand.Execute(null);

            Assert.Equal(30, a.Y);
            Assert.Equal(30, b.Y);
        }

        [Fact]
        public void AlignBottom_MovesUnlockedGraphicsSoTheirBottomEdgesAlign()
        {
            EditorActionsViewModel sut = CreateSut();
            DrawingModel a = Graphic(y: 0, height: 50), b = Graphic(y: 100, height: 100);
            sut.SelectedGraphics = [a, b];

            sut.AlignBottomCommand.Execute(null);

            // bottom = max(50, 200) = 200
            Assert.Equal(150, a.Y);
            Assert.Equal(100, b.Y);
        }

        [Fact]
        public void AlignMiddle_CentersUnlockedGraphicsOnSharedVerticalMidpoint()
        {
            EditorActionsViewModel sut = CreateSut();
            DrawingModel a = Graphic(y: 0, height: 100), b = Graphic(y: 200, height: 100);
            sut.SelectedGraphics = [a, b];

            sut.AlignMiddleCommand.Execute(null);

            // top=0, bottom=300, centerY=150
            Assert.Equal(100, a.Y);
            Assert.Equal(100, b.Y);
        }

        [Fact]
        public void AlignCommands_CanExecute_RequireAtLeastTwoSelectedGraphics()
        {
            EditorActionsViewModel sut = CreateSut();
            sut.SelectedGraphics = [Graphic()];
            Assert.False(sut.AlignLeftCommand.CanExecute(null));

            sut.SelectedGraphics = [Graphic(), Graphic()];
            Assert.True(sut.AlignLeftCommand.CanExecute(null));
        }

        // --- Save / unsaved-changes tracking ---

        [Fact]
        public void SaveProject_WithExistingPath_SkipsFileDialogAndUpdatesExistingProject()
        {
            EditorActionsViewModel sut = CreateSut();
            sut.Project = new ProjectDetails { Path = "C:\\already\\saved.obap" };

            sut.SaveProjectCommand.Execute(null);

            _fileDialog.Verify(f => f.ShowSaveFileDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _cache.Verify(c => c.UpdateExistingProject(sut.Project), Times.Once);
            _cache.Verify(c => c.ClearBackup(), Times.Once);
            Assert.False(sut.HasUnsavedChanges);
        }

        [Fact]
        public void SaveProject_WithNoPath_PromptsAndSavesNewProject_WhenUserPicksAFile()
        {
            EditorActionsViewModel sut = CreateSut();
            sut.Project = new ProjectDetails();
            _fileDialog.Setup(f => f.ShowSaveFileDialog(It.IsAny<string>(), null, null)).Returns("C:\\new\\project.obap");

            sut.SaveProjectCommand.Execute(null);

            _cache.Verify(c => c.SaveNewProject(sut.Project, "C:\\new\\project.obap"), Times.Once);
            _cache.Verify(c => c.UpdateExistingProject(sut.Project), Times.Once);
        }

        [Fact]
        public void SaveProject_WithNoPath_DoesNothing_WhenUserCancelsFileDialog()
        {
            EditorActionsViewModel sut = CreateSut();
            sut.Project = new ProjectDetails();
            _fileDialog.Setup(f => f.ShowSaveFileDialog(It.IsAny<string>(), null, null)).Returns((string)null);

            sut.SaveProjectCommand.Execute(null);

            _cache.Verify(c => c.SaveNewProject(It.IsAny<ProjectDetails>(), It.IsAny<string>()), Times.Never);
            _cache.Verify(c => c.UpdateExistingProject(It.IsAny<ProjectDetails>()), Times.Never);
        }

        [Fact]
        public void SaveProjectCommand_CanExecute_RequiresAnOpenProject()
        {
            EditorActionsViewModel sut = CreateSut();
            Assert.False(sut.SaveProjectCommand.CanExecute(null));

            sut.Project = new ProjectDetails();
            Assert.True(sut.SaveProjectCommand.CanExecute(null));
        }

        [Fact]
        public void MarkProjectSaved_ThenNoEdits_HasUnsavedChangesIsFalse()
        {
            EditorActionsViewModel sut = CreateSut();
            sut.Project = new ProjectDetails();

            sut.MarkProjectSaved();

            Assert.False(sut.HasUnsavedChanges);
        }

        [Fact]
        public void EditAfterMarkProjectSaved_HasUnsavedChangesIsTrue()
        {
            EditorActionsViewModel sut = CreateSut();
            sut.Project = new ProjectDetails();
            sut.MarkProjectSaved();

            sut.Project.Title = "Changed";

            Assert.True(sut.HasUnsavedChanges);
        }

        [Fact]
        public void MarkProjectUnsaved_ForcesHasUnsavedChangesTrue()
        {
            EditorActionsViewModel sut = CreateSut();
            sut.Project = new ProjectDetails();
            sut.MarkProjectSaved();

            sut.MarkProjectUnsaved();

            Assert.True(sut.HasUnsavedChanges);
        }

        // --- Unsaved-changes confirmation ---

        [Fact]
        public void ConfirmDiscardUnsavedChanges_ReturnsTrue_WithoutPrompting_WhenNothingUnsaved()
        {
            EditorActionsViewModel sut = CreateSut();
            sut.Project = new ProjectDetails();
            sut.MarkProjectSaved();

            bool result = sut.ConfirmDiscardUnsavedChanges();

            Assert.True(result);
            _messageBox.Verify(m => m.Show(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>()), Times.Never);
        }

        [Fact]
        public void ConfirmDiscardUnsavedChanges_No_ReturnsTrue_WithoutSaving()
        {
            EditorActionsViewModel sut = CreateSut();
            sut.Project = new ProjectDetails { Path = "C:\\p.obap" };
            sut.MarkProjectSaved();
            sut.Project.Title = "Changed";
            _messageBox.Setup(m => m.Show(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>()))
                .Returns(MessageBoxResult.No);

            bool result = sut.ConfirmDiscardUnsavedChanges();

            Assert.True(result);
            _cache.Verify(c => c.UpdateExistingProject(It.IsAny<ProjectDetails>()), Times.Never);
        }

        [Fact]
        public void ConfirmDiscardUnsavedChanges_Cancel_ReturnsFalse()
        {
            EditorActionsViewModel sut = CreateSut();
            sut.Project = new ProjectDetails { Path = "C:\\p.obap" };
            sut.MarkProjectSaved();
            sut.Project.Title = "Changed";
            _messageBox.Setup(m => m.Show(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>()))
                .Returns(MessageBoxResult.Cancel);

            Assert.False(sut.ConfirmDiscardUnsavedChanges());
        }

        [Fact]
        public void ConfirmDiscardUnsavedChanges_Yes_SavesAndReturnsWhetherStillUnsaved()
        {
            EditorActionsViewModel sut = CreateSut();
            sut.Project = new ProjectDetails { Path = "C:\\p.obap" };
            sut.MarkProjectSaved();
            sut.Project.Title = "Changed";
            _messageBox.Setup(m => m.Show(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>()))
                .Returns(MessageBoxResult.Yes);

            bool result = sut.ConfirmDiscardUnsavedChanges();

            Assert.True(result);
            _cache.Verify(c => c.UpdateExistingProject(sut.Project), Times.Once);
            Assert.False(sut.HasUnsavedChanges);
        }

        // --- Close project ---

        [Fact]
        public void CloseProject_ClearsProjectAndNavigatesToLaunch_WhenConfirmed()
        {
            EditorActionsViewModel sut = CreateSut();
            sut.Project = new ProjectDetails();
            sut.MarkProjectSaved(); // no unsaved changes -> confirm succeeds without prompting

            sut.CloseProjectCommand.Execute(null);

            Assert.Null(sut.Project);
            _cache.Verify(c => c.ClearBackup(), Times.Once);
            _navigation.Verify(n => n.NavigateTo<LaunchViewModel>(), Times.Once);
        }

        [Fact]
        public void CloseProject_DoesNothing_WhenUserCancelsDiscardPrompt()
        {
            EditorActionsViewModel sut = CreateSut();
            sut.Project = new ProjectDetails { Path = "C:\\p.obap" };
            sut.MarkProjectSaved();
            sut.Project.Title = "Changed";
            _messageBox.Setup(m => m.Show(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>()))
                .Returns(MessageBoxResult.Cancel);

            sut.CloseProjectCommand.Execute(null);

            Assert.NotNull(sut.Project);
            _navigation.Verify(n => n.NavigateTo<LaunchViewModel>(), Times.Never);
        }

        // --- Scene-changed subscription ---

        [Fact]
        public void Constructor_SubscribesToSceneChanged_AndUpdatesCurrentScene()
        {
            Action<object> capturedHandler = null;
            _pubSub.Setup(p => p.Subscribe(SubTopic.SceneChanged, It.IsAny<Action<object>>()))
                .Callback<SubTopic, Action<object>>((_, handler) => capturedHandler = handler);

            EditorActionsViewModel sut = CreateSut();
            SceneModel newScene = new();

            Assert.NotNull(capturedHandler);
            capturedHandler(newScene);

            Assert.Same(newScene, sut.CurrentScene);
        }

        // --- Export command gating ---

        [Fact]
        public void ExportProjectCommand_CanExecute_FalseWhileExporting()
        {
            EditorActionsViewModel sut = CreateSut();
            Assert.True(sut.ExportProjectCommand.CanExecute(null));

            sut.IsExporting = true;

            Assert.False(sut.ExportProjectCommand.CanExecute(null));
            Assert.True(sut.CancelExportCommand.CanExecute(null));
        }
    }
}
