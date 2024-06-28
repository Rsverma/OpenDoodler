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
            return _context.Projects.ToList();
        }

        public void SaveNewProject(ProjectEntity entity)
        {
            _context.Projects.Add(entity);
            _context.SaveChanges();
        }
        public void UpdateExistingProject(ProjectEntity entity)
        {
            _context.Projects.Add(entity);
            _context.SaveChanges();
        }
    }
}
