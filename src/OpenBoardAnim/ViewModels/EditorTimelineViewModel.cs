using OpenBoardAnim.Core;
using OpenBoardAnim.Models;
using OpenBoardAnim.Services;
using OpenBoardAnim.Utilities;
using OpenBoardAnim.Utils;
using System.ComponentModel;
using System.Windows.Input;
using System.Xml.Linq;

namespace OpenBoardAnim.ViewModels
{
    public class EditorTimelineViewModel : ViewModel
    {
        // Base pixel scale used to lay scenes out proportionally to their estimated duration -
        // sum of each graphic's Delay + Duration. A rough estimate (hand-drawn stroke timing
        // isn't known ahead of time), good enough for a navigational timeline. Scaled at
        // render time by ZoomLevel.
        private const double BasePixelsPerSecond = 40;
        private const double BaseMinSegmentWidth = 160;
        // Segments never shrink narrower than this, even zoomed all the way out, so a scene
        // card stays clickable.
        private const double AbsoluteMinSegmentWidth = 40;
        private const double SegmentGap = 6;
        // Floor on a transition block's width, even zoomed all the way out or at a very short
        // duration, so its icon stays visible - otherwise it's sized to TransitionDurationSeconds
        // at the current zoom, same as segment widths are sized to scene duration.
        private const double MinTransitionBlockWidth = 20;
        private const double TransitionBlockHeight = 20;
        private const double MinZoom = 0.25;
        private const double MaxZoom = 4.0;
        private const double ZoomStep = 1.25;

        // Fixed row layout shared between the timeline's own Canvas and the track-header icon
        // column next to it (EditorTimelineView.xaml) - kept as one source of truth here so the
        // two Canvases can never drift out of vertical alignment. Order: time ruler, voiceover
        // (audio) track, camera-effects track, scene/graphics track, background-music track.
        public double TimelineCanvasHeight => 170;

        // "Nice" tick intervals (seconds) to choose from, smallest to largest - RecomputeTimeRuler
        // picks the first one wide enough (in pixels, at the current zoom) to keep labels legible.
        private static readonly double[] NiceTickIntervalsSeconds = { 1, 2, 5, 10, 15, 30, 60, 120, 300, 600 };
        private const double MinPixelsBetweenTicks = 50;

        private double PixelsPerSecond => BasePixelsPerSecond * _zoomLevel;
        private double MinSegmentWidth => Math.Max(AbsoluteMinSegmentWidth, BaseMinSegmentWidth * _zoomLevel);

        private readonly IPubSubService _pubSub;
        private readonly IDialogService _dialog;
        private SceneModel _addScene;
        public ICommand ZoomInCommand { get; set; }
        public ICommand ZoomOutCommand { get; set; }
        public ICommand ResetZoomCommand { get; set; }
        public ICommand PreviewSceneCommand { get; set; }
        public ICommand AddCameraEffectCommand { get; set; }
        public EditorTimelineViewModel(IPubSubService pubSub, IDialogService dialog)
        {
            _pubSub = pubSub;
            _dialog = dialog;
            _pubSub.Subscribe(SubTopic.SceneReplaced, SceneReplacedHandler);
            _pubSub.Subscribe(SubTopic.SceneTemplateInserted, SceneTemplateInsertedHandler);
            ZoomInCommand = new RelayCommand(o => ZoomLevel *= ZoomStep, o => ZoomLevel < MaxZoom - 0.001);
            ZoomOutCommand = new RelayCommand(o => ZoomLevel /= ZoomStep, o => ZoomLevel > MinZoom + 0.001);
            ResetZoomCommand = new RelayCommand(o => ZoomLevel = 1.0, o => Math.Abs(ZoomLevel - 1.0) > 0.001);
            PreviewSceneCommand = new RelayCommand(o => PreviewScene(o as SceneModel), canExecute: o => Project != null);
            // The actual "add" logic lives on EditorActionsViewModel (a sibling view-model with
            // no direct reference here) - routed through the pub/sub mediator, same as
            // SubTopic.SceneChanged, rather than reaching across view-models directly.
            AddCameraEffectCommand = new RelayCommand(
                o => { if (o is SceneModel scene) _pubSub.Publish(SubTopic.CameraEffectRequested, scene); },
                canExecute: o => Project != null);
            Segments = new BindingList<SceneTimelineSegment>();
            TimeRulerTicks = new BindingList<TimeRulerTick>();
            TransitionBlocks = new BindingList<SceneTransitionTimelineBlock>();
        }

