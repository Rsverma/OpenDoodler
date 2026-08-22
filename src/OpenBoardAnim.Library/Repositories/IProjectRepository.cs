namespace OpenBoardAnim.Library.Repositories
{
    public interface IProjectRepository
    {
        List<ProjectEntity> GetRecentProjects();
        void SaveNewProject(ProjectEntity entity);
        void UpdateExistingProject(ProjectEntity entity);
        void DeleteProject(int projectID);
    }
}
