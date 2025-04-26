using OpenBoardAnim.Utilities;

namespace OpenBoardAnim.Library.Repositories
{
    public class GraphicRepository
    {
        private readonly DataContext _context;

        public GraphicRepository(DataContext context)
        {
            _context = context;
        }
        public List<GraphicEntity> GetAllGraphics(int lastId = 0)
        {
            List<GraphicEntity> nextPage = new List<GraphicEntity>();
            try
            {
                nextPage = [.. _context.Graphics
                        .OrderBy(b => b.GraphicID)
                        .Where(b => b.GraphicID > lastId)
                        .Take(20)];
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
            return nextPage;
        }

        public List<GraphicEntity> GetAllGraphics(string searchText, int lastId)
        {
            List<GraphicEntity> nextPage = new List<GraphicEntity>();
            try
            {
                nextPage = [.. _context.Graphics
                        .OrderBy(b => b.GraphicID)
                        .Where(b => b.GraphicID > lastId && b.Name.Contains(searchText))
                        .Take(20)];
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
            return nextPage;
        }
        public async Task AddNewGraphics(GraphicEntity[] entities)
        {
            try
            {
                await _context.Graphics.AddRangeAsync(entities);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }
        public void AddNewGraphic(GraphicEntity entity)
        {
            try
            {
                _context.Graphics.Add(entity);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }
    }
}
