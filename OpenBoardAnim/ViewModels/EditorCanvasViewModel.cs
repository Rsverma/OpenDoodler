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
            _pubSub.Subscribe(SubTopic.SceneReplaced, SceneReplacedHandler);
            _pubSub.Subscribe(SubTopic.GraphicAdded, GraphicAddedHandler);
            SceneGraphics = [];
        }

        private BindingList<GraphicModel> _sceneGraphics;

        public BindingList<GraphicModel> SceneGraphics
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
            GraphicModel model = (GraphicModel)obj;
            if(model != null )
            {
                SceneGraphics.Add(model);
            }
        }

        private void SceneReplacedHandler(object obj)
        {
            Geometry model = (Geometry)obj;
            if(model!=null)
                { }

        }
    }
}
