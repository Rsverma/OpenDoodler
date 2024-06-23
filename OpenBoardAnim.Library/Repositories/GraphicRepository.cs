namespace OpenBoardAnim.Library.Repositories
{
    public class GraphicRepository
    {
        public GraphicRepository()
        {
            GraphicEntities = new List<GraphicEntity>()
            {
                new GraphicEntity()
                {
                    Name = "peep-102",
                    FilePath = "Resources\\peep-102.svg"

                },
                new GraphicEntity()
                {
                    Name = "peep-64",
                    FilePath = "Resources\\peep-64.svg"

                },
                new GraphicEntity()
                {
                    Name = "peep-43",
                    FilePath = "Resources\\peep-43.svg"

                },
                new GraphicEntity()
                {
                    Name = "peep-61",
                    FilePath = "Resources\\peep-61.svg"

                }
            };

        }
        public List<GraphicEntity> GraphicEntities { get; set; }

    }
}
