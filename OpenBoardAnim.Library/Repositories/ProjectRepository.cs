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
        private readonly DataContext _context;
        public ProjectRepository(DataContext context)
        {
            _context = context;
        }

        public List<ProjectEntity> GetRecentProjects()
        {
            try
            {
                return _context.Projects.ToList();
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
                _context.Projects.Add(entity);
                _context.SaveChanges();
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
                _context.Projects.Add(entity);
                _context.SaveChanges();
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
                ProjectEntity project = _context.Projects.Find(projectID);
                if (project != null)
                {
                    _context.Projects.Remove(project);
                    _context.SaveChanges();
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
