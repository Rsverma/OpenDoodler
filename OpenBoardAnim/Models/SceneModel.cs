using OpenBoardAnim.Core;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenBoardAnim.Models
{
    public class SceneModel
    {
        public SceneModel()
        {

            ReplaceSceneCommand = new RelayCommand(ReplaceSceneCommandHandler,
                canExecute: o => true);
        }
        public Action<SceneModel> ReplaceScene;
        private void ReplaceSceneCommandHandler(object obj)
        {
            ReplaceScene?.Invoke(this);
        }

        public string Name { get; set; }
        public string ImgPath { get; set; }
        public Geometry ImgGeometry { get; set; }
        public string Shape { get; set; }
        public ICommand ReplaceSceneCommand { get; set; }

    }
}
