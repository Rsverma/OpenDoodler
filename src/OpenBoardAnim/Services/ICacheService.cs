using OpenBoardAnim.Models;
using System.ComponentModel;

namespace OpenBoardAnim.Services
{
    public interface ICacheService
    {
        BindingList<RecentProjectModel> RecentProjects { get; set; }
        ProjectDetails CurrentProject { get; set; }
        BindingList<DrawingModel> LoadedGraphics { get; set; }
        BindingList<DrawingModel> AllShapes { get; set; }
        BindingList<SceneTemplateModel> LoadedSceneTemplates { get; set; }

        ProjectDetails LoadProjectFromFile(RecentProjectModel model);
        ProjectDetails LoadProjectFromFile(string filePath);
        void SaveNewProject(ProjectDetails project, string filePath);
        void SaveSceneAsTemplate(SceneModel scene, string name);
        void DeleteSceneTemplate(SceneTemplateModel template);
        List<DrawingModel> GetGraphics(string searchText, int offsetID);
        int CleanupInvalidGraphics();
        void DeleteGraphic(DrawingModel model);
        Task SaveNewGraphics(string[] paths);
        void UpdateExistingProject(ProjectDetails project);
        void SaveBackup(ProjectDetails project);
        bool BackupExists();
        ProjectDetails LoadBackup();
        void ClearBackup();
        void DeleteProject(RecentProjectModel model);
    }
}
