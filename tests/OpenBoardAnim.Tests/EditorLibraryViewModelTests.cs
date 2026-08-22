using Moq;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using OpenBoardAnim.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Xunit;

namespace OpenBoardAnim.Tests
{
    // AddTextCommandHandler itself is not exercised here - it calls GeometryHelper.ConvertTextToGeometry,
    // which needs Application.Current.MainWindow for DPI (real WPF rendering, out of scope - same as
    // GeometryHelper/PathAnimationHelper elsewhere). Its CanExecute logic (pure) is covered.
    public class EditorLibraryViewModelTests
    {
        private readonly Mock<IPubSubService> _pubSub = new();
        private readonly Mock<ICacheService> _cache = new();
        private readonly Mock<IDialogService> _dialog = new();
        private readonly Mock<IOpenFileDialogService> _openFileDialog = new();
        private readonly Mock<IMessageBoxService> _messageBox = new();
        private Action<object> _sceneChangedHandler;

        public EditorLibraryViewModelTests()
        {
            _cache.SetupGet(c => c.LoadedGraphics).Returns(new BindingList<DrawingModel>());
            _cache.SetupGet(c => c.AllShapes).Returns(new BindingList<DrawingModel>());
            _cache.SetupGet(c => c.LoadedSceneTemplates).Returns(new BindingList<SceneTemplateModel>());
            _pubSub.Setup(p => p.Subscribe(SubTopic.SceneChanged, It.IsAny<Action<object>>()))
                .Callback<SubTopic, Action<object>>((_, h) => _sceneChangedHandler = h);
        }

        private EditorLibraryViewModel CreateSut()
            => new(_pubSub.Object, _cache.Object, _dialog.Object, _openFileDialog.Object, _messageBox.Object);

        [Fact]
        public void Constructor_LoadsGraphicsShapesAndTemplatesFromCache()
        {
            BindingList<DrawingModel> graphics = new([new DrawingModel { ID = 1 }]);
            BindingList<DrawingModel> shapes = new([new DrawingModel { ID = 2 }]);
            _cache.SetupGet(c => c.LoadedGraphics).Returns(graphics);
            _cache.SetupGet(c => c.AllShapes).Returns(shapes);

            EditorLibraryViewModel sut = CreateSut();

            Assert.Same(graphics, sut.Graphics);
            Assert.Same(shapes, sut.Shapes);
            Assert.NotNull(graphics[0].AddGraphic);
            Assert.NotNull(graphics[0].DeleteGraphic);
            Assert.NotNull(shapes[0].AddGraphic);
        }

        [Fact]
        public void AddGraphicHandler_PublishesAClonedGraphicAdded()
        {
            DrawingModel graphic = new() { ID = 5, Name = "Original" };
            _cache.SetupGet(c => c.LoadedGraphics).Returns(new BindingList<DrawingModel>([graphic]));
            EditorLibraryViewModel sut = CreateSut();

            graphic.AddGraphic.Invoke(graphic);

            _pubSub.Verify(p => p.Publish(SubTopic.GraphicAdded, It.Is<DrawingModel>(g => g.ID == 5 && !ReferenceEquals(g, graphic))), Times.Once);
        }

        [Fact]
        public void DeleteGraphicHandler_DeletesThroughCache()
        {
            DrawingModel graphic = new() { ID = 7 };
            _cache.SetupGet(c => c.LoadedGraphics).Returns(new BindingList<DrawingModel>([graphic]));
            EditorLibraryViewModel sut = CreateSut();

            graphic.DeleteGraphic.Invoke(graphic);

            _cache.Verify(c => c.DeleteGraphic(graphic), Times.Once);
        }

        [Fact]
        public void SearchGraphicsCommand_ReplacesGraphicsFromCache()
        {
            EditorLibraryViewModel sut = CreateSut();
            _cache.Setup(c => c.GetGraphics("cat", 0)).Returns([new DrawingModel { ID = 9, Name = "Cat" }]);
            sut.SearchText = "cat";

            sut.SearchGraphicsCommand.Execute(null);

            DrawingModel result = Assert.Single(sut.Graphics);
            Assert.Equal(9, result.ID);
            Assert.NotNull(result.AddGraphic);
        }

