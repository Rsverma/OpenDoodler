using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace OpenBoardAnim.ViewModels
{
    public class EditorTimelineViewModel : ViewModel
    {
        private readonly IPubSubService _pubSub;
        SceneModel _addScene = new SceneModel{Name = "+",Index = 3};
        public EditorTimelineViewModel(IPubSubService pubSub)
        {
            _pubSub = pubSub;
            _pubSub.Subscribe(SubTopic.SceneReplaced, SceneReplacedHandler);
            Scenes = new BindingList<SceneModel> { new SceneModel
            {
                Name="1",
                Index = 1
            } ,new SceneModel
            {
                Name="2",
                Index = 2
            } ,_addScene
            };
            SelectedScene = Scenes.FirstOrDefault();
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
            SceneModel newScene = new SceneModel
            {
                Name = _addScene.Index.ToString(),
                Index = _addScene.Index
            };
            _scenes.Insert(_addScene.Index - 1, newScene);
            ++_addScene.Index;
            _selectedScene = newScene;
        }
    }
}
