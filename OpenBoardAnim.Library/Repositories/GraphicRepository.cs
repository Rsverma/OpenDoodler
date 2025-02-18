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
            var nextPage = _context.Graphics
                .OrderBy(b => b.GraphicID)
                .Where(b => b.GraphicID > lastId)
                .Take(20)
                .ToList();
            return nextPage;
        }

        public List<GraphicEntity> GetAllGraphics(string searchText, int lastId)
        {
            var nextPage = _context.Graphics
                .OrderBy(b => b.GraphicID)
                .Where(b => b.GraphicID > lastId && b.Name.Contains(searchText))
                .Take(20)
                .ToList();
            return nextPage;
        }
        public async Task AddNewGraphics(GraphicEntity[] entities)
        {
            await _context.Graphics.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }
        public void AddNewGraphic(GraphicEntity entity)
        {
            _context.Graphics.Add(entity);
            _context.SaveChanges();
        }
    }
}