        [Fact]
        public void LoadMoreGraphicsCommand_UsesLastLoadedGraphicIdAsOffset()
        {
            _cache.SetupGet(c => c.LoadedGraphics).Returns(new BindingList<DrawingModel>([new DrawingModel { ID = 20 }]));
            EditorLibraryViewModel sut = CreateSut();
            _cache.Setup(c => c.GetGraphics("", 20)).Returns([new DrawingModel { ID = 21 }]);

            sut.LoadMoreGraphicsCommand.Execute(null);

            Assert.Equal(2, sut.Graphics.Count);
            _cache.Verify(c => c.GetGraphics(It.IsAny<string>(), 20), Times.Once);
        }

        [Fact]
        public async Task ImportGraphicsCommand_SavesPickedFiles_AndReloadsGraphics()
        {
            EditorLibraryViewModel sut = CreateSut();
            _openFileDialog.Setup(f => f.ShowOpenFileDialog("SVG File (*.svg)|*.svg", true)).Returns(["a.svg", "b.svg"]);
            BindingList<DrawingModel> reloaded = new([new DrawingModel { ID = 1 }]);
            _cache.SetupGet(c => c.LoadedGraphics).Returns(reloaded);

            sut.ImportGraphicsCommand.Execute(null);
            await Task.Delay(10); // let the async void handler complete

            _cache.Verify(c => c.SaveNewGraphics(new[] { "a.svg", "b.svg" }), Times.Once);
            Assert.Same(reloaded, sut.Graphics);
        }

