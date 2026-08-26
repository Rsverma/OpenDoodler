using OpenBoardAnim.Utilities;

namespace OpenBoardAnim.Library.Repositories
{
    public class GraphicRepository : IGraphicRepository
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
                // A blank/null searchText should mean "show everything" - Name.Contains(searchText)
                // applied unconditionally instead translates to a SQL LIKE against a null/empty
                // pattern, which matches nothing rather than throwing, so it silently returned
                // zero rows for an empty search.
                IQueryable<GraphicEntity> query = context.Graphics.Where(b => b.GraphicID > lastId);
                if (!string.IsNullOrEmpty(searchText))
                    query = query.Where(b => b.Name.Contains(searchText));
                nextPage = [.. query.OrderBy(b => b.GraphicID).Take(20)];
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

        public void DeleteGraphic(int graphicId)
        {
            try
            {
                using var context = _contextFactory();
                GraphicEntity entity = context.Graphics.Find(graphicId);
                if (entity != null)
                {
                    context.Graphics.Remove(entity);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
        }

        // Unlike GetAllGraphics, not paged - needed so a cleanup sweep (see
        // CacheService.CleanupInvalidGraphics) can check every stored graphic, not just
        // whatever page happens to be currently loaded/searched in the UI.
        public List<GraphicEntity> GetAllGraphicsUnpaged()
        {
            List<GraphicEntity> all = new List<GraphicEntity>();
            try
            {
                using var context = _contextFactory();
                all = [.. context.Graphics.OrderBy(b => b.GraphicID)];
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
            return all;
        }

        public void DeleteGraphics(IEnumerable<int> graphicIds)
        {
            try
            {
                using var context = _contextFactory();
                List<GraphicEntity> entities = [.. context.Graphics.Where(g => graphicIds.Contains(g.GraphicID))];
                if (entities.Count > 0)
                {
                    context.Graphics.RemoveRange(entities);
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
