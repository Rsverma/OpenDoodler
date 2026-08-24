namespace OpenBoardAnim.Library.Repositories
{
    public interface IProjectRepository
    {
        List<ProjectEntity> GetRecentProjects();
        void SaveNewProject(ProjectEntity entity);
        void UpdateExistingProject(ProjectEntity entity);
        void UpdateProjectMetadata(string filePath, int sceneCount, DateTime latestLaunchTime);
        void DeleteProject(int projectID);
    }
}
