using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using System.ComponentModel;
using System.Windows.Media;

namespace OpenBoardAnim.ViewModels
{
    public class EditorCanvasViewModel : ViewModel
    {
        private IPubSubService _pubSub;

        public EditorCanvasViewModel(IPubSubService pubSub)
        {
            _pubSub = pubSub;
            _pubSub.Subscribe(SubTopic.SceneChanged, SceneChangedHandler);
            _pubSub.Subscribe(SubTopic.GraphicAdded, GraphicAddedHandler);
        }

        private BindingList<GraphicModelBase> _sceneGraphics;

        public BindingList<GraphicModelBase> SceneGraphics
        {
            get { return _sceneGraphics; }
            set
            {
                _sceneGraphics = value;
                OnPropertyChanged();
            }
        }

        private void GraphicAddedHandler(object obj)
        {
            if (obj is GraphicModelBase model)
            {
                SceneGraphics.Add(model);
            }
        }

        private void SceneChangedHandler(object obj)
        {
            SceneModel model = (SceneModel)obj;
            if(model!=null)
            {
                SceneGraphics = model.Graphics;
            }

        }
    }
}
