using Microsoft.Win32;
using OpenBoardAnim.Library;
using OpenBoardAnim.Library.Repositories;
using OpenBoardAnim.Models;
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
        private List<GraphicEntity> _graphicEntities;
        private List<SceneEntity> _sceneEntities;
        public BindingList<RecentProjectModel> RecentProjects { get; set; }
        public ProjectDetails CurrentProject { get; set; }
        public BindingList<DrawingModel> LoadedGraphics { get; set; }
        public BindingList<SceneModel> LoadedScenes { get; set; }

        public CacheService(GraphicRepository gRepo, SceneRepository sRepo, ProjectRepository pRepo)
        {
            _gRepo = gRepo;
            _sRepo = sRepo;
            _pRepo = pRepo;
            LoadRecentProjects();
            LoadGraphics();
            LoadScenes();
        }
        public ProjectDetails LoadProjectFromFile(RecentProjectModel model)
        {
            string json = File.ReadAllText(model.FilePath);
            ProjectDetails project = JsonSerializer.Deserialize<ProjectDetails>(json);
            foreach (var s in project.Scenes)
            {
                foreach (var g in s.Graphics)
                {
                    if(g is DrawingModel d)
                        d.ImgDrawingGroup = GeometryHelper.GetPathGeometryFromSVG(d.SVGText);
                    else if(g is TextModel t)
                        t.TextGeometry = GeometryHelper.ConvertTextToGeometry(t.RawText, t.SelectedFontFamily,
                            t.SelectedFontStyle, t.SelectedFontWeight, t.SelectedFontSize);
                }
            }
            return project;
        }
        public void SaveNewProject(ProjectDetails project, string filePath)
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
        private void LoadScenes()
        {
            _sceneEntities = _sRepo.SceneEntities;
            List<SceneModel> scenes = _sceneEntities.Select(e =>
            new SceneModel
            {
                Name = e.Name
            }).ToList();
            LoadedScenes = new BindingList<SceneModel>(scenes);

        }
        private void LoadGraphics()
        {
            _graphicEntities = _gRepo.GetAllGraphics();
            List<DrawingModel> graphics = _graphicEntities.Select(e =>
            new DrawingModel
            {
                ID = e.GraphicID,
                Name = e.Name,
                SVGText = e.SVGText,
                ImgDrawingGroup = GeometryHelper.GetPathGeometryFromSVG(e.SVGText)
            }).ToList();
            LoadedGraphics = new BindingList<DrawingModel>(graphics);
        }
        public List<DrawingModel> GetGraphics(string searchText,int offsetID)
        {
            _graphicEntities = _gRepo.GetAllGraphics(searchText, offsetID);
            List<DrawingModel> graphics = _graphicEntities.Select(e =>
            new DrawingModel
            {
                ID = e.GraphicID,
                Name = e.Name,
                SVGText = e.SVGText,
                ImgDrawingGroup = GeometryHelper.GetPathGeometryFromSVG(e.SVGText)
            }).ToList();
            return graphics;
        }
        public async Task SaveNewGraphics(string[] paths)
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
        private void LoadRecentProjects()
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

        public void UpdateExistingProject(ProjectDetails project)
        {
            File.WriteAllText(project.Path, JsonSerializer.Serialize(project));
        }

        public void DeleteProject(RecentProjectModel model)
        {
            _pRepo.DeleteProject(model.ProjectID);
            _ = RecentProjects.Remove(model);
        }
    }
}