        // Isolates the preview dialog to just this scene, instead of always previewing the
        // whole project from scene 1 - see ProjectDetails.PreviewSceneIndex. Always cleared
        // again once the (modal) dialog closes, so it can't leak into a later whole-project
        // preview from the Actions panel's own Preview button.
        private void PreviewScene(SceneModel scene)
        {
            try
            {
                if (scene == null || Project == null) return;
                Project.PreviewSceneIndex = Scenes.IndexOf(scene);
                _ = _dialog.ShowDialog(DialogType.PreviewProject, Project);
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
            finally
            {
                if (Project != null)
                    Project.PreviewSceneIndex = null;
            }
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
                RecomputeSegments();
            }
        }

        public string ZoomPercentageText => $"{_zoomLevel * 100:0}%";

        private void SceneReplacedHandler(object obj)
        {
            try
            {
                int index = SelectedScene.Index;

                SceneModel scene = (SceneModel)obj;
                scene.Index = index;
                WireGraphicsNotifications(scene);
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

        // Inserts a scene-template gallery selection as a brand-new scene right after the
        // currently selected one, rather than overwriting anything (unlike SceneReplacedHandler
        // above) - picking a starter layout should never destroy existing work.
        private void SceneTemplateInsertedHandler(object obj)
        {
            try
            {
                if (obj is not SceneModel template) return;
                int position = SelectedScene != null ? Scenes.IndexOf(SelectedScene) : -1;
                if (position < 0) position = Scenes.Count - 1;
                InsertSceneAfter(position, template);
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
                // TransitionBlocks (unlike AudioPath above) is computed imperatively rather than
                // bound straight to Settings.SceneTransition, so a change made in the Project
                // Settings dialog needs an explicit nudge to reach the timeline's markers.
                if (_project?.Settings != null)
                {
                    _project.Settings.PropertyChanged -= ProjectSettingsPropertyChangedHandler;
                    _project.Settings.PropertyChanged += ProjectSettingsPropertyChangedHandler;
                }
                // Scenes is assigned right after Project by EditorViewModel.LoadProjectIntoEditor -
                // guard against the brief window where Project is set but Scenes (and so
                // Segments, which TransitionBlocks is derived alongside) isn't populated yet.
                if (_scenes != null)
                    RecomputeSegments();
            }
        }

        private void ProjectSettingsPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProjectSettings.SceneTransition) || e.PropertyName == nameof(ProjectSettings.TransitionDurationSeconds))
                RecomputeSegments();
        }

        // Width of the background-music layer - spans every real scene (excluding the
        // trailing "+" add-scene card), same span the playhead is clamped to.
        private double _realContentWidth = BaseMinSegmentWidth;
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

        private double _totalWidth = BaseMinSegmentWidth;
        public double TotalWidth
        {
            get { return _totalWidth; }
            private set
            {
                _totalWidth = value;
                OnPropertyChanged();
            }
        }

        // Time-ruler tick marks along the top of the timeline. Purely a pixels-per-second
        // reading of the same rough duration estimate used for segment widths (see
        // GetEstimatedDurationSeconds) - not reconciled against individual segments' actual
        // (possibly MinSegmentWidth-floored) widths, since this is a navigational aid rather
        // than a frame-accurate scale.
        public BindingList<TimeRulerTick> TimeRulerTicks
        {
            get { return _timeRulerTicks; }
            private set
            {
                _timeRulerTicks = value;
                OnPropertyChanged();
            }
        }
        private BindingList<TimeRulerTick> _timeRulerTicks;

