using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using OpenBoardAnim.Utilities;
using System.ComponentModel;
using System.Windows.Input;
using System.Xml.Linq;

namespace OpenBoardAnim.ViewModels
{
    public class EditorTimelineViewModel : ViewModel
    {
        private readonly IPubSubService _pubSub;
        private SceneModel _addScene;
        public ICommand SceneLeftCommand { get; set; }
        public ICommand SceneRightCommand { get; set; }
        public ICommand SceneDeleteCommand { get; set; }
        public EditorTimelineViewModel(IPubSubService pubSub)
        {
            _pubSub = pubSub;
            _pubSub.Subscribe(SubTopic.SceneReplaced, SceneReplacedHandler);
            SceneLeftCommand = new RelayCommand(SceneLeftCommandHandler, o => true);
            SceneRightCommand = new RelayCommand(SceneRightCommandHandler, o => true);
            SceneDeleteCommand = new RelayCommand(SceneDeleteCommandHandler, o => true);
        }

        private void SceneDeleteCommandHandler(object obj)
        {
            try
            {
                try
                {
                    if (SelectedScene == null)
                        return;
                }
                catch (Exception ex) { if (Logger.LogError(ex, LogAction.LogAndShow)) throw; }
                int index = SelectedScene.Index;
                if (index == 1) SelectedScene = Scenes[index];
                else SelectedScene = Scenes[index - 2];
                Scenes.RemoveAt(index - 1);
                for (int i = 0; i < Scenes.Count; i++)
                {
                    SceneModel scene = Scenes[i];
                    scene.Name = i.ToString();
                    scene.Index = i;
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void SceneRightCommandHandler(object obj)
        {
            try
            {
                try
                {
                    if (SelectedScene == null)
                        return;
                }
                catch (Exception ex) { if (Logger.LogError(ex, LogAction.LogAndShow)) throw; }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void SceneLeftCommandHandler(object obj)
        {
            try
            {
                try
                {
                    if (SelectedScene == null)
                        return;
                }
                catch (Exception ex) { if (Logger.LogError(ex, LogAction.LogAndShow)) throw; }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void SceneReplacedHandler(object obj)
        {
            try
            {
                int index = SelectedScene.Index;

                SceneModel scene = (SceneModel)obj;
                scene.Index = index;
                Scenes[index - 1] = scene;
                SelectedScene = scene;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private BindingList<SceneModel> _scenes;

        public BindingList<SceneModel> Scenes
        {
            get { return _scenes; }
            set
            {
                _scenes = value;
                UpdateBindings(value);
                OnPropertyChanged();
            }
        }

        private void UpdateBindings(BindingList<SceneModel> value)
        {
            try
            {
                foreach (var item in value)
                {
                    item.SceneLeftAction = SceneLeftHandler;
                    item.SceneRightAction = SceneRightHandler;
                    item.SceneDeleteAction = SceneDeleteHandler;
                }
                _addScene = _scenes.LastOrDefault();
                SelectedScene = _scenes.FirstOrDefault();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
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
            try
            {
                int index = _scenes.Count;
                SceneModel newScene = new SceneModel
                {
                    Name = index.ToString(),
                    Index = index,
                    SceneDeleteAction = SceneDeleteHandler,
                    SceneLeftAction = SceneLeftHandler,
                    SceneRightAction = SceneRightHandler,
                };
                _scenes.Insert(index - 1, newScene);
                ++_addScene.Index;
                _selectedScene = newScene;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void SceneLeftHandler(SceneModel model)
        {
            try
            {
                if (model == null) return;
                int index = model.Index;
                if (index == 1) return;
                SceneModel previous = Scenes[index - 2];
                previous.Name = model.Name;
                previous.Index = model.Index;
                model.Name = (index - 1).ToString();
                model.Index = index - 1;
                Scenes.RemoveAt(index - 2);
                Scenes.Insert(index - 1, previous);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void SceneRightHandler(SceneModel model)
        {
            try
            {
                if (model == null) return;
                int index = model.Index;
                if (index >= Scenes.Count - 1) return;
                SceneModel next = Scenes[index];
                model.Name = next.Name;
                model.Index = next.Index;
                next.Name = index.ToString();
                next.Index = index;
                Scenes.RemoveAt(index - 1);
                Scenes.Insert(index, model);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void SceneDeleteHandler(SceneModel model)
        {
            try
            {
                if (model == null) return;
                int index = model.Index;
                if (index == 1) SelectedScene = Scenes[index];
                else SelectedScene = Scenes[index - 2];
                Scenes.RemoveAt(index - 1);
                for (int i = 1; i < Scenes.Count; i++)
                {
                    SceneModel scene = Scenes[i - 1];
                    scene.Name = i.ToString();
                    scene.Index = i;
                }
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }
    }
}
