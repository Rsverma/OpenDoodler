using OpenBoardAnim.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBoardAnim.Library.Repositories
{
    public class ProjectRepository
    {
        private readonly Func<DataContext> _contextFactory;
        public ProjectRepository(Func<DataContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public List<ProjectEntity> GetRecentProjects()
        {
            try
            {
                using var context = _contextFactory();
                return context.Projects.ToList();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
            return new List<ProjectEntity>();
        }

        public void SaveNewProject(ProjectEntity entity)
        {
            try
            {
                using var context = _contextFactory();
                context.Projects.Add(entity);
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                if(Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }
        public void UpdateExistingProject(ProjectEntity entity)
        {
            try
            {
                using var context = _contextFactory();
                context.Projects.Update(entity);
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }

        public void DeleteProject(int projectID)
        {
            try
            {
                using var context = _contextFactory();
                ProjectEntity project = context.Projects.Find(projectID);
                if (project != null)
                {
                    context.Projects.Remove(project);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }
    }
}
