using OpenBoardAnim.Models;
using OpenBoardAnim.Utilities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace OpenBoardAnim.Utils
{
    // Video export capture. Split from PreviewPlaybackHandler deliberately - both used to live
    // in one isExport-branching method, but that made every preview-only tweak a risk to export
    // and vice versa.
    //
    // Does NOT use WPF's Storyboard/Clock system at all: an earlier version drove a paused,
    // controllable Storyboard via Seek() per frame, but Seek doesn't reliably flush into the
    // property system before an off-screen RenderTargetBitmap.Render() reads it (it depends on
    // WPF's own composition tick, which nothing here was actually forcing) - every captured frame
    // in a scene came out identical, frozen on the scene's first frame. Instead, every property
    // this needs (via SceneTimelineEngine - stroke reveal, hand position, camera pan/zoom,
    // entrance opacity/scale) is computed analytically for the exact virtual time of each frame
    // and set directly - no clock, nothing to defer, so there's nothing that can silently
    // not-yet-apply. PreviewPlaybackHandler now shares that same engine for its own Seek/Play,
    // even though it drives time completely differently (a live, scrubbable Canvas rather than a
    // fixed 30fps frame-by-frame capture loop).
    public class ExportRenderHandler
    {
        private const double EndHoldSeconds = 0.5;

        public static async Task ExportAsync(ProjectDetails project, Canvas canvas, IProgress<ExportProgressInfo> progress = null, string outputVideoPath = null, CancellationToken cancellationToken = default)
        {
            VideoExporter exporter = null;
            try
            {
                if (project == null) return;
                canvas.ClipToBounds = true;
                double cameraViewportWidth = project.Settings?.EditorWidth ?? 0;
                double cameraViewportHeight = project.Settings?.EditorHeight ?? 0;
                EntranceStyle entranceStyle = project.Settings?.EntranceStyle ?? EntranceStyle.HandDrawn;
                HandStyle handStyle = project.Settings?.HandStyle ?? HandStyle.LightSkin;
                SceneTransition sceneTransition = project.Settings?.SceneTransition ?? SceneTransition.None;
                Brush strokeBrush = Brushes.Black;
                try
                {
                    if (!string.IsNullOrWhiteSpace(project.Settings?.StrokeColorHex))
                        strokeBrush = (Brush)new BrushConverter().ConvertFromString(project.Settings.StrokeColorHex);
                }
                catch (FormatException) { /* keep default black on an unparsable hex value */ }
                double strokeWidth = project.Settings != null && project.Settings.StrokeWidth > 0 ? project.Settings.StrokeWidth : 1;

                Image hand = new() { Source = SceneRenderHelpers.ResolveHandImage(handStyle, project.Settings?.CustomHandImagePath) };

                List<SceneAudioCue> sceneAudioCues = new();
                Dictionary<int, double> sceneStartTimes = new();
                List<(string Path, double Start, double TrimStart, double TrimEnd, int SceneIndex)> rawVoiceoverCues = new();
                double virtualClock = 0;
                const int exportFrameRate = 30;

                int startSceneIndex = 0;
                int endSceneIndex = project.Scenes.Count - 2;
                if (project.PreviewSceneIndex is int previewIndex && previewIndex >= 0 && previewIndex <= endSceneIndex)
                {
                    startSceneIndex = previewIndex;
                    endSceneIndex = previewIndex;
                }
                double transitionDurationSeconds = Math.Max(0.05, project.Settings?.TransitionDurationSeconds ?? 0.6);

                double estimatedSeconds = 0;
                for (int s = startSceneIndex; s <= endSceneIndex; s++)
                    estimatedSeconds += SceneRenderHelpers.GetEstimatedSceneDurationSeconds(project.Scenes[s]);
                for (int s = startSceneIndex; s < endSceneIndex; s++)
                    if (SceneRenderHelpers.GetEffectiveTransition(project.Scenes[s], sceneTransition) != SceneTransition.None)
                        estimatedSeconds += transitionDurationSeconds;
                estimatedSeconds += EndHoldSeconds;
                int estimatedTotalFrames = Math.Max(1, (int)Math.Round(estimatedSeconds * exportFrameRate));

                exporter = new(canvas, exportFrameRate, outputVideoPath, project.AudioPath, project.AudioVolume, sceneAudioCues,
                    project.AudioTrimStart, project.AudioTrimEnd, progress, estimatedTotalFrames);
                exporter.StartCapture();

                for (int i = startSceneIndex; i <= endSceneIndex; i++)
                {
                    SceneTransition boundaryTransition = i > startSceneIndex
                        ? SceneRenderHelpers.GetEffectiveTransition(project.Scenes[i - 1], sceneTransition)
                        : SceneTransition.None;
                    if (boundaryTransition != SceneTransition.None)
                    {
                        await RunTransitionAsync(canvas, boundaryTransition, transitionDurationSeconds, exporter, exportFrameRate, cancellationToken);
                        virtualClock += transitionDurationSeconds;
                    }
                    else
                    {
                        canvas.Children.Clear();
                    }

                    ScaleTransform cameraScale = new(1, 1);
                    TranslateTransform cameraTranslate = new(0, 0);
                    canvas.RenderTransform = new TransformGroup { Children = { cameraScale, cameraTranslate } };

                    bool showHand = entranceStyle == EntranceStyle.HandDrawn && handStyle != HandStyle.None;
                    if (showHand)
                    {
                        canvas.Children.Add(hand);
                        Canvas.SetLeft(hand, 0);
                        Canvas.SetTop(hand, 1150);
                        Canvas.SetZIndex(hand, 1);
                        hand.RenderTransform = new MatrixTransform();
                    }

                    SceneModel scene = project.Scenes[i];
                    if (scene == null) continue;

                    bool hasVoiceover = !string.IsNullOrWhiteSpace(scene.VoiceoverPath) && System.IO.File.Exists(scene.VoiceoverPath);
                    sceneStartTimes[i] = virtualClock;
                    if (hasVoiceover)
                        rawVoiceoverCues.Add((scene.VoiceoverPath, virtualClock, scene.VoiceoverTrimStart, scene.VoiceoverTrimEnd, i));

                    double sceneDuration = await RunSceneAsync(canvas, scene, entranceStyle, hand, showHand, strokeBrush, strokeWidth,
                        cameraScale, cameraTranslate, cameraViewportWidth, cameraViewportHeight, exporter, exportFrameRate, cancellationToken);
                    virtualClock += sceneDuration;
                }
                canvas.Children.Remove(hand);

                int holdFrameCount = Math.Max(1, (int)Math.Round(EndHoldSeconds * exportFrameRate));
                for (int f = 0; f < holdFrameCount; f++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    exporter.CaptureFrame();
                    await YieldToUiAsync(canvas);
                }

                foreach (var raw in rawVoiceoverCues)
                {
                    double effectiveTrimEnd = raw.TrimEnd;
                    if (sceneStartTimes.TryGetValue(raw.SceneIndex + 1, out double nextSceneStart))
                    {
                        double capEnd = raw.TrimStart + Math.Max(0, nextSceneStart - raw.Start);
                        if (effectiveTrimEnd <= raw.TrimStart || effectiveTrimEnd > capEnd)
                            effectiveTrimEnd = capEnd;
                    }
                    sceneAudioCues.Add(new SceneAudioCue(raw.Path, raw.Start, raw.TrimStart, effectiveTrimEnd));
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (Logger.LogError(ex, LogAction.LogAndThrow))
                    throw;
            }
            finally
            {
                if (exporter != null)
                    await exporter.StopCapture(progress, cancellationToken);
            }
        }

        // Plays a hard-cut alternative between the outgoing (fully-drawn) scene and the incoming
        // (blank) one: a plain white rectangle whose Opacity (crossfade) or Width (wipe) is
        // computed directly per frame, linearly, from 0 to full over durationSeconds.
        private static async Task RunTransitionAsync(Canvas canvas, SceneTransition transition, double durationSeconds, VideoExporter exporter, int frameRate, CancellationToken cancellationToken)
        {
            if (canvas.Children.Count == 0)
                return;

            Rectangle overlay = new()
            {
                Fill = Brushes.White,
                Width = transition == SceneTransition.Wipe ? 0 : canvas.Width,
                Height = canvas.Height,
                Opacity = transition == SceneTransition.Wipe ? 1 : 0
            };
            Canvas.SetLeft(overlay, 0);
            Canvas.SetTop(overlay, 0);
            Canvas.SetZIndex(overlay, 1000);
            canvas.Children.Add(overlay);

            int frameCount = Math.Max(1, (int)Math.Round(durationSeconds * frameRate));
            for (int f = 0; f < frameCount; f++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                double fraction = durationSeconds > 0 ? Math.Clamp(f / (double)frameRate / durationSeconds, 0, 1) : 1;
                if (transition == SceneTransition.Wipe)
                    overlay.Width = fraction * canvas.Width;
                else
                    overlay.Opacity = fraction;

                canvas.UpdateLayout();
                exporter.CaptureFrame();
                await YieldToUiAsync(canvas);
            }

            canvas.Children.Clear();
        }

        // Builds one scene's full visual timeline (every graphic's stroke/entrance windows, every
        // camera effect's window - see SceneTimelineEngine.BuildScenePlan, shared with
        // PreviewPlaybackHandler) and steps through it frame by frame, evaluating and applying
        // state directly at each step rather than through any WPF animation clock. Returns the
        // scene's real content duration (the same value SceneRenderHelpers.
        // GetEstimatedSceneDurationSeconds approximates from outside).
        private static async Task<double> RunSceneAsync(Canvas canvas, SceneModel scene, EntranceStyle entranceStyle, Image hand, bool showHand,
            Brush strokeBrush, double strokeWidth, ScaleTransform cameraScale, TranslateTransform cameraTranslate,
            double cameraViewportWidth, double cameraViewportHeight, VideoExporter exporter, int frameRate, CancellationToken cancellationToken)
        {
            SceneTimelinePlan plan = SceneTimelineEngine.BuildScenePlan(canvas, scene, entranceStyle, strokeBrush, strokeWidth, cameraViewportWidth, cameraViewportHeight);

            int frameCount = Math.Max(1, (int)Math.Round(plan.Duration * frameRate));
            for (int f = 0; f < frameCount; f++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                double t = f / (double)frameRate;

                foreach (GraphicPlan graphicPlan in plan.GraphicPlans)
                    SceneTimelineEngine.ApplyGraphicState(graphicPlan, t, showHand ? hand : null);
                SceneTimelineEngine.ApplyCameraState(plan.CameraPlans, cameraScale, cameraTranslate, t);

                canvas.UpdateLayout();
                exporter.CaptureFrame();
                await YieldToUiAsync(canvas);
            }

            return plan.Duration;
        }

        // Task.Yield()'s continuation resumes at DispatcherPriority.Normal under WPF's dispatcher
        // - since Normal outranks Render, a tight loop that re-posts itself every frame at that
        // priority starves the window's own repaint (and IProgress<T> reports, also posted to the
        // dispatcher) for as long as the loop keeps running: the queue never drains down to
        // Render/lower while Normal-priority work keeps arriving right behind it. Yielding at
        // Background - below both Render and Normal - guarantees a paint pass and any pending
        // progress report get serviced every single frame instead of only once capture finishes.
        private static Task YieldToUiAsync(Canvas canvas)
        {
            return canvas.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Background).Task;
        }
    }
}
