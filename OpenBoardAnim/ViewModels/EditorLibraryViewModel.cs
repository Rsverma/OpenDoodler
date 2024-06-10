using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using System.ComponentModel;

namespace OpenBoardAnim.ViewModels
{
    public class EditorLibraryViewModel : ViewModel
    {
        private IPubSubService _pubSub;

        public EditorLibraryViewModel(IPubSubService pubSub)
        {
            _pubSub = pubSub;
            Text1 = "library";
            SceneModel value = new SceneModel() { Name = "img1", ImgPath = "/Resources/pencil.png" };
            value.AddScene += AddSceneHandler;
            Scenes = [value];
        }

        private void AddSceneHandler(SceneModel model)
        {
            _pubSub.Publish(SubTopic.SceneReplaced, model.Shape);
        }

        private BindingList<SceneModel> _scenes;

        public BindingList<SceneModel> Scenes
        {
            get { return _scenes; }
            set
            {
                _scenes = value;
                OnPropertyChanged();
            }
        }

        private string _text;

        public string Text1
        {
            get { return _text; }
            set
            {
                _text = value;
                OnPropertyChanged();
            }
        }
    }
}
