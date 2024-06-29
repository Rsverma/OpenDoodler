using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using System.ComponentModel;

namespace OpenBoardAnim.ViewModels
{
    public class EditorTimelineViewModel : ViewModel
    {
        private readonly IPubSubService _pubSub;
        private SceneModel _addScene;
        public EditorTimelineViewModel(IPubSubService pubSub)
        {
            _pubSub = pubSub;
            _pubSub.Subscribe(SubTopic.SceneReplaced, SceneReplacedHandler);
        }

        private void SceneReplacedHandler(object obj)
        {
            int index = SelectedScene.Index;

            SceneModel scene = (SceneModel)obj;
            scene.Index = index;
            Scenes[index - 1] = scene;
            SelectedScene = scene;
        }

        private BindingList<SceneModel> _scenes;

        public BindingList<SceneModel> Scenes
        {
            get { return _scenes; }
            set
            {
                _scenes = value;
                _addScene = _scenes.LastOrDefault();
                SelectedScene = _scenes.FirstOrDefault();
                OnPropertyChanged();
            }
        }

        private SceneModel _selectedScene;

        public SceneModel SelectedScene
        {
            get { return _selectedScene; }
            set
            {
                if (value != _selectedScene)
                {
                    _selectedScene = value;
                    if (_selectedScene == _addScene)
                    {
                        AddNewScene();
                    }
                    OnPropertyChanged();
                    _pubSub.Publish(SubTopic.SceneChanged, _selectedScene);
                }
            }
        }

        private void AddNewScene()
        {
            int index = _scenes.Count;
            SceneModel newScene = new SceneModel
            {
                Name = index.ToString(),
                Index = index
            };
            _scenes.Insert(index - 1, newScene);
            ++_addScene.Index;
            _selectedScene = newScene;
        }
    }
}
