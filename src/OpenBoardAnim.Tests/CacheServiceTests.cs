using Moq;
using OpenBoardAnim.Library;
using OpenBoardAnim.Library.Repositories;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using System.IO;
using System.Text.Json;
using Xunit;

namespace OpenBoardAnim.Tests
{
    // CacheService's constructor eagerly evaluates BuiltInSceneTemplates.GetAll(AllShapes)
    // (a real static call, not routed through ISceneRepository) which reads
    // Resources\peep-*.svg from disk - see the test csproj's linked copies of those files.
    // Repositories here are mocked to return graphics/entities with empty SVGText so
    // GeometryHelper.GetPathGeometryFromSVG (SharpVectors) is never actually invoked - that
    // parsing path is WPF-rendering-coupled and out of scope for these unit tests, same as
    // the rest of GeometryHelper/PathAnimationHelper.
    public class CacheServiceTests
    {
        private readonly Mock<IGraphicRepository> _gRepo = new();
        private readonly Mock<ISceneRepository> _sRepo = new();
        private readonly Mock<IProjectRepository> _pRepo = new();
        private readonly Mock<IShapeRepository> _shRepo = new();

        public CacheServiceTests()
        {
            _gRepo.Setup(r => r.GetAllGraphics(It.IsAny<int>())).Returns([]);
            _sRepo.Setup(r => r.GetAllTemplates()).Returns([]);
            _pRepo.Setup(r => r.GetRecentProjects()).Returns([]);
            _shRepo.Setup(r => r.GetAllShapes()).Returns([]);
        }

        private CacheService CreateSut() => new(_gRepo.Object, _sRepo.Object, _pRepo.Object, _shRepo.Object);

        [Fact]
        public void Constructor_LoadsRecentProjects_MappedFromEntities()
        {
            _pRepo.Setup(r => r.GetRecentProjects()).Returns([
                new ProjectEntity { ProjectID = 1, Title = "My Project", FilePath = "C:\\p.obap", CreatedOn = new DateTime(2026, 1, 1), LatestLaunchTime = new DateTime(2026, 1, 2), SceneCount = 3 }
            ]);

            CacheService sut = CreateSut();

            RecentProjectModel model = Assert.Single(sut.RecentProjects);
            Assert.Equal(1, model.ProjectID);
            Assert.Equal("My Project", model.Title);
            Assert.Equal("C:\\p.obap", model.FilePath);
            Assert.Equal(3, model.Scenes);
        }

        [Fact]
        public void Constructor_SeedsThenLoadsBuiltInSceneTemplates()
        {
            _sRepo.Setup(r => r.GetAllTemplates()).Returns([
                new SceneTemplateEntity { SceneTemplateID = 1, Name = "Title Intro", IsBuiltIn = true, SceneJson = JsonSerializer.Serialize(new SceneModel { Name = "Title Intro" }) }
            ]);

            CacheService sut = CreateSut();

            _sRepo.Verify(r => r.SeedBuiltInTemplatesIfNeeded(It.IsAny<IEnumerable<(string Name, string SceneJson)>>()), Times.Once);
            SceneTemplateModel template = Assert.Single(sut.LoadedSceneTemplates);
            Assert.Equal("Title Intro", template.Name);
            Assert.True(template.IsBuiltIn);
        }