        // Scene-to-scene transition markers on the graphics track - see
        // SceneTransitionTimelineBlock and SceneRenderHelpers.GetEffectiveTransition.
        public BindingList<SceneTransitionTimelineBlock> TransitionBlocks
        {
            get { return _transitionBlocks; }
            private set
            {
                _transitionBlocks = value;
                OnPropertyChanged();
            }
        }
        private BindingList<SceneTransitionTimelineBlock> _transitionBlocks;

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
                    WireGraphicsNotifications(item);
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

        // Segment widths are estimated from each graphic's Delay/Duration (see
        // GetEstimatedDurationSeconds) but RecomputeSegments was previously only ever called
        // from scene-structure operations (add/remove/reorder/duplicate/replace) - editing an
        // already-placed graphic's Delay/Duration in the Actions panel silently left its
        // scene's segment stale until something else happened to trigger a recompute (a scene
        // switch, reorder, or relaunching the app to reload the project from scratch). Wiring
        // directly to each graphic's own PropertyChanged closes that gap without needing every
        // duration-editing call site to know about the timeline. Also wires the scene's camera
        // effects the same way, since GetEstimatedDurationSeconds now factors those in too.
        private void WireGraphicsNotifications(SceneModel scene)
        {
            if (scene != null)
            {
                scene.PropertyChanged += ScenePropertyChangedHandler;
                // Routes SceneModel.AddCameraEffectCommand (bound directly from the Scene
                // Settings dialog, which has no ViewModel to reach) through the same pub/sub
                // topic the timeline's own right-click menu already uses - EditorActionsViewModel
                // is the one actually subscribed and holding the Project context this needs.
                scene.AddCameraEffectAction = s => _pubSub.Publish(SubTopic.CameraEffectRequested, s);
            }

            if (scene?.Graphics != null)
            {
                scene.Graphics.ListChanged += (s, e) =>
                {
                    if (e.ListChangedType == ListChangedType.ItemAdded && e.NewIndex >= 0 && e.NewIndex < scene.Graphics.Count)
                        scene.Graphics[e.NewIndex].PropertyChanged += GraphicPropertyChangedHandler;
                    RecomputeSegments();
                };
                foreach (GraphicModelBase graphic in scene.Graphics)
                    graphic.PropertyChanged += GraphicPropertyChangedHandler;
            }

            if (scene?.CameraEffects != null)
            {
                scene.CameraEffects.ListChanged += (s, e) =>
                {
                    if (e.ListChangedType == ListChangedType.ItemAdded && e.NewIndex >= 0 && e.NewIndex < scene.CameraEffects.Count)
                        scene.CameraEffects[e.NewIndex].PropertyChanged += CameraEffectPropertyChangedHandler;
                    RecomputeSegments();
                };
                foreach (CameraEffectModel effect in scene.CameraEffects)
                    effect.PropertyChanged += CameraEffectPropertyChangedHandler;
            }
        }

