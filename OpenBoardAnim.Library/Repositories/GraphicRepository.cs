using OpenBoardAnim.Utilities;

namespace OpenBoardAnim.Library.Repositories
{
    public class GraphicRepository
    {
        private readonly Func<DataContext> _contextFactory;

        public GraphicRepository(Func<DataContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }
        public List<GraphicEntity> GetAllGraphics(int lastId = 0)
        {
            List<GraphicEntity> nextPage = new List<GraphicEntity>();
            try
            {
                using var context = _contextFactory();
                nextPage = [.. context.Graphics
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
                using var context = _contextFactory();
                nextPage = [.. context.Graphics
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
                using var context = _contextFactory();
                await context.Graphics.AddRangeAsync(entities.Where(e => e != null));
                await context.SaveChangesAsync();
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
                using var context = _contextFactory();
                context.Graphics.Add(entity);
                context.SaveChanges();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }
    }
}
