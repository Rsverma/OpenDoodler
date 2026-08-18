using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenBoardAnim.ViewModels
{
    public class EditorCanvasViewModel : ViewModel
    {
        private const double MinZoom = 0.25;
        private const double MaxZoom = 4.0;
        private const double ZoomStep = 1.25;

        private IPubSubService _pubSub;

        public ICommand ZoomInCommand { get; set; }
        public ICommand ZoomOutCommand { get; set; }
        public ICommand ResetZoomCommand { get; set; }

        public EditorCanvasViewModel(IPubSubService pubSub)
        {
            _pubSub = pubSub;
            _pubSub.Subscribe(SubTopic.SceneChanged, SceneChangedHandler);
            _pubSub.Subscribe(SubTopic.GraphicAdded, GraphicAddedHandler);
            ZoomInCommand = new RelayCommand(o => ZoomLevel *= ZoomStep, o => ZoomLevel < MaxZoom - 0.001);
            ZoomOutCommand = new RelayCommand(o => ZoomLevel /= ZoomStep, o => ZoomLevel > MinZoom + 0.001);
            ResetZoomCommand = new RelayCommand(o => ZoomLevel = 1.0, o => Math.Abs(ZoomLevel - 1.0) > 0.001);
        }

        private double _zoomLevel = 1.0;
        public double ZoomLevel
        {
            get { return _zoomLevel; }
            set
            {
                double clamped = Math.Clamp(value, MinZoom, MaxZoom);
                if (_zoomLevel == clamped) return;
                _zoomLevel = clamped;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ZoomPercentageText));
            }
        }

        public string ZoomPercentageText => $"{_zoomLevel * 100:0}%";

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
            if (obj is GraphicModelBase model && SceneGraphics != null)
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