        private void ScenePropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SceneModel.TransitionOverride))
                RecomputeSegments();
        }

        private void GraphicPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GraphicModelBase.Delay) || e.PropertyName == nameof(GraphicModelBase.Duration))
                RecomputeSegments();
        }

        private void CameraEffectPropertyChangedHandler(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(CameraEffectModel.StartTime) || e.PropertyName == nameof(CameraEffectModel.EndTime))
                RecomputeSegments();
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
                TransitionBlocks.Clear();
                SceneTransition projectDefaultTransition = Project?.Settings?.SceneTransition ?? SceneTransition.None;
                // Same clamp PreviewPlaybackHandler/ExportRenderHandler apply before actually playing a
                // transition - keeps the block's width meaningful even if the project's
                // duration field was left at 0 or a negative value.
                double transitionDurationSeconds = Math.Max(0.05, Project?.Settings?.TransitionDurationSeconds ?? 0.6);
                double x = 0;
                for (int i = 0; i < _scenes.Count; i++)
                {
                    SceneModel scene = _scenes[i];
                    double sceneDuration = GetEstimatedDurationSeconds(scene);
                    double width = Math.Max(MinSegmentWidth, sceneDuration * PixelsPerSecond);
                    Segments.Add(new SceneTimelineSegment
                    {
                        Scene = scene,
                        X = x,
                        Width = width,
                        IsSelected = scene == _selectedScene,
                        CameraEffectBlocks = new BindingList<CameraEffectTimelineBlock>(BuildCameraEffectBlocks(scene, width, sceneDuration))
                    });

                    // A transition only plays between two real scenes - never after the last
                    // real one (nothing follows it in playback) and never involving the
                    // trailing "+" add-scene card. See SceneRenderHelpers.GetEffectiveTransition.
                    bool hasNextRealScene = scene != _addScene && i + 1 < _scenes.Count && _scenes[i + 1] != _addScene;
                    SceneTransition effectiveTransition = hasNextRealScene
                        ? SceneRenderHelpers.GetEffectiveTransition(scene, projectDefaultTransition)
                        : SceneTransition.None;
                    double gap = SegmentGap;
                    if (effectiveTransition != SceneTransition.None)
                    {
                        // The block fills the entire gap - its width is what actually reads as
                        // "how much time this transition takes" on the timeline, same idea as a
                        // segment's own width standing in for scene duration.
                        gap = Math.Max(MinTransitionBlockWidth, transitionDurationSeconds * PixelsPerSecond);
                        TransitionBlocks.Add(new SceneTransitionTimelineBlock
                        {
                            X = x + width,
                            Width = gap,
                            Height = TransitionBlockHeight,
                            IsOverride = scene.TransitionOverride != SceneTransitionOverride.Inherit,
                            ToolTipText = scene.TransitionOverride != SceneTransitionOverride.Inherit
                                ? $"{effectiveTransition}, {transitionDurationSeconds:0.0}s (override on this scene)"
                                : $"{effectiveTransition}, {transitionDurationSeconds:0.0}s (project default)"
                        });
                    }
                    x += width + gap;
                }
                TotalWidth = Math.Max(x, MinSegmentWidth);
                SceneTimelineSegment lastRealSegment = Segments.LastOrDefault(s => s.Scene != _addScene);
                double lastRealSegmentEnd = lastRealSegment != null ? lastRealSegment.X + lastRealSegment.Width : 0;
                RealContentWidth = Math.Max(lastRealSegmentEnd, MinSegmentWidth);
                RecomputeTimeRuler();
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // Regenerates the time-ruler ticks from scratch at the current zoom level - cheap
        // enough (at most a few dozen ticks) to just rebuild rather than diff against the
        // previous set.
        private void RecomputeTimeRuler()
        {
            TimeRulerTicks.Clear();
            double intervalSeconds = ChooseTickIntervalSeconds();
            double totalSeconds = RealContentWidth / PixelsPerSecond;
            for (double seconds = 0; seconds <= totalSeconds + intervalSeconds; seconds += intervalSeconds)
                TimeRulerTicks.Add(new TimeRulerTick { X = seconds * PixelsPerSecond, Label = FormatTickLabel(seconds) });
        }

        private double ChooseTickIntervalSeconds()
        {
            foreach (double step in NiceTickIntervalsSeconds)
            {
                if (step * PixelsPerSecond >= MinPixelsBetweenTicks)
                    return step;
            }
            return NiceTickIntervalsSeconds[^1];
        }

        private static string FormatTickLabel(double seconds)
        {
            int total = (int)Math.Round(seconds);
            return $"{total / 60}:{total % 60:00}";
        }

        // Pixel position/width of each camera effect within its scene's own segment, directly
        // from its absolute StartTime/EndTime (seconds from the scene's start).
        private static List<CameraEffectTimelineBlock> BuildCameraEffectBlocks(SceneModel scene, double segmentWidth, double sceneDuration)
        {
            List<CameraEffectTimelineBlock> blocks = new();
            if (scene?.CameraEffects == null || sceneDuration <= 0) return blocks;

            // segmentWidth may be floored by MinSegmentWidth, so the scene's actual
            // seconds-to-pixels ratio can differ from the timeline's global PixelsPerSecond -
            // using the effective ratio keeps blocks from overflowing the card.
            double pixelsPerSecond = segmentWidth / sceneDuration;
            foreach (CameraEffectModel effect in scene.CameraEffects.OrderBy(e => e.StartTime))
            {
                double blockX = effect.StartTime * pixelsPerSecond;
                double blockWidth = Math.Max(4, (effect.EndTime - effect.StartTime) * pixelsPerSecond);
                blocks.Add(new CameraEffectTimelineBlock { X = blockX, Width = blockWidth, Effect = effect });
            }
            return blocks;
        }

        private static double GetEstimatedDurationSeconds(SceneModel scene)
        {
            if (scene == null) return 0;
            double graphicsTotal = scene.Graphics?.Sum(g => g.Delay + g.Duration) ?? 0;
            // Camera effects run as a second, concurrent timeline (see
            // SceneTimelineEngine.BuildScenePlan) - a scene's real duration is whichever of the
            // two actually runs longer. Effects are keyed by absolute EndTime, so the camera
            // timeline's length is just the latest one.
            double cameraTotal = scene.CameraEffects != null && scene.CameraEffects.Count > 0
                ? scene.CameraEffects.Max(e => e.EndTime)
                : 0;
            return Math.Max(graphicsTotal, cameraTotal);
        }

        private void UpdateSelectionState()
        {
            foreach (SceneTimelineSegment segment in Segments)
                segment.IsSelected = segment.Scene == _selectedScene;
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
                WireGraphicsNotifications(newScene);
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
                InsertSceneAfter(position, model.Clone());
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndShow))
                    throw;
            }
        }

        // Shared by SceneDuplicateHandler and SceneTemplateInsertedHandler - inserts newScene
        // right after position, renumbers every scene's Name/Index to match its new position,
        // and selects it.
        private void InsertSceneAfter(int position, SceneModel newScene)
        {
            newScene.SceneLeftAction = SceneLeftHandler;
            newScene.SceneRightAction = SceneRightHandler;
            newScene.SceneDeleteAction = SceneDeleteHandler;
            newScene.SceneDuplicateAction = SceneDuplicateHandler;
            WireGraphicsNotifications(newScene);

            Scenes.Insert(position + 1, newScene);
            for (int i = 1; i < Scenes.Count; i++)
            {
                SceneModel scene = Scenes[i - 1];
                scene.Name = i.ToString();
                scene.Index = i;
            }

            SelectedScene = newScene;
            RecomputeSegments();
        }

        // Reorders scenes via drag-and-drop on the timeline (EditorTimelineView's drag/drop
        // code-behind calls this) - moves dragged to right before target's current position.
        // Unlike SceneLeftHandler/SceneRightHandler (which only swap with an immediate
        // neighbor), this can jump a scene to any position in one gesture.
        public void MoveScene(SceneModel dragged, SceneModel target)
        {
            try
            {
                if (dragged == null || target == null || dragged == target) return;
                if (dragged == _addScene || target == _addScene) return;
                int oldIndex = Scenes.IndexOf(dragged);
                if (oldIndex < 0) return;

                Scenes.RemoveAt(oldIndex);
                int targetIndex = Scenes.IndexOf(target);
                if (targetIndex < 0)
                {
                    // target vanished mid-operation (shouldn't happen) - put dragged back rather
                    // than lose it.
                    Scenes.Insert(oldIndex, dragged);
                    return;
                }
                Scenes.Insert(targetIndex, dragged);

                for (int i = 1; i < Scenes.Count; i++)
                {
                    SceneModel scene = Scenes[i - 1];
                    scene.Name = i.ToString();
                    scene.Index = i;
                }

                SelectedScene = dragged;
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
