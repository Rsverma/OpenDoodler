using Moq;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using OpenBoardAnim.ViewModels;
using System.ComponentModel;
using Xunit;

namespace OpenBoardAnim.Tests
{
    public class EditorTimelineViewModelTests
    {
        private readonly Mock<IPubSubService> _pubSub = new();
        private readonly Mock<IDialogService> _dialog = new();
        private Action<object> _sceneReplacedHandler;
        private Action<object> _sceneTemplateInsertedHandler;

        public EditorTimelineViewModelTests()
        {
            _pubSub.Setup(p => p.Subscribe(SubTopic.SceneReplaced, It.IsAny<Action<object>>()))
                .Callback<SubTopic, Action<object>>((_, h) => _sceneReplacedHandler = h);
            _pubSub.Setup(p => p.Subscribe(SubTopic.SceneTemplateInserted, It.IsAny<Action<object>>()))
                .Callback<SubTopic, Action<object>>((_, h) => _sceneTemplateInsertedHandler = h);
        }

        private EditorTimelineViewModel CreateSut() => new(_pubSub.Object, _dialog.Object);

        // Mirrors the real convention (see ProjectDetails' default scenes): realSceneCount
        // numbered scenes ("1".."N") followed by a trailing "+" add-scene marker.
        private static BindingList<SceneModel> CreateScenes(int realSceneCount)
        {
            BindingList<SceneModel> scenes = new();
            for (int i = 1; i <= realSceneCount; i++)
                scenes.Add(new SceneModel { Name = i.ToString(), Index = i });
            scenes.Add(new SceneModel { Name = "+", Index = realSceneCount + 1 });
            return scenes;
        }

        [Fact]
        public void SettingScenes_SelectsFirstScene_AndBuildsOneSegmentPerScene()
        {
            EditorTimelineViewModel sut = CreateSut();
            BindingList<SceneModel> scenes = CreateScenes(2);

            sut.Scenes = scenes;

            Assert.Same(scenes[0], sut.SelectedScene);
            Assert.Equal(3, sut.Segments.Count);
        }

        [Fact]
        public void RecomputeSegments_EmptyScenesHaveEqualMinimumWidth()
        {
            EditorTimelineViewModel sut = CreateSut();
            sut.Scenes = CreateScenes(1);

            // Both real scenes and the add-scene marker have no graphics, so every segment
            // falls back to the same minimum width.
            Assert.Equal(sut.Segments[0].Width, sut.Segments[1].Width, 3);
        }

        [Fact]
        public void ZoomIn_RescalesSegmentWidths()
        {
            EditorTimelineViewModel sut = CreateSut();
            sut.Scenes = CreateScenes(1);
            double widthBefore = sut.Segments[0].Width;

            sut.ZoomInCommand.Execute(null);

            Assert.True(sut.Segments[0].Width > widthBefore);
        }

        [Fact]
        public void ZoomLevel_ClampsToConfiguredRange()
        {
            EditorTimelineViewModel sut = CreateSut();

            sut.ZoomLevel = 100;
            Assert.Equal(4.0, sut.ZoomLevel, 3);

            sut.ZoomLevel = 0.0001;
            Assert.Equal(0.25, sut.ZoomLevel, 3);
        }

        [Fact]
        public void SelectingTheAddSceneMarker_InsertsANewSceneBeforeIt()
        {
            EditorTimelineViewModel sut = CreateSut();
            sut.Scenes = CreateScenes(1); // ["1", "+"]
            SceneModel addScene = sut.Scenes[^1];

            sut.SelectedScene = addScene;

            Assert.Equal(3, sut.Scenes.Count); // "1", new "2", "+"
            Assert.Equal("2", sut.Scenes[1].Name);
            Assert.Same(sut.Scenes[1], sut.SelectedScene);
            Assert.Equal(3, addScene.Index); // add marker pushed one further out
        }

        [Fact]
        public void PerSceneDeleteAction_RemovesThatSceneAndRenumbers()
        {
            EditorTimelineViewModel sut = CreateSut();
            sut.Scenes = CreateScenes(3); // ["1","2","3","+"]
            SceneModel middle = sut.Scenes[1];

            middle.SceneDeleteCommand.Execute(null);

            Assert.Equal(3, sut.Scenes.Count);
            Assert.DoesNotContain(middle, sut.Scenes);
        }

