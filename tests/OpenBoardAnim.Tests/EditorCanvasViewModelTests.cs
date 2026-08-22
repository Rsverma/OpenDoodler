using Moq;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using OpenBoardAnim.ViewModels;
using Xunit;

namespace OpenBoardAnim.Tests
{
    public class EditorCanvasViewModelTests
    {
        private readonly Mock<IPubSubService> _pubSub = new();
        private Action<object> _sceneChangedHandler;
        private Action<object> _graphicAddedHandler;

        public EditorCanvasViewModelTests()
        {
            _pubSub.Setup(p => p.Subscribe(SubTopic.SceneChanged, It.IsAny<Action<object>>()))
                .Callback<SubTopic, Action<object>>((_, h) => _sceneChangedHandler = h);
            _pubSub.Setup(p => p.Subscribe(SubTopic.GraphicAdded, It.IsAny<Action<object>>()))
                .Callback<SubTopic, Action<object>>((_, h) => _graphicAddedHandler = h);
        }

        private EditorCanvasViewModel CreateSut() => new(_pubSub.Object);

        [Fact]
        public void Constructor_SubscribesToSceneChangedAndGraphicAdded()
        {
            CreateSut();

            Assert.NotNull(_sceneChangedHandler);
            Assert.NotNull(_graphicAddedHandler);
        }

        [Fact]
        public void ZoomInCommand_MultipliesZoomLevelByStep()
        {
            EditorCanvasViewModel sut = CreateSut();

            sut.ZoomInCommand.Execute(null);

            Assert.Equal(1.25, sut.ZoomLevel, 3);
        }

        [Fact]
        public void ZoomOutCommand_DividesZoomLevelByStep()
        {
            EditorCanvasViewModel sut = CreateSut();

            sut.ZoomOutCommand.Execute(null);

            Assert.Equal(0.8, sut.ZoomLevel, 3);
        }

        [Fact]
        public void ZoomLevel_ClampsToMaximum()
        {
            EditorCanvasViewModel sut = CreateSut();

            sut.ZoomLevel = 100;

            Assert.Equal(4.0, sut.ZoomLevel, 3);
        }

        [Fact]
        public void ZoomLevel_ClampsToMinimum()
        {
            EditorCanvasViewModel sut = CreateSut();

            sut.ZoomLevel = 0.001;

            Assert.Equal(0.25, sut.ZoomLevel, 3);
        }

        [Fact]
        public void ResetZoomCommand_SetsZoomLevelToOne()
        {
            EditorCanvasViewModel sut = CreateSut();
            sut.ZoomLevel = 2.0;

            sut.ResetZoomCommand.Execute(null);

            Assert.Equal(1.0, sut.ZoomLevel, 3);
        }

        [Fact]
        public void ResetZoomCommand_CanExecute_FalseWhenAlreadyAtOne()
        {
            EditorCanvasViewModel sut = CreateSut();

            Assert.False(sut.ResetZoomCommand.CanExecute(null));

            sut.ZoomLevel = 2.0;
            Assert.True(sut.ResetZoomCommand.CanExecute(null));
        }

        [Fact]
        public void ZoomInCommand_CanExecute_FalseAtMaximum()
        {
            EditorCanvasViewModel sut = CreateSut();
            sut.ZoomLevel = 4.0;

            Assert.False(sut.ZoomInCommand.CanExecute(null));
        }

        [Fact]
        public void ZoomOutCommand_CanExecute_FalseAtMinimum()
        {
            EditorCanvasViewModel sut = CreateSut();
            sut.ZoomLevel = 0.25;

            Assert.False(sut.ZoomOutCommand.CanExecute(null));
        }

        [Fact]
        public void ZoomPercentageText_FormatsAsWholePercent()
        {
            EditorCanvasViewModel sut = CreateSut();
            sut.ZoomLevel = 1.5;

            Assert.Equal("150%", sut.ZoomPercentageText);
        }

        [Fact]
        public void SceneChangedHandler_ReplacesSceneGraphics()
        {
            EditorCanvasViewModel sut = CreateSut();
            SceneModel scene = new();
            scene.Graphics.Add(new DrawingModel());

            _sceneChangedHandler(scene);

            Assert.Same(scene.Graphics, sut.SceneGraphics);
        }

        [Fact]
        public void GraphicAddedHandler_AppendsToCurrentSceneGraphics()
        {
            EditorCanvasViewModel sut = CreateSut();
            SceneModel scene = new();
            _sceneChangedHandler(scene);
            DrawingModel added = new();

            _graphicAddedHandler(added);

            Assert.Contains(added, sut.SceneGraphics);
        }

        [Fact]
        public void GraphicAddedHandler_IsNoOp_WhenNoSceneIsActiveYet()
        {
            EditorCanvasViewModel sut = CreateSut();

            _graphicAddedHandler(new DrawingModel());

            Assert.Null(sut.SceneGraphics);
        }
    }
}
