using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBoardAnim.Library.Repositories
{
    public class SceneRepository
    {
        public SceneRepository() 
        {
            SceneEntities = new List<SceneEntity>()
            {
                new SceneEntity()
                {
                    Name = "peep-102",
                    FilePath = "Resources\\peep-102.svg"

                },
                new SceneEntity()
                {
                    Name = "peep-64",
                    FilePath = "Resources\\peep-64.svg"

                },
                new SceneEntity()
                {
                    Name = "peep-43",
                    FilePath = "Resources\\peep-43.svg"

                },
                new SceneEntity()
                {
                    Name = "peep-61",
                    FilePath = "Resources\\peep-61.svg"

                }
            };

        }
        public List<SceneEntity> SceneEntities { get; set; }

    }
}
