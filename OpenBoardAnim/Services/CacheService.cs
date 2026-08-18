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
        public BindingList<RecentProjectModel> RecentProjects { get; set; }
        public ProjectDetails CurrentProject { get; set; }
        public BindingList<DrawingModel> LoadedGraphics { get; set; }
        public BindingList<DrawingModel> AllShapes { get; set; }
        public BindingList<SceneTemplateModel> LoadedSceneTemplates { get; set; }

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
                LoadShapes();
                LoadSceneTemplates();
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
                List<GraphicEntity> shapeEntities = _shRepo.GetAllShapes();
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
            return LoadProjectFromFile(model.FilePath);
        }

        public ProjectDetails LoadProjectFromFile(string filePath)
        {
            ProjectDetails project = null;
            try
            {
                string json = File.ReadAllText(filePath);
                project = JsonSerializer.Deserialize<ProjectDetails>(json);
                foreach (var s in project.Scenes)
                {
                    foreach (var g in s.Graphics)
                    {
                        if (g is DrawingModel d)
                            d.ImgDrawingGroup = GeometryHelper.GetPathGeometryFromSVG(d.SVGText);
                        else if (g is TextModel t)
                            t.TextGeometry = GeometryHelper.ConvertTextToGeometry(t.RawText, t.SelectedFontFamily,
                                t.SelectedFontStyle, t.SelectedFontWeight, t.SelectedFontSize, t.IsUnderline);
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
        private void LoadSceneTemplates()
        {
            try
            {
                _sRepo.SeedBuiltInTemplatesIfNeeded(BuiltInSceneTemplates.GetAll(AllShapes));
                List<SceneTemplateEntity> entities = _sRepo.GetAllTemplates();
                List<SceneTemplateModel> templates = entities.Select(GetModelFromSceneTemplateEntity).Where(x => x != null).ToList();
                LoadedSceneTemplates = new BindingList<SceneTemplateModel>(templates);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }

        private static SceneTemplateModel GetModelFromSceneTemplateEntity(SceneTemplateEntity e)
        {
            try
            {
                SceneModel scene = JsonSerializer.Deserialize<SceneModel>(e.SceneJson);
                foreach (GraphicModelBase g in scene.Graphics)
                {
                    if (g is DrawingModel d)
                        d.ImgDrawingGroup = GeometryHelper.GetPathGeometryFromSVG(d.SVGText);
                    else if (g is TextModel t)
                        t.TextGeometry = GeometryHelper.ConvertTextToGeometry(t.RawText, t.SelectedFontFamily,
                            t.SelectedFontStyle, t.SelectedFontWeight, t.SelectedFontSize, t.IsUnderline);
                }
                return new SceneTemplateModel
                {
                    Id = e.SceneTemplateID,
                    Name = e.Name,
                    IsBuiltIn = e.IsBuiltIn,
                    Scene = scene
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, LogAction.LogOnly);
                return null;
            }
        }

        public void SaveSceneAsTemplate(SceneModel scene, string name)
        {
            try
            {
                if (scene == null || string.IsNullOrWhiteSpace(name)) return;
                SceneModel clone = scene.Clone();
                clone.Name = name;
                clone.Index = 0;
                string json = JsonSerializer.Serialize(clone);
                _sRepo.AddTemplate(new SceneTemplateEntity { Name = name, SceneJson = json, IsBuiltIn = false });
                LoadSceneTemplates();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }

        public void DeleteSceneTemplate(SceneTemplateModel template)
        {
            try
            {
                if (template == null || template.IsBuiltIn) return;
                _sRepo.DeleteTemplate(template.Id);
                LoadedSceneTemplates.Remove(template);
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
                List<DrawingModel> graphics = _graphicEntities.Select(GetModelFromGraphicEntity).Where(x=>x!=null).ToList();
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
                if (Logger.LogError(ex, LogAction.LogAndShow))
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
                        GraphicEntity graphicEntity = new()
                        {
                            Name = Path.GetFileNameWithoutExtension(file),
                            SVGText = svgText
                        };
                        return GetModelFromGraphicEntity(graphicEntity) == null ? null : graphicEntity;
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

        // A single rolling autosave slot (sibling to the OpenBoardAnim.db, same
        // %LocalAppData% convention as DataContext) - distinct from the in-memory undo/redo
        // snapshot, which is lost on crash or close-without-saving. Never registered as a
        // recent project or touched via project.Path/Title - a pure background side-channel.
        private static readonly string BackupFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpenBoardAnim.autosave.obap");

        public void SaveBackup(ProjectDetails project)
        {
            try
            {
                if (project == null) return;
                File.WriteAllText(BackupFilePath, JsonSerializer.Serialize(project));
            }
            catch (Exception ex)
            {
                // Best-effort - a failed background backup shouldn't interrupt editing.
                Logger.LogWarning($"Failed to write autosave backup: {ex.Message}");
            }
        }

        public bool BackupExists() => File.Exists(BackupFilePath);

        public ProjectDetails LoadBackup() => LoadProjectFromFile(BackupFilePath);

        public void ClearBackup()
        {
            try
            {
                if (File.Exists(BackupFilePath))
                    File.Delete(BackupFilePath);
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to delete autosave backup: {ex.Message}");
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
