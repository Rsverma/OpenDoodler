using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
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
            CustPath = PathGeometry.CreateFromGeometry(
            Geometry.Parse("m21.241 7.1495c-4.0125 1e-7 -7.5565 1.5303-10.631 4.5861-5.8865 5.851-6.1348 15.13-0.7502 21.273-0.139-0.056 22.098 22.046 22.154 21.991-0.056 0.055 22.182-22.047 22.126-21.991 5.385-6.143 5.136-15.422-0.75-21.273-5.882-5.8464-15.21-6.38-21.39-1.042-2.952-2.5567-7.088-3.5445-10.759-3.5445z"));
        }

        private PathGeometry _custPath;

        public PathGeometry CustPath
        {
            get { return _custPath; }
            set
            {
                _custPath = value;
                OnPropertyChanged();
            }
        }
        private void GraphicAddedHandler(object obj)
        {
            throw new NotImplementedException();
        }

        private void SceneReplacedHandler(object obj)
        {
            PathGeometry model = (PathGeometry)obj;
            if(model!=null)
                CustPath = model;

        }
    }
}
