using OpenBoardAnim.Core;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenBoardAnim.Models
{
    public class SceneModel
    {
        public SceneModel()
        {

            AddGraphicCommand = new RelayCommand(AddGraphicCommandHandler,
                canExecute: o => true);
        }
        public Action<SceneModel> AddScene;
        private void AddGraphicCommandHandler(object obj)
        {
            if (AddScene != null) { AddScene(this); }
        }

        public string Name { get; set; }
        public string ImgPath { get; set; }
        public  PathGeometry Shape {  get; set; }
        public ICommand AddGraphicCommand { get; set; }

    }
}
