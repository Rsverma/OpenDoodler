namespace OpenBoardAnim.Library.Repositories
{
    public class GraphicRepository
    {
        private readonly DataContext _context;

        public GraphicRepository(DataContext context)
        {
            _context = context;
        }
        public List<GraphicEntity> GetAllGraphics()
        {
            return _context.Graphics.ToList();
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
