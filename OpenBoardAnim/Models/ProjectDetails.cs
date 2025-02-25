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

        private ProjectSettings _settings;
        public ProjectSettings Settings
        {
            get { return _settings; }
            set
            {
                _settings = value;
                OnPropertyChanged();
            }
        }

        public List<SceneModel> Scenes { get; set; }
    }
    public enum BoardType
    {
        WhiteBoard,
        Blackboard,
        Greenboard
    }
    public class ProjectSettings :ObservableObject
    {
        private BoardType _boardType;
        public BoardType BoardType
        {
            get { return _boardType; }
            set
            {
                _boardType = value;
                OnPropertyChanged();
            }
        }

    }
}
