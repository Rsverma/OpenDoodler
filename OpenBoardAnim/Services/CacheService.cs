using Microsoft.Win32;
using OpenBoardAnim.Library;
using OpenBoardAnim.Library.Repositories;
using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using OpenBoardAnim.Utils;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Media;

namespace OpenBoardAnim.Services
{
    public class CacheService
    {
        private readonly GraphicRepository _gRepo;
        private readonly SceneRepository _sRepo;
        private readonly ProjectRepository _pRepo;
        private readonly ShapeRepository _shRepo;
        private List<GraphicEntity> _graphicEntities;
        private List<SceneEntity> _sceneEntities;
        public BindingList<RecentProjectModel> RecentProjects { get; set; }
        public ProjectDetails CurrentProject { get; set; }
        public BindingList<DrawingModel> LoadedGraphics { get; set; }
        public BindingList<DrawingModel> AllShapes { get; set; }
        public BindingList<SceneModel> LoadedScenes { get; set; }

        public CacheService(GraphicRepository gRepo, SceneRepository sRepo, ProjectRepository pRepo, ShapeRepository shRepo)
        {
            try
            {
                _gRepo = gRepo;
                _sRepo = sRepo;
                _pRepo = pRepo;
                _shRepo = shRepo;
                LoadRecentProjects();
                LoadGraphics();
                LoadScenes();
                LoadShapes();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void LoadShapes()
        {
            try
            {
                List<GraphicEntity> shapeEntities = _shRepo.GetAllShapess();
                List<DrawingModel> drawingModels = shapeEntities.Select(GetModelFromGraphicEntity).ToList();
                AllShapes = new BindingList<DrawingModel>(drawingModels);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }

        public ProjectDetails LoadProjectFromFile(RecentProjectModel model)
        {
            ProjectDetails project = null;
            try
            {
                string json = File.ReadAllText(model.FilePath);
                project = JsonSerializer.Deserialize<ProjectDetails>(json);
                foreach (var s in project.Scenes)
                {
                    foreach (var g in s.Graphics)
                    {
                        if (g is DrawingModel d)
                            d.ImgDrawingGroup = GeometryHelper.GetPathGeometryFromSVG(d.SVGText);
                        else if (g is TextModel t)
                            t.TextGeometry = GeometryHelper.ConvertTextToGeometry(t.RawText, t.SelectedFontFamily,
                                t.SelectedFontStyle, t.SelectedFontWeight, t.SelectedFontSize);
                    }
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
            return project;
        }
        public void SaveNewProject(ProjectDetails project, string filePath)
        {
            try
            {
                project.Path = filePath;
                project.Title = Path.GetFileNameWithoutExtension(project.Path);
                File.WriteAllText(filePath, JsonSerializer.Serialize(project));
                _pRepo.SaveNewProject(new ProjectEntity
                {
                    Title = project.Title,
                    CreatedOn = project.CreatedOn,
                    FilePath = project.Path,
                    LatestLaunchTime = DateTime.Now,
                    SceneCount = project.Scenes.Count,
                });
                RecentProjects.Insert(0, new RecentProjectModel
                {
                    Title = project.Title,
                    Scenes = project.Scenes.Count,
                    CreatedOn = project.CreatedOn,
                    FilePath = project.Path,
                    LatestLaunchTime = DateTime.Now,
                });
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }
        private void LoadScenes()
        {
            try
            {
                _sceneEntities = _sRepo.SceneEntities;
                List<SceneModel> scenes = _sceneEntities.Select(e =>
                new SceneModel
                {
                    Name = e.Name
                }).ToList();
                LoadedScenes = new BindingList<SceneModel>(scenes);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }

        }
        private void LoadGraphics()
        {
            try
            {
                _graphicEntities = _gRepo.GetAllGraphics();
                List<DrawingModel> graphics = _graphicEntities.Select(GetModelFromGraphicEntity).ToList();
                LoadedGraphics = new BindingList<DrawingModel>(graphics);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }

        private static DrawingModel GetModelFromGraphicEntity(GraphicEntity e)
        {
            try
            {

                return new DrawingModel
                {
                    ID = e.GraphicID,
                    Name = e.Name,
                    SVGText = e.SVGText,
                    ImgDrawingGroup = GeometryHelper.GetPathGeometryFromSVG(e.SVGText)
                };
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
                return null;
            }

        }

        public List<DrawingModel> GetGraphics(string searchText,int offsetID)
        {
            List<DrawingModel> graphics = null;
            try
            {

                _graphicEntities = _gRepo.GetAllGraphics(searchText, offsetID);
                graphics = [.. _graphicEntities.Select(GetModelFromGraphicEntity)];

            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
            return graphics;
        }
        public async Task SaveNewGraphics(string[] paths)
        {
            try
            {
                await _gRepo.AddNewGraphics(paths.Select(file =>
                    {
                        string svgText = File.ReadAllText(file);
                        return new GraphicEntity
                        {
                            Name = Path.GetFileNameWithoutExtension(file),
                            SVGText = svgText
                        };
                    }).ToArray());

                LoadGraphics();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }
        private void LoadRecentProjects()
        {
            try
            {
                List<ProjectEntity> projects = _pRepo.GetRecentProjects();
                var models = projects.Select(x => new RecentProjectModel
                {
                    ProjectID = x.ProjectID,
                    CreatedOn = x.CreatedOn,
                    LatestLaunchTime = x.LatestLaunchTime,
                    Scenes = x.SceneCount,
                    Title = x.Title,
                    FilePath = x.FilePath
                }).ToList();
                RecentProjects = new BindingList<RecentProjectModel>(models);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }

        public void UpdateExistingProject(ProjectDetails project)
        {
            try
            {
                File.WriteAllText(project.Path, JsonSerializer.Serialize(project));
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }

        public void DeleteProject(RecentProjectModel model)
        {
            try
            {
                _pRepo.DeleteProject(model.ProjectID);
                _ = RecentProjects.Remove(model);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }
    }
}