        [Fact]
        public void LoadProjectFromFile_DeserializesProjectFromDisk()
        {
            string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.obap");
            ProjectDetails original = new() { Title = "From Disk", Scenes = [] };
            File.WriteAllText(path, JsonSerializer.Serialize(original));
            try
            {
                CacheService sut = CreateSut();

                ProjectDetails loaded = sut.LoadProjectFromFile(path);

                Assert.Equal("From Disk", loaded.Title);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void LoadProjectFromFile_ByRecentProjectModel_DelegatesToFilePath()
        {
            string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.obap");
            File.WriteAllText(path, JsonSerializer.Serialize(new ProjectDetails { Title = "Via Model", Scenes = [] }));
            try
            {
                CacheService sut = CreateSut();

                ProjectDetails loaded = sut.LoadProjectFromFile(new RecentProjectModel { FilePath = path });

                Assert.Equal("Via Model", loaded.Title);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void SaveNewProject_WritesFile_PersistsEntity_AndPrependsRecentProjects()
        {
            string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.obap");
            try
            {
                CacheService sut = CreateSut();
                ProjectDetails project = new() { Scenes = [] };

                sut.SaveNewProject(project, path);

                Assert.True(File.Exists(path));
                Assert.Equal(path, project.Path);
                _pRepo.Verify(r => r.SaveNewProject(It.Is<ProjectEntity>(e => e.FilePath == path)), Times.Once);
                Assert.Equal(path, sut.RecentProjects[0].FilePath);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void UpdateExistingProject_OverwritesFileAtProjectPath()
        {
            string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.obap");
            File.WriteAllText(path, "stale content");
            try
            {
                CacheService sut = CreateSut();
                ProjectDetails project = new() { Path = path, Title = "Updated", Scenes = [] };

                sut.UpdateExistingProject(project);

                string written = File.ReadAllText(path);
                Assert.Contains("Updated", written);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void SaveSceneAsTemplate_AddsTemplateAndReloadsGallery()
        {
            _sRepo.SetupSequence(r => r.GetAllTemplates())
                .Returns([]) // initial load in the constructor
                .Returns([new SceneTemplateEntity { SceneTemplateID = 5, Name = "My Scene", SceneJson = JsonSerializer.Serialize(new SceneModel()) }]); // reload after save
            CacheService sut = CreateSut();

            sut.SaveSceneAsTemplate(new SceneModel { Name = "My Scene" }, "My Scene");

            _sRepo.Verify(r => r.AddTemplate(It.Is<SceneTemplateEntity>(e => e.Name == "My Scene" && !e.IsBuiltIn)), Times.Once);
            Assert.Single(sut.LoadedSceneTemplates);
        }

        [Fact]
        public void SaveSceneAsTemplate_NoOp_WhenNameIsBlank()
        {
            CacheService sut = CreateSut();

            sut.SaveSceneAsTemplate(new SceneModel(), "   ");

            _sRepo.Verify(r => r.AddTemplate(It.IsAny<SceneTemplateEntity>()), Times.Never);
        }

        [Fact]
        public void DeleteSceneTemplate_RemovesUserTemplate()
        {
            _sRepo.Setup(r => r.GetAllTemplates()).Returns([
                new SceneTemplateEntity { SceneTemplateID = 7, Name = "Custom", IsBuiltIn = false, SceneJson = JsonSerializer.Serialize(new SceneModel()) }
            ]);
            CacheService sut = CreateSut();
            SceneTemplateModel template = sut.LoadedSceneTemplates.Single();

            sut.DeleteSceneTemplate(template);

            _sRepo.Verify(r => r.DeleteTemplate(7), Times.Once);
            Assert.Empty(sut.LoadedSceneTemplates);
        }

        [Fact]
        public void DeleteSceneTemplate_NoOp_ForBuiltInTemplate()
        {
            _sRepo.Setup(r => r.GetAllTemplates()).Returns([
                new SceneTemplateEntity { SceneTemplateID = 9, Name = "Built-in", IsBuiltIn = true, SceneJson = JsonSerializer.Serialize(new SceneModel()) }
            ]);
            CacheService sut = CreateSut();
            SceneTemplateModel template = sut.LoadedSceneTemplates.Single();

            sut.DeleteSceneTemplate(template);

            _sRepo.Verify(r => r.DeleteTemplate(It.IsAny<int>()), Times.Never);
            Assert.Single(sut.LoadedSceneTemplates);
        }

        [Fact]
        public void GetGraphics_MapsRepositoryPageToDrawingModels()
        {
            _gRepo.Setup(r => r.GetAllGraphics("cat", 10)).Returns([
                new GraphicEntity { GraphicID = 11, Name = "Cat", SVGText = "" }
            ]);
            CacheService sut = CreateSut();

            List<DrawingModel> results = sut.GetGraphics("cat", 10);

            DrawingModel model = Assert.Single(results);
            Assert.Equal(11, model.ID);
            Assert.Equal("Cat", model.Name);
        }

        [Fact]
        public void CleanupInvalidGraphics_RemovesEntriesWithNoUsableGeometry()
        {
            _gRepo.Setup(r => r.GetAllGraphics(It.IsAny<int>())).Returns([
                new GraphicEntity { GraphicID = 1, Name = "Blank", SVGText = "" }
            ]);
            _gRepo.Setup(r => r.GetAllGraphicsUnpaged()).Returns([
                new GraphicEntity { GraphicID = 1, Name = "Blank", SVGText = "" }
            ]);
            CacheService sut = CreateSut();

            int removed = sut.CleanupInvalidGraphics();

            Assert.Equal(1, removed);
            _gRepo.Verify(r => r.DeleteGraphics(It.Is<IEnumerable<int>>(ids => ids.Single() == 1)), Times.Once);
            Assert.Empty(sut.LoadedGraphics);
        }

        [Fact]
        public void CleanupInvalidGraphics_NoOp_WhenCatalogIsEmpty()
        {
            _gRepo.Setup(r => r.GetAllGraphicsUnpaged()).Returns([]);
            CacheService sut = CreateSut();

            int removed = sut.CleanupInvalidGraphics();

            Assert.Equal(0, removed);
            _gRepo.Verify(r => r.DeleteGraphics(It.IsAny<IEnumerable<int>>()), Times.Never);
        }

        [Fact]
        public void DeleteGraphic_RemovesFromRepositoryAndLoadedList()
        {
            _gRepo.Setup(r => r.GetAllGraphics(It.IsAny<int>())).Returns([
                new GraphicEntity { GraphicID = 3, Name = "ToDelete", SVGText = "" }
            ]);
            CacheService sut = CreateSut();
            DrawingModel model = sut.LoadedGraphics.Single();

            sut.DeleteGraphic(model);

            _gRepo.Verify(r => r.DeleteGraphic(3), Times.Once);
            Assert.Empty(sut.LoadedGraphics);
        }

        [Fact]
        public async Task SaveNewGraphics_ReadsFilesAndAddsThemViaRepository()
        {
            string path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.svg");
            File.WriteAllText(path, "");
            try
            {
                CacheService sut = CreateSut();

                await sut.SaveNewGraphics([path]);

                _gRepo.Verify(r => r.AddNewGraphics(It.Is<GraphicEntity[]>(arr => arr.Length == 1 && arr[0].Name == Path.GetFileNameWithoutExtension(path))), Times.Once);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [Fact]
        public void DeleteProject_RemovesFromRepositoryAndRecentProjects()
        {
            _pRepo.Setup(r => r.GetRecentProjects()).Returns([
                new ProjectEntity { ProjectID = 4, Title = "Gone", FilePath = "C:\\gone.obap", CreatedOn = DateTime.Now, LatestLaunchTime = DateTime.Now, SceneCount = 1 }
            ]);
            CacheService sut = CreateSut();
            RecentProjectModel model = sut.RecentProjects.Single();

            sut.DeleteProject(model);

            _pRepo.Verify(r => r.DeleteProject(4), Times.Once);
            Assert.Empty(sut.RecentProjects);
        }
    }
}
