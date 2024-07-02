using OpenBoardAnim.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace OpenBoardAnim.Models
{
    public class ProjectDetails : ObservableObject
    {
        public ProjectDetails()
        {
            Scenes = new List<SceneModel> { new SceneModel
                {
                    Name="1",
                    Index = 1
                } ,new SceneModel
                {
                    Name="+",
                    Index = 2
                }   
            };
        }

        public string Title { get; set; } = "Untitled Project";
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public string Path { get; set; }
        public List<SceneModel> Scenes { get; set; }
    }
}
