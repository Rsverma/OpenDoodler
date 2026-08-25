using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OpenBoardAnim.Utils
{
    // Live in-app preview playback (ProjectPreviewView). Split out from ExportRenderHandler
    // deliberately - both used to live in one isExport-branching method, but that made every
    // export-only timing change a risk to preview too. The two remain free to diverge in how they
    // drive time (export steps a fixed frame clock forward; this scrubs/plays a live Canvas), but
    // both now share SceneTimelineEngine's "what does this scene look like at time t" math, since
    // that question has exactly one right answer regardless of who's asking.
    //
    // This is an instance, not the old static PlayAsync: a scrub timeline needs a persistent
    // notion of "where we currently are" that both a Play/Pause loop and a dragged Slider can
    // read and write, which a fire-and-forget async method can't offer. One instance is built per
    // ProjectPreviewView and owns the canvas, the built timeline layout, and both audio players
    // for its lifetime; call Dispose() when the view closes.
    public sealed class PreviewPlaybackHandler : IDisposable
    {
        // One scene's place on the flat, absolute-seconds preview timeline: an optional
        // transition segment [TransitionStart, ContentStart) immediately followed by the scene's
        // own content segment [ContentStart, ContentStart + ContentDuration). ContentDuration is
        // SceneRenderHelpers.GetEstimatedSceneDurationSeconds's estimate, not the exact duration
        // SceneTimelineEngine.BuildScenePlan computes once the scene is actually built - close
        // enough for slider layout (same tolerance GetEstimatedSceneDurationSeconds's own callers
        // already accept), and ApplyGraphicState/ApplyCameraState's own [0,1] clamping absorbs
        // any tiny mismatch at a scene's very end harmlessly.
        private sealed class SceneLayout
        {
            public int SceneIndex;
            public double TransitionStart;
            public double TransitionDuration;
            public SceneTransition TransitionType;
            public double ContentStart;
            public double ContentDuration;
        }

        private readonly ProjectDetails _project;
        private readonly Canvas _canvas;
        private readonly List<SceneLayout> _layout = new();
        private readonly EntranceStyle _entranceStyle;
        private readonly HandStyle _handStyle;
        private readonly SceneTransition _sceneTransitionDefault;
        private readonly Brush _strokeBrush;
        private readonly double _strokeWidth;
        private readonly double _cameraViewportWidth;
        private readonly double _cameraViewportHeight;
        private readonly Image _hand;

        private readonly ScaleTransform _cameraScale = new(1, 1);
        private readonly TranslateTransform _cameraTranslate = new(0, 0);

        // Where CurrentTime's audio (background music) offset/cap map to - see the constructor
        // for why single-scene preview needs both, mirroring the old Button_Click logic.
        private readonly double _musicOffsetSeconds;
        private readonly double? _musicCapSeconds;

        // Whichever segment is currently reflected on the live canvas - null until the first
        // Seek. Rebuilt only when Seek's target segment/phase differs from this, not every call.
        private SceneLayout _builtSegment;
        private bool _builtInTransition;
        private SceneTimelinePlan _activePlan;
        private bool _handAddedForActiveScene;
        private Rectangle _transitionOverlay;

        private MediaPlayer _musicPlayer;
        private MediaPlayer _voiceoverPlayer;
        private int _voiceoverSceneIndex = -1;

        private double _lastRenderingTimeSeconds = -1;

        public double TotalDuration { get; }
        public double CurrentTime { get; private set; }
        public bool IsPlaying { get; private set; }

        // Raised on every Seek (both from dragging and from auto-play ticking) so the view can
        // keep its Slider/time label in sync without polling.
        public event Action<double> TimeChanged;
        // Raised once when auto-play reaches the end on its own (not from a manual Seek to the
        // end) so the view can flip its Play button back to "Play".
        public event Action PlaybackEnded;

        public PreviewPlaybackHandler(ProjectDetails project, Canvas canvas)
        {
            _project = project ?? throw new ArgumentNullException(nameof(project));
            _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            // A zoomed-in camera effect would otherwise render past the canvas's own bounds.
            _canvas.ClipToBounds = true;

            _cameraViewportWidth = project.Settings?.EditorWidth ?? 0;
            _cameraViewportHeight = project.Settings?.EditorHeight ?? 0;
            _entranceStyle = project.Settings?.EntranceStyle ?? EntranceStyle.HandDrawn;
            _handStyle = project.Settings?.HandStyle ?? HandStyle.LightSkin;
            _sceneTransitionDefault = project.Settings?.SceneTransition ?? SceneTransition.None;
            _strokeBrush = Brushes.Black;
            try
            {
                if (!string.IsNullOrWhiteSpace(project.Settings?.StrokeColorHex))
                    _strokeBrush = (Brush)new BrushConverter().ConvertFromString(project.Settings.StrokeColorHex);
            }
            catch (FormatException) { /* keep default black on an unparsable hex value */ }
            _strokeWidth = project.Settings != null && project.Settings.StrokeWidth > 0 ? project.Settings.StrokeWidth : 1;
            double transitionDurationSeconds = Math.Max(0.05, project.Settings?.TransitionDurationSeconds ?? 0.6);

            _hand = new Image { Source = SceneRenderHelpers.ResolveHandImage(_handStyle, project.Settings?.CustomHandImagePath) };

            // Excludes the trailing "+" add-scene card either way; PreviewSceneIndex further
            // narrows this to a single scene for an isolated preview (see
            // ProjectDetails.PreviewSceneIndex) instead of always starting from scene 1.
            int startSceneIndex = 0;
            int endSceneIndex = project.Scenes.Count - 2;
            if (project.PreviewSceneIndex is int previewIndex && previewIndex >= 0 && previewIndex <= endSceneIndex)
            {
                startSceneIndex = previewIndex;
                endSceneIndex = previewIndex;
            }

            double cursor = 0;
            for (int i = startSceneIndex; i <= endSceneIndex; i++)
            {
                SceneTransition transitionType = i > startSceneIndex
                    ? SceneRenderHelpers.GetEffectiveTransition(project.Scenes[i - 1], _sceneTransitionDefault)
                    : SceneTransition.None;
                double transitionDuration = transitionType != SceneTransition.None ? transitionDurationSeconds : 0;
                double transitionStart = cursor;
                double contentStart = transitionStart + transitionDuration;
                double contentDuration = SceneRenderHelpers.GetEstimatedSceneDurationSeconds(project.Scenes[i]);

                _layout.Add(new SceneLayout
                {
                    SceneIndex = i,
                    TransitionStart = transitionStart,
                    TransitionDuration = transitionDuration,
                    TransitionType = transitionType,
                    ContentStart = contentStart,
                    ContentDuration = contentDuration
                });
                cursor = contentStart + contentDuration;
            }
            TotalDuration = cursor;

            // Roughly lines the background-music track up with where an isolated single-scene
            // preview would fall in the full project (an estimate, same as GetEstimatedScene
            // DurationSeconds itself), and caps it so it doesn't bleed into the next scene's
            // portion - mirrors the old Button_Click logic, just computed once up front instead
            // of per Play click.
            double musicOffset = Math.Max(0, project.AudioTrimStart);
            double? musicCap = project.AudioTrimEnd > project.AudioTrimStart ? project.AudioTrimEnd : null;
            if (project.PreviewSceneIndex is int isolatedIndex && isolatedIndex >= 0 && isolatedIndex <= project.Scenes.Count - 2)
            {
                double priorOffset = 0;
                for (int s = 0; s < isolatedIndex; s++)
                    priorOffset += SceneRenderHelpers.GetEstimatedSceneDurationSeconds(project.Scenes[s]);
                musicOffset += priorOffset;
                double sceneCap = musicOffset + TotalDuration;
                musicCap = musicCap.HasValue ? Math.Min(musicCap.Value, sceneCap) : sceneCap;
            }
            _musicOffsetSeconds = musicOffset;
            _musicCapSeconds = musicCap;

            Seek(0);
        }

        // Moves the live canvas to show exactly what time (seconds, absolute across the whole
        // previewed range) looks like - the one entry point both dragging the Slider and each
        // Play tick go through, so both always agree on what a given time renders as.
        public void Seek(double time)
        {
            double clamped = Math.Clamp(time, 0, TotalDuration);
            CurrentTime = clamped;

            SceneLayout segment = FindSegment(clamped);
            if (segment == null)
            {
                TimeChanged?.Invoke(CurrentTime);
                return;
            }

            bool inTransition = clamped < segment.ContentStart;
            if (!ReferenceEquals(segment, _builtSegment) || inTransition != _builtInTransition)
                EnterSegment(segment, inTransition);

            if (inTransition)
            {
                double localT = segment.TransitionDuration > 0
                    ? Math.Clamp((clamped - segment.TransitionStart) / segment.TransitionDuration, 0, 1)
                    : 1;
                if (segment.TransitionType == SceneTransition.Wipe)
                    _transitionOverlay.Width = localT * _canvas.Width;
                else
                    _transitionOverlay.Opacity = localT;
            }
            else
            {
                double localT = clamped - segment.ContentStart;
                foreach (GraphicPlan plan in _activePlan.GraphicPlans)
                    SceneTimelineEngine.ApplyGraphicState(plan, localT, _handAddedForActiveScene ? _hand : null);
                SceneTimelineEngine.ApplyCameraState(_activePlan.CameraPlans, _cameraScale, _cameraTranslate, localT);
            }

            _canvas.UpdateLayout();

            if (IsPlaying)
                ApplyAudioCaps(segment, inTransition);

            TimeChanged?.Invoke(CurrentTime);
        }

        public void Play()
        {
            if (IsPlaying) return;
            if (CurrentTime >= TotalDuration)
                Seek(0);
            IsPlaying = true;
            _lastRenderingTimeSeconds = -1;
            CompositionTarget.Rendering += OnRendering;
            SyncAudioForNewSegment(_builtSegment, _builtInTransition);
        }

        public void Pause()
        {
            if (!IsPlaying) return;
            IsPlaying = false;
            CompositionTarget.Rendering -= OnRendering;
            _musicPlayer?.Pause();
            _voiceoverPlayer?.Pause();
        }

        public void Dispose()
        {
            Pause();
            _musicPlayer?.Close();
            _musicPlayer = null;
            _voiceoverPlayer?.Close();
            _voiceoverPlayer = null;
        }

        private void OnRendering(object sender, EventArgs e)
        {
            if (e is not RenderingEventArgs args) return;
            double now = args.RenderingTime.TotalSeconds;
            if (_lastRenderingTimeSeconds < 0)
            {
                _lastRenderingTimeSeconds = now;
                return;
            }
            double delta = now - _lastRenderingTimeSeconds;
            _lastRenderingTimeSeconds = now;

            double next = CurrentTime + delta;
            if (next >= TotalDuration)
            {
                Seek(TotalDuration);
                Pause();
                PlaybackEnded?.Invoke();
                return;
            }
            Seek(next);
        }

        private SceneLayout FindSegment(double time)
        {
            for (int i = 0; i < _layout.Count; i++)
            {
                double segmentEnd = _layout[i].ContentStart + _layout[i].ContentDuration;
                if (time < segmentEnd || i == _layout.Count - 1)
                    return _layout[i];
            }
            return null;
        }

        // Rebuilds the live canvas for a newly-entered segment - either a transition (the
        // outgoing scene's fully-drawn snapshot with a white wipe/crossfade overlay on top,
        // mirroring ExportRenderHandler.RunTransitionAsync's "whatever's already on screen"
        // approach but via a static snapshot instead of live elements, so scrubbing straight into
        // the middle of a transition without ever having played the outgoing scene still renders
        // correctly) or a scene's content (fresh graphic/camera plans from SceneTimelineEngine).
        private void EnterSegment(SceneLayout segment, bool inTransition)
        {
            _canvas.Children.Clear();

            if (inTransition)
            {
                int layoutIndex = _layout.IndexOf(segment);
                int outgoingSceneIndex = layoutIndex > 0 ? _layout[layoutIndex - 1].SceneIndex : segment.SceneIndex;
                RenderTargetBitmap snapshot = SceneRenderHelpers.RenderSceneSnapshot(_project, outgoingSceneIndex);
                if (snapshot != null)
                {
                    Image snapshotImage = new()
                    {
                        Source = snapshot,
                        Width = _cameraViewportWidth,
                        Height = _cameraViewportHeight,
                        Stretch = Stretch.Fill
                    };
                    _canvas.Children.Add(snapshotImage);
                }

                _transitionOverlay = new Rectangle
                {
                    Fill = Brushes.White,
                    Width = segment.TransitionType == SceneTransition.Wipe ? 0 : _canvas.Width,
                    Height = _canvas.Height,
                    Opacity = segment.TransitionType == SceneTransition.Wipe ? 1 : 0
                };
                Canvas.SetLeft(_transitionOverlay, 0);
                Canvas.SetTop(_transitionOverlay, 0);
                Canvas.SetZIndex(_transitionOverlay, 1000);
                _canvas.Children.Add(_transitionOverlay);

                _activePlan = null;
            }
            else
            {
                // RenderTransform is a canvas-level property Children.Clear() doesn't touch -
                // reset it for every scene's content (camera effects or not) so a previous
                // scene's pan/zoom end-state can't bleed into this one. Left untouched during a
                // transition (above) so the outgoing scene's last camera position keeps applying
                // to its snapshot, matching ExportRenderHandler's equivalent behavior.
                _cameraScale.ScaleX = 1;
                _cameraScale.ScaleY = 1;
                _cameraTranslate.X = 0;
                _cameraTranslate.Y = 0;
                _canvas.RenderTransform = new TransformGroup { Children = { _cameraScale, _cameraTranslate } };

                _handAddedForActiveScene = _entranceStyle == EntranceStyle.HandDrawn && _handStyle != HandStyle.None;
                if (_handAddedForActiveScene)
                {
                    _canvas.Children.Add(_hand);
                    Canvas.SetLeft(_hand, 0);
                    Canvas.SetTop(_hand, 1150);
                    Canvas.SetZIndex(_hand, 1);
                    _hand.RenderTransform = new MatrixTransform();
                }

                SceneModel scene = _project.Scenes[segment.SceneIndex];
                _activePlan = SceneTimelineEngine.BuildScenePlan(_canvas, scene, _entranceStyle, _strokeBrush, _strokeWidth,
                    _cameraViewportWidth, _cameraViewportHeight);
            }

            _builtSegment = segment;
            _builtInTransition = inTransition;

            if (IsPlaying)
                SyncAudioForNewSegment(segment, inTransition);
        }

        private void EnsureMusicPlayer()
        {
            if (_musicPlayer != null || string.IsNullOrWhiteSpace(_project.AudioPath) || !File.Exists(_project.AudioPath))
                return;
            _musicPlayer = new MediaPlayer();
            _musicPlayer.Open(new Uri(_project.AudioPath));
            _musicPlayer.Volume = _project.AudioVolume / 100.0;
        }

        // Starts (or repositions) both audio tracks for wherever CurrentTime currently is -
        // called when Play begins and whenever auto-play ticks across into a new segment. Not
        // called on every tick/every Seek while already playing: reassigning a MediaPlayer's
        // Position continuously (rather than just once, then letting it run) causes audible
        // stutter, so once started each track is left to play freely until the next segment
        // change or an explicit Pause.
        private void SyncAudioForNewSegment(SceneLayout segment, bool inTransition)
        {
            if (segment == null) return;

            EnsureMusicPlayer();
            if (_musicPlayer != null)
            {
                _musicPlayer.Position = TimeSpan.FromSeconds(Math.Max(0, _musicOffsetSeconds + CurrentTime));
                _musicPlayer.Play();
            }

            if (inTransition || segment.SceneIndex != _voiceoverSceneIndex)
            {
                _voiceoverPlayer?.Close();
                _voiceoverPlayer = null;
                _voiceoverSceneIndex = -1;
            }

            if (inTransition) return;

            SceneModel scene = _project.Scenes[segment.SceneIndex];
            if (string.IsNullOrWhiteSpace(scene.VoiceoverPath) || !File.Exists(scene.VoiceoverPath))
                return;

            if (_voiceoverPlayer == null)
            {
                _voiceoverPlayer = new MediaPlayer();
                _voiceoverPlayer.Open(new Uri(scene.VoiceoverPath));
                _voiceoverSceneIndex = segment.SceneIndex;
            }
            double localTime = CurrentTime - segment.ContentStart;
            _voiceoverPlayer.Position = TimeSpan.FromSeconds(Math.Max(0, scene.VoiceoverTrimStart) + localTime);
            _voiceoverPlayer.Play();
        }

        // Pauses either track once its own trim-end is reached - the polling-timer equivalent
        // this replaces (SceneRenderHelpers.StartTrimStopTimer) isn't needed since Seek already
        // runs every frame while playing, so the check just rides along with that.
        private void ApplyAudioCaps(SceneLayout segment, bool inTransition)
        {
            if (_musicPlayer != null && _musicCapSeconds.HasValue && _musicOffsetSeconds + CurrentTime >= _musicCapSeconds.Value)
                _musicPlayer.Pause();

            if (_voiceoverPlayer == null || inTransition) return;
            SceneModel scene = _project.Scenes[segment.SceneIndex];
            if (scene.VoiceoverTrimEnd <= scene.VoiceoverTrimStart) return;
            double voiceoverPosition = Math.Max(0, scene.VoiceoverTrimStart) + (CurrentTime - segment.ContentStart);
            if (voiceoverPosition >= scene.VoiceoverTrimEnd)
                _voiceoverPlayer.Pause();
        }
    }
}
