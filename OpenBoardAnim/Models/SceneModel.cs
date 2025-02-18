using OpenBoardAnim.Core;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace OpenBoardAnim.Models
{
    public class SceneModel : ObservableObject
    {
        public SceneModel()
        {
            Graphics = new BindingList<GraphicModelBase>();
            ReplaceSceneCommand = new RelayCommand(ReplaceSceneCommandHandler, canExecute: o => true);
            SceneLeftCommand = new RelayCommand(SceneLeftCommandHandler, canExecute: o => true);
            SceneRightCommand = new RelayCommand(SceneRightCommandHandler, canExecute: o => true);
            SceneDeleteCommand = new RelayCommand(SceneDeleteCommandHandler, canExecute: o => true);
        }

        private void SceneDeleteCommandHandler(object obj)
        {
            SceneDeleteAction?.Invoke(this);
        }

        private void SceneRightCommandHandler(object obj)
        {
            SceneRightAction?.Invoke(this);
        }

        private void SceneLeftCommandHandler(object obj)
        {
            SceneLeftAction?.Invoke(this);
        }

        public Action<SceneModel> ReplaceScene;
        public Action<SceneModel> SceneLeftAction;
        public Action<SceneModel> SceneRightAction;
        public Action<SceneModel> SceneDeleteAction;
        private void ReplaceSceneCommandHandler(object obj)
        {
            ReplaceScene?.Invoke(this);
        }

        public SceneModel Clone()
        {
            return new SceneModel
            {
                Name = Name,
                Graphics = new BindingList<GraphicModelBase>(Graphics.Select(x=>x.Clone()).ToList()),
            };
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        private BindingList<GraphicModelBase> _graphics;
        public BindingList<GraphicModelBase> Graphics
        {
            get { return _graphics; }
            set
            {
                _graphics = value;
                OnPropertyChanged();
            }
        }
        public int Index { get; set; }
        [JsonIgnore]
        public ICommand ReplaceSceneCommand { get; set; }
        public ICommand SceneLeftCommand { get; set; }
        public ICommand SceneRightCommand { get; set; }
        public ICommand SceneDeleteCommand { get; set; }

    }
}