        [Fact]
        public void PerSceneDuplicateAction_InsertsACloneRightAfterAndSelectsIt()
        {
            EditorTimelineViewModel sut = CreateSut();
            sut.Scenes = CreateScenes(2); // ["1","2","+"]
            SceneModel first = sut.Scenes[0];

            first.SceneDuplicateCommand.Execute(null);

            Assert.Equal(4, sut.Scenes.Count);
            Assert.Equal("1", sut.Scenes[0].Name);
            Assert.Equal("2", sut.Scenes[1].Name); // the duplicate, renumbered into position
            Assert.Same(sut.Scenes[1], sut.SelectedScene);
            Assert.NotSame(first, sut.Scenes[1]);
        }

        [Fact]
        public void MoveScene_ReordersAndRenumbersScenes()
        {
            EditorTimelineViewModel sut = CreateSut();
            sut.Scenes = CreateScenes(3); // ["1","2","3","+"]
            SceneModel first = sut.Scenes[0];
            SceneModel third = sut.Scenes[2];

            sut.MoveScene(first, third);

            Assert.Equal(sut.Scenes.IndexOf(third) - 1, sut.Scenes.IndexOf(first));
            Assert.Equal("1", sut.Scenes[0].Name);
            Assert.Equal("2", sut.Scenes[1].Name);
            Assert.Same(first, sut.SelectedScene);
        }

        [Fact]
        public void MoveScene_IgnoresTheAddSceneMarkerAsDraggedOrTarget()
        {
            EditorTimelineViewModel sut = CreateSut();
            sut.Scenes = CreateScenes(2); // ["1","2","+"]
            SceneModel addScene = sut.Scenes[^1];
            SceneModel first = sut.Scenes[0];

            sut.MoveScene(addScene, first);
            sut.MoveScene(first, addScene);

            Assert.Equal(3, sut.Scenes.Count);
            Assert.Same(addScene, sut.Scenes[^1]);
        }

        [Fact]
        public void MovePlayheadPreview_ClampsWithinRealContentBounds()
        {
            EditorTimelineViewModel sut = CreateSut();
            sut.Scenes = CreateScenes(2);

            sut.MovePlayheadPreview(-99999);
            Assert.Equal(0, sut.PlayheadX, 3);

            sut.MovePlayheadPreview(99999 + 99999);
            Assert.Equal(sut.RealContentWidth, sut.PlayheadX, 3);
        }

        [Fact]
        public void CommitPlayheadPosition_SelectsTheNearestRealScene()
        {
            EditorTimelineViewModel sut = CreateSut();
            sut.Scenes = CreateScenes(2); // ["1","2","+"]
            SceneModel secondScene = sut.Scenes[1];
            double secondSegmentCenter = sut.Segments[1].X + sut.Segments[1].Width / 2;
            sut.PlayheadX = secondSegmentCenter;

            sut.CommitPlayheadPosition();

            Assert.Same(secondScene, sut.SelectedScene);
        }

        [Fact]
        public void SceneReplacedHandler_SwapsInTheNewSceneAtTheSelectedIndex()
        {
            EditorTimelineViewModel sut = CreateSut();
            sut.Scenes = CreateScenes(2); // ["1","2","+"], selected = "1"
            SceneModel replacement = new() { Name = "replacement" };

            _sceneReplacedHandler(replacement);

            Assert.Same(replacement, sut.Scenes[0]);
            Assert.Same(replacement, sut.SelectedScene);
            Assert.Equal(1, replacement.Index);
        }

        [Fact]
        public void SceneTemplateInsertedHandler_InsertsAfterTheSelectedScene()
        {
            EditorTimelineViewModel sut = CreateSut();
            sut.Scenes = CreateScenes(2); // ["1","2","+"], selected = "1"
            SceneModel template = new() { Name = "template" };

            _sceneTemplateInsertedHandler(template);

            Assert.Equal(4, sut.Scenes.Count);
            Assert.Same(template, sut.Scenes[1]);
            Assert.Same(template, sut.SelectedScene);
        }

        [Fact]
        public void PreviewSceneCommand_CanExecute_RequiresAnOpenProject()
        {
            EditorTimelineViewModel sut = CreateSut();
            sut.Scenes = CreateScenes(1);

            Assert.False(sut.PreviewSceneCommand.CanExecute(sut.Scenes[0]));

            sut.Project = new ProjectDetails();
            Assert.True(sut.PreviewSceneCommand.CanExecute(sut.Scenes[0]));
        }

        [Fact]
        public void PreviewSceneCommand_ShowsDialogAndClearsPreviewIndexAfterwards()
        {
            EditorTimelineViewModel sut = CreateSut();
            sut.Scenes = CreateScenes(1);
            sut.Project = new ProjectDetails();

            sut.PreviewSceneCommand.Execute(sut.Scenes[0]);

            _dialog.Verify(d => d.ShowDialog(DialogType.PreviewProject, sut.Project), Times.Once);
            Assert.Null(sut.Project.PreviewSceneIndex);
        }
    }
}
