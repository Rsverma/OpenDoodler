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
            CameraEffects = new BindingList<CameraEffectModel>();
            ReplaceSceneCommand = new RelayCommand(ReplaceSceneCommandHandler, canExecute: o => true);
            SceneLeftCommand = new RelayCommand(SceneLeftCommandHandler, canExecute: o => true);
            SceneRightCommand = new RelayCommand(SceneRightCommandHandler, canExecute: o => true);
            SceneDeleteCommand = new RelayCommand(SceneDeleteCommandHandler, canExecute: o => true);
            SceneDuplicateCommand = new RelayCommand(SceneDuplicateCommandHandler, canExecute: o => true);
            AddCameraEffectCommand = new RelayCommand(AddCameraEffectCommandHandler, canExecute: o => true);
        }

        private void SceneDeleteCommandHandler(object obj)
        {
            SceneDeleteAction?.Invoke(this);
        }

        private void SceneDuplicateCommandHandler(object obj)
        {
            SceneDuplicateAction?.Invoke(this);
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
        public Action<SceneModel> SceneDuplicateAction;
        // Wired by EditorTimelineViewModel.WireGraphicsNotifications (same as the Action fields
        // above) to the pub/sub CameraEffectRequested route - lets the Scene Settings dialog
        // (whose DataContext is just this SceneModel, with no direct ViewModel access) trigger
        // the same "Add Camera Effect" flow as the timeline's right-click menu and Actions panel.
        public Action<SceneModel> AddCameraEffectAction;
        private void ReplaceSceneCommandHandler(object obj)
        {
            ReplaceScene?.Invoke(this);
        }

        private void AddCameraEffectCommandHandler(object obj)
        {
            AddCameraEffectAction?.Invoke(this);
        }

        public SceneModel Clone()
        {
            return new SceneModel
            {
                Name = Name,
                Index = Index,
                Graphics = new BindingList<GraphicModelBase>(Graphics.Select(x=>x.Clone()).ToList()),
                VoiceoverPath = VoiceoverPath,
                VoiceoverTrimStart = VoiceoverTrimStart,
                VoiceoverTrimEnd = VoiceoverTrimEnd,
                CameraEffects = new BindingList<CameraEffectModel>(CameraEffects.Select(e => e.Clone()).ToList()),
                TransitionOverride = TransitionOverride,
            };
        }

        private string _voiceoverPath;
        public string VoiceoverPath
        {
            get { return _voiceoverPath; }
            set
            {
                _voiceoverPath = value;
                OnPropertyChanged();
            }
        }

        // Seconds into the voiceover file to start playback from. Both trim fields are plain
        // seconds rather than nullable so they bind directly to a NumericUpDown; 0 for the end
        // is the sentinel for "no explicit end - play to the file's natural end", since an
        // actual end time of 0 (silence) is never a value anyone would intentionally choose.
        private double _voiceoverTrimStart;
        public double VoiceoverTrimStart
        {
            get { return _voiceoverTrimStart; }
            set
            {
                _voiceoverTrimStart = value;
                OnPropertyChanged();
            }
        }

        private double _voiceoverTrimEnd;
        public double VoiceoverTrimEnd
        {
            get { return _voiceoverTrimEnd; }
            set
            {
                _voiceoverTrimEnd = value;
                OnPropertyChanged();
            }
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

        private BindingList<CameraEffectModel> _cameraEffects;
        public BindingList<CameraEffectModel> CameraEffects
        {
            get { return _cameraEffects; }
            set
            {
                _cameraEffects = value;
                // BindingList<T> only raises ListChanged, not PropertyChanged, so a binding to
                // CameraEffects.Count (the timeline badge) wouldn't refresh on add/remove without
                // this - re-raising CameraEffects itself forces WPF to re-walk the binding path.
                if (_cameraEffects != null)
                    _cameraEffects.ListChanged += (s, e) => OnPropertyChanged(nameof(CameraEffects));
                OnPropertyChanged();
            }
        }
        // Overrides ProjectSettings.SceneTransition for the transition that plays as this scene
        // ends and the next one begins - Inherit (the default) leaves the project-wide setting
        // in effect, so existing projects (saved before this existed) keep behaving exactly as
        // before. Has no effect on the last real scene, which has no "next" scene to transition
        // into (see PreviewAndExportHandler.RunAnimationsOnCanvas).
        private SceneTransitionOverride _transitionOverride = SceneTransitionOverride.Inherit;
        public SceneTransitionOverride TransitionOverride
        {
            get { return _transitionOverride; }
            set
            {
                _transitionOverride = value;
                OnPropertyChanged();
            }
        }

        public int Index { get; set; }
        [JsonIgnore]
        public ICommand ReplaceSceneCommand { get; set; }
        [JsonIgnore]
        public ICommand SceneLeftCommand { get; set; }
        [JsonIgnore]
        public ICommand SceneRightCommand { get; set; }
        [JsonIgnore]
        public ICommand SceneDeleteCommand { get; set; }
        [JsonIgnore]
        public ICommand SceneDuplicateCommand { get; set; }
        [JsonIgnore]
        public ICommand AddCameraEffectCommand { get; set; }

    }

    // Inherit defers to ProjectSettings.SceneTransition; the other members mirror
    // Models.SceneTransition and force that specific transition (or none) for this scene's
    // outgoing boundary regardless of the project-wide default.
    public enum SceneTransitionOverride
    {
        Inherit,
        None,
        Crossfade,
        Wipe
    }
}
