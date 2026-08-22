namespace OpenBoardAnim.Library.Repositories
{
    public interface IGraphicRepository
    {
        List<GraphicEntity> GetAllGraphics(int lastId = 0);
        List<GraphicEntity> GetAllGraphics(string searchText, int lastId);
        Task AddNewGraphics(GraphicEntity[] entities);
        void AddNewGraphic(GraphicEntity entity);
        void DeleteGraphic(int graphicId);
        List<GraphicEntity> GetAllGraphicsUnpaged();
        void DeleteGraphics(IEnumerable<int> graphicIds);
    }
}