        [Fact]
        public async Task ImportGraphicsCommand_UserCancels_StillReloadsButNeverSaves()
        {
            EditorLibraryViewModel sut = CreateSut();
            _openFileDialog.Setup(f => f.ShowOpenFileDialog(It.IsAny<string>(), true)).Returns([]);

            sut.ImportGraphicsCommand.Execute(null);
            await Task.Delay(10);

            _cache.Verify(c => c.SaveNewGraphics(It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public void CleanupInvalidGraphicsCommand_ShowsCountWhenGraphicsWereRemoved()
        {
            EditorLibraryViewModel sut = CreateSut();
            _cache.Setup(c => c.CleanupInvalidGraphics()).Returns(3);

            sut.CleanupInvalidGraphicsCommand.Execute(null);

            _messageBox.Verify(m => m.Show(
                "Removed 3 invalid graphic(s) from the library.",
                It.IsAny<string>(), It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>()), Times.Once);
        }

        [Fact]
        public void CleanupInvalidGraphicsCommand_ShowsNoneFoundMessage_WhenNothingRemoved()
        {
            EditorLibraryViewModel sut = CreateSut();
            _cache.Setup(c => c.CleanupInvalidGraphics()).Returns(0);

            sut.CleanupInvalidGraphicsCommand.Execute(null);

            _messageBox.Verify(m => m.Show(
                "No invalid graphics found.",
                It.IsAny<string>(), It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>()), Times.Once);
        }

        [Fact]
        public void ManageLibraryCommand_ShowsLibraryManagerDialog()
        {
            EditorLibraryViewModel sut = CreateSut();

            sut.ManageLibraryCommand.Execute(null);

            _dialog.Verify(d => d.ShowDialog(DialogType.LibraryManager, sut), Times.Once);
        }

        [Fact]
        public void SaveCurrentSceneAsTemplateCommand_CanExecute_RequiresACurrentScene()
        {
            EditorLibraryViewModel sut = CreateSut();
            Assert.False(sut.SaveCurrentSceneAsTemplateCommand.CanExecute(null));

            _sceneChangedHandler(new SceneModel());
            Assert.True(sut.SaveCurrentSceneAsTemplateCommand.CanExecute(null));
        }

        [Fact]
        public void SaveCurrentSceneAsTemplateCommand_PromptsWithTheCurrentScene()
        {
            EditorLibraryViewModel sut = CreateSut();
            SceneModel scene = new() { Name = "My Scene" };
            _sceneChangedHandler(scene);

            sut.SaveCurrentSceneAsTemplateCommand.Execute(null);

            _dialog.Verify(d => d.ShowDialog(DialogType.SaveSceneTemplate, It.Is<SceneTemplateModel>(p => ReferenceEquals(p.Scene, scene))), Times.Once);
        }

        [Fact]
        public void SaveTemplateHandler_SavesThroughCacheAndReloadsGallery()
        {
            EditorLibraryViewModel sut = CreateSut();
            SceneModel scene = new() { Name = "My Scene" };
            _sceneChangedHandler(scene);
            SceneTemplateModel captured = null;
            _dialog.Setup(d => d.ShowDialog(DialogType.SaveSceneTemplate, It.IsAny<SceneTemplateModel>()))
                .Callback<DialogType, SceneTemplateModel>((_, prompt) => captured = prompt)
                .Returns(true);
            sut.SaveCurrentSceneAsTemplateCommand.Execute(null);
            BindingList<SceneTemplateModel> reloaded = new([new SceneTemplateModel { Name = "My Scene" }]);
            _cache.SetupGet(c => c.LoadedSceneTemplates).Returns(reloaded);

            captured.SaveTemplate.Invoke(captured);

            _cache.Verify(c => c.SaveSceneAsTemplate(scene, "My Scene"), Times.Once);
            Assert.Same(reloaded, sut.SceneTemplates);
        }

        [Fact]
        public void InsertTemplateHandler_PublishesAClonedScene()
        {
            SceneModel original = new() { Name = "Template Scene" };
            SceneTemplateModel template = new() { Name = "Template", Scene = original };
            _cache.SetupGet(c => c.LoadedSceneTemplates).Returns(new BindingList<SceneTemplateModel>([template]));
            CreateSut();

            template.InsertTemplateCommand.Execute(null);

            _pubSub.Verify(p => p.Publish(SubTopic.SceneTemplateInserted, It.Is<SceneModel>(s => s.Name == "Template Scene" && !ReferenceEquals(s, original))), Times.Once);
        }

        [Fact]
        public void DeleteTemplateHandler_DeletesThroughCache()
        {
            SceneTemplateModel template = new() { Name = "Template", IsBuiltIn = false };
            _cache.SetupGet(c => c.LoadedSceneTemplates).Returns(new BindingList<SceneTemplateModel>([template]));
            CreateSut();

            template.DeleteTemplateCommand.Execute(null);

            _cache.Verify(c => c.DeleteSceneTemplate(template), Times.Once);
        }

        [Fact]
        public void SelectedTextColorHex_ParsesToSelectedTextColor()
        {
            EditorLibraryViewModel sut = CreateSut();

            sut.SelectedTextColorHex = "#FFFF0000";

            Assert.Equal(Colors.Red, ((SolidColorBrush)sut.SelectedTextColor).Color);
        }

        [Fact]
        public void SelectedTextColorHex_InvalidHex_KeepsThePreviousColor()
        {
            EditorLibraryViewModel sut = CreateSut();
            sut.SelectedTextColorHex = "#FFFF0000";

            sut.SelectedTextColorHex = "not-a-color";

            Assert.Equal(Colors.Red, ((SolidColorBrush)sut.SelectedTextColor).Color);
        }

        [Fact]
        public void AddTextCommand_CanExecute_RequiresTextAndFontSelection()
        {
            EditorLibraryViewModel sut = CreateSut();
            Assert.False(sut.AddTextCommand.CanExecute(null));

            sut.RawText = "Hello";
            Assert.False(sut.AddTextCommand.CanExecute(null)); // still no font family/typeface

            sut.SelectedFontFamily = new FontFamily("Segoe UI");
            sut.SelectedTypeFace = sut.SelectedFontFamily.FamilyTypefaces.First();
            Assert.True(sut.AddTextCommand.CanExecute(null));
        }
    }
}
