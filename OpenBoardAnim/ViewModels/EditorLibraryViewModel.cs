using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using System.ComponentModel;

namespace OpenBoardAnim.ViewModels
{
    public class EditorLibraryViewModel : ViewModel
    {
        private IPubSubService _pubSub;
        private readonly CacheService _cache;

        public EditorLibraryViewModel(IPubSubService pubSub,CacheService cache)
        {
            _pubSub = pubSub;
            _cache = cache;
            Graphics = cache.LoadedGraphics;
            foreach (var graphic in Graphics)
            {
                graphic.AddGraphic = AddGraphicHandler;
            }
            Scenes = cache.LoadedScenes;
            foreach (var scene in Scenes)
            {
                scene.ReplaceScene = ReplaceSceneHandler;
            }
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

        private void ReplaceSceneHandler(SceneModel model)
        {
            _pubSub.Publish(SubTopic.SceneReplaced, model.Clone());
        }

        private BindingList<GraphicModel> _graphics;

        public BindingList<GraphicModel> Graphics
        {
            get { return _graphics; }
            set
            {
                _graphics = value;
                OnPropertyChanged();
            }
        }
        private void AddGraphicHandler(GraphicModel model)
        {
            _pubSub.Publish(SubTopic.GraphicAdded, model.Clone());
        }

    }
}
