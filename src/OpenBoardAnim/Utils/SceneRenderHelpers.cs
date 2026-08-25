using OpenBoardAnim.Models;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace OpenBoardAnim.Utils
{
    // Genuinely shared, timing-independent helpers used by both PreviewPlaybackHandler and
    // ExportRenderHandler (and a few view/viewmodel call sites directly). Deliberately kept
    // separate from those two so changes to preview's or export's animation-timing code never
    // have to touch this file, and this file never has a reason to know about either.
    public static class SceneRenderHelpers
    {
        // Renders a single scene's graphics in their final (fully-drawn) state onto an
        // off-screen Canvas and returns a snapshot bitmap - used for the Launch screen's
        // project thumbnails, which always show scene 1 regardless of whichever scene happens
        // to be open in the editor at save time. Deliberately an off-screen render rather than
        // capturing the live editor canvas: it has zero visible impact (no flicker to a
        // different scene) and works no matter which scene is currently open.
        public static RenderTargetBitmap RenderSceneSnapshot(ProjectDetails project, int sceneIndex)
        {
            if (project?.Scenes == null || sceneIndex < 0 || sceneIndex >= project.Scenes.Count) return null;
            SceneModel scene = project.Scenes[sceneIndex];
            if (scene?.Graphics == null) return null;

            double width = project.Settings?.EditorWidth ?? 0;
            double height = project.Settings?.EditorHeight ?? 0;
            if (width <= 0 || height <= 0) return null;

            Canvas canvas = new() { Width = width, Height = height, Background = Brushes.White };
            foreach (GraphicModelBase graphic in scene.Graphics)
            {
                if (!graphic.IsVisible) continue;
                UIElement element = BuildStaticElement(graphic);
                if (element == null) continue;
                canvas.Children.Add(element);
                Canvas.SetLeft(element, graphic.X);
                Canvas.SetTop(element, graphic.Y);
            }

            canvas.Measure(new Size(width, height));
            canvas.Arrange(new Rect(0, 0, width, height));
            canvas.UpdateLayout();

            RenderTargetBitmap bitmap = new((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(canvas);
            bitmap.Freeze();
            return bitmap;
        }

        // Builds the settled (non-hand-drawn-stroke) visual for a text graphic - shared by both
        // handlers' entrance-animation branch and BuildStaticElement below, since all three need
        // the exact same per-block bold/italic rendering. TextModel.TextGeometry (used for the
        // hand-drawn stroke animation itself) already bakes the same per-run formatting in via
        // GeometryHelper.ConvertTextToGeometry, so this only needs to match it for the TextBlock
        // shown once strokes finish (or immediately, for Fade In / Pop In entrance styles).
        public static TextBlock BuildTextBlock(TextModel text)
        {
            TextDecorationCollection baseDecorations = GeometryHelper.BuildDecorations(text.IsUnderline, text.IsStrikethrough);
            TextBlock textBlock = new()
            {
                Foreground = text.SelectedColor,
                FontFamily = text.SelectedFontFamily,
                FontSize = text.SelectedFontSize,
                FontStyle = text.SelectedFontStyle,
                FontWeight = text.SelectedFontWeight,
                TextDecorations = baseDecorations.Count > 0 ? baseDecorations : null
            };
            string rawText = text.RawText ?? string.Empty;
            if (text.FormatRuns == null || text.FormatRuns.Count == 0)
            {
                textBlock.Text = rawText;
            }
            else
            {
                foreach (TextFormatRun run in text.FormatRuns)
                {
                    if (run.Length <= 0) continue;
                    TextDecorationCollection runDecorations = GeometryHelper.BuildDecorations(run.IsUnderline, run.IsStrikethrough);
                    textBlock.Inlines.Add(new Run(rawText.Substring(run.Start, run.Length))
                    {
                        FontWeight = run.IsBold ? FontWeights.Bold : FontWeights.Normal,
                        FontStyle = run.IsItalic ? FontStyles.Italic : FontStyles.Normal,
                        TextDecorations = runDecorations.Count > 0 ? runDecorations : null
                    });
                }
            }
            return textBlock;
        }

        // The same final-state (non-hand-drawn) visual construction as each handler's
        // entrance-animation branch, factored out separately rather than shared with it - that
        // branch is also responsible for building the hand-drawn stroke geometry and driving the
        // entrance Storyboard, neither of which a static snapshot needs at all.
        private static UIElement BuildStaticElement(GraphicModelBase graphic)
        {
            if (graphic is DrawingModel drawing)
            {
                DrawingGroup drawingGroup = drawing.ImgDrawingGroup?.Clone();
                if (drawingGroup == null) return null;
                Rect drawingBounds = drawingGroup.Bounds;
                double drawingScale = drawingBounds.Width > 0 && drawingBounds.Height > 0
                    ? Math.Min(drawing.Width / drawingBounds.Width, drawing.Height / drawingBounds.Height)
                    : 1;
                drawingGroup.Transform = new ScaleTransform(drawingScale, drawingScale);
                return new Image { Source = new DrawingImage(drawingGroup) };
            }
            if (graphic is TextModel text)
            {
                TextBlock element = BuildTextBlock(text);
                Rect textBounds = text.TextGeometry?.Bounds ?? Rect.Empty;
                double textScale = !textBounds.IsEmpty && textBounds.Width > 0 && textBounds.Height > 0
                    ? Math.Min(text.Width / textBounds.Width, text.Height / textBounds.Height)
                    : 1;
                if (textScale != 1)
                    element.RenderTransform = new ScaleTransform(textScale, textScale);
                return element;
            }
            return null;
        }

        // Rough per-scene duration estimate (sum of each visible graphic's Delay + Duration) -
        // used by EditorTimelineViewModel for the timeline's proportional layout and by
        // ProjectPreviewView to position/cap the background-music track for a single-scene
        // preview (hand-drawn stroke timing isn't known ahead of time, so this is "good enough",
        // not a promise).
        public static double GetEstimatedSceneDurationSeconds(SceneModel scene)
        {
            if (scene?.Graphics == null) return 0;
            double graphicsTotal = scene.Graphics.Where(g => g.IsVisible).Sum(g => g.Delay + g.Duration);
            // Camera effects run as a second, concurrent timeline - the scene's real duration is
            // whichever of the two actually runs longer. Effects are keyed by absolute EndTime,
            // so the camera timeline's length is just the latest one.
            double cameraTotal = scene.CameraEffects != null && scene.CameraEffects.Count > 0
                ? scene.CameraEffects.Max(e => e.EndTime)
                : 0;
            return Math.Max(graphicsTotal, cameraTotal);
        }

        // Live playback (preview) has no equivalent to ffmpeg's -t, so a trimmed clip's end is
        // enforced by polling position and pausing once it's reached. Shared with
        // ProjectPreviewView for the background-music track, which needs the same behavior.
        public static DispatcherTimer StartTrimStopTimer(MediaPlayer player, double trimEndSeconds)
        {
            DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(200) };
            timer.Tick += (s, e) =>
            {
                if (player.Position.TotalSeconds >= trimEndSeconds)
                {
                    player.Pause();
                    timer.Stop();
                }
            };
            timer.Start();
            return timer;
        }

        // Resolves a scene's outgoing transition: its own TransitionOverride if set to anything
        // but Inherit, otherwise the project-wide default. Shared by both handlers' playback
        // loops and by EditorTimelineViewModel's timeline marker so the badge shown there always
        // matches what actually plays.
        public static SceneTransition GetEffectiveTransition(SceneModel precedingScene, SceneTransition projectDefault)
        {
            return precedingScene?.TransitionOverride switch
            {
                SceneTransitionOverride.None => SceneTransition.None,
                SceneTransitionOverride.Crossfade => SceneTransition.Crossfade,
                SceneTransitionOverride.Wipe => SceneTransition.Wipe,
                _ => projectDefault
            };
        }
    }
}
