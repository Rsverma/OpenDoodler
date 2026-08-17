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
        // Pixel scale used to lay scenes out proportionally to their estimated duration -
        // sum of each graphic's Delay + Duration. A rough estimate (hand-drawn stroke timing
        // isn't known ahead of time), good enough for a navigational timeline.
        private const double PixelsPerSecond = 40;
        private const double MinSegmentWidth = 160;
        private const double SegmentGap = 6;

        private readonly IPubSubService _pubSub;
        private SceneModel _addScene;
        // Rightmost X the playhead may reach - the end of the last real scene's segment,
        // excluding the trailing "+" add-scene card so dragging can never land on it.
        private double _maxPlayheadX;
        public ICommand SceneDeleteCommand { get; set; }
        public EditorTimelineViewModel(IPubSubService pubSub)
        {
            _pubSub = pubSub;
            _pubSub.Subscribe(SubTopic.SceneReplaced, SceneReplacedHandler);
            SceneDeleteCommand = new RelayCommand(SceneDeleteCommandHandler, o => true);
            Segments = new BindingList<SceneTimelineSegment>();
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
                RecomputeSegments();
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
                RecomputeSegments();
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

        // Set by EditorViewModel alongside Scenes so the background-music layer can read
        // Project.AudioPath directly - live edits from the Project Settings dialog (same
        // ProjectDetails instance) reach this binding for free via its own OnPropertyChanged.
        private ProjectDetails _project;
        public ProjectDetails Project
        {
            get { return _project; }
            set
            {
                _project = value;
                OnPropertyChanged();
            }
        }

        // Width of the background-music layer - spans every real scene (excluding the
        // trailing "+" add-scene card), same span the playhead is clamped to.
        private double _realContentWidth = MinSegmentWidth;
        public double RealContentWidth
        {
            get { return _realContentWidth; }
            private set
            {
                _realContentWidth = value;
                OnPropertyChanged();
            }
        }

        private BindingList<SceneTimelineSegment> _segments;
        public BindingList<SceneTimelineSegment> Segments
        {
            get { return _segments; }
            private set
            {
                _segments = value;
                OnPropertyChanged();
            }
        }

        private double _totalWidth = MinSegmentWidth;
        public double TotalWidth
        {
            get { return _totalWidth; }
            private set
            {
                _totalWidth = value;
                OnPropertyChanged();
            }
        }

        private double _playheadX;
        public double PlayheadX
        {
            get { return _playheadX; }
            set
            {
                _playheadX = value;
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
                    item.SceneDuplicateAction = SceneDuplicateHandler;
                }
                _addScene = _scenes.LastOrDefault();
                SelectedScene = _scenes.FirstOrDefault();
                RecomputeSegments();
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
                    else
                    {
                        UpdateSelectionState();
                    }
                    OnPropertyChanged();
                    _pubSub.Publish(SubTopic.SceneChanged, _selectedScene);
                }
            }
        }

        // Recomputes each scene's timeline position/width from its estimated duration.
        // Called whenever scenes are added, removed, reordered, duplicated, or replaced -
        // not on every graphic edit, since duration only needs to be roughly right here.
        private void RecomputeSegments()
        {
            try
            {
                Segments.Clear();
                double x = 0;
                foreach (SceneModel scene in _scenes)
                {
                    double width = Math.Max(MinSegmentWidth, GetEstimatedDurationSeconds(scene) * PixelsPerSecond);
                    Segments.Add(new SceneTimelineSegment
                    {
                        Scene = scene,
                        X = x,
                        Width = width,
                        IsSelected = scene == _selectedScene
                    });
                    x += width + SegmentGap;
                }
                TotalWidth = Math.Max(x, MinSegmentWidth);
                SceneTimelineSegment lastRealSegment = Segments.LastOrDefault(s => s.Scene != _addScene);
                _maxPlayheadX = lastRealSegment != null ? lastRealSegment.X + lastRealSegment.Width : 0;
                RealContentWidth = Math.Max(_maxPlayheadX, MinSegmentWidth);
                UpdatePlayheadPosition();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private static double GetEstimatedDurationSeconds(SceneModel scene)
        {
            if (scene?.Graphics == null || scene.Graphics.Count == 0) return 0;
            return scene.Graphics.Sum(g => g.Delay + g.Duration);
        }

        private void UpdateSelectionState()
        {
            foreach (SceneTimelineSegment segment in Segments)
                segment.IsSelected = segment.Scene == _selectedScene;
            UpdatePlayheadPosition();
        }

        private void UpdatePlayheadPosition()
        {
            SceneTimelineSegment segment = Segments.FirstOrDefault(s => s.Scene == _selectedScene);
            PlayheadX = segment != null ? segment.X + segment.Width / 2 : 0;
        }

        // Live-updates the playhead's visual position while the user is dragging it, without
        // changing the selected scene yet - that only happens once the drag ends.
        public void MovePlayheadPreview(double deltaX)
        {
            PlayheadX = Math.Clamp(PlayheadX + deltaX, 0, _maxPlayheadX);
        }

        // Snaps the playhead to whichever real scene (excluding the trailing "+" add-scene
        // card) its dropped position is closest to, and selects it.
        public void CommitPlayheadPosition()
        {
            try
            {
                List<SceneTimelineSegment> candidates = Segments.Where(s => s.Scene != _addScene).ToList();
                if (candidates.Count == 0) return;
                SceneTimelineSegment nearest = candidates
                    .OrderBy(s => Math.Abs((s.X + s.Width / 2) - PlayheadX))
                    .First();
                SelectedScene = nearest.Scene;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
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
                    SceneDuplicateAction = SceneDuplicateHandler,
                };
                _scenes.Insert(index - 1, newScene);
                ++_addScene.Index;
                _selectedScene = newScene;
                RecomputeSegments();
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
                RecomputeSegments();
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
                RecomputeSegments();
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
                RecomputeSegments();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        private void SceneDuplicateHandler(SceneModel model)
        {
            try
            {
                if (model == null) return;
                int position = Scenes.IndexOf(model);
                if (position < 0) return;

                SceneModel duplicate = model.Clone();
                duplicate.SceneLeftAction = SceneLeftHandler;
                duplicate.SceneRightAction = SceneRightHandler;
                duplicate.SceneDeleteAction = SceneDeleteHandler;
                duplicate.SceneDuplicateAction = SceneDuplicateHandler;

                Scenes.Insert(position + 1, duplicate);
                for (int i = 1; i < Scenes.Count; i++)
                {
                    SceneModel scene = Scenes[i - 1];
                    scene.Name = i.ToString();
                    scene.Index = i;
                }

                SelectedScene = duplicate;
                RecomputeSegments();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }
    }
}
