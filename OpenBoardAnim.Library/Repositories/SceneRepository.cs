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

                },
                new SceneEntity()
                {
                    Name = "peep-64",

                },
                new SceneEntity()
                {
                    Name = "peep-43",

                },
                new SceneEntity()
                {
                    Name = "peep-61",

                }
            };

        }
        public List<SceneEntity> SceneEntities { get; set; }

    }
}
