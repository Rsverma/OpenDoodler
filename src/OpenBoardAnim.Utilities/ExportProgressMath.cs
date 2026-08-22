using System;
using System.Collections.Generic;
using System.Globalization;

namespace OpenBoardAnim.Utilities
{
    // One captured frame's file name and how long it should hold on screen in the exported
    // video - see ExportProgressMath.BuildFrameDurations.
    public readonly record struct FrameDurationEntry(string FrameName, double Duration);

    // Pure export-progress/timing math pulled out of VideoExporter so it can be unit tested
    // without a live WPF Canvas/CompositionTarget. Every formula here is moved verbatim from
    // VideoExporter - no behavior change.
    public static class ExportProgressMath
    {
        // CompositionTarget.Rendering fires at the display's own refresh rate (60/120/144Hz+),
        // not at frameRate - this gates capture to roughly one frame per 1/frameRate seconds of
        // elapsed capture time so a high-refresh monitor doesn't render/encode/write far more
        // frames than the export video will ever use.
        public static bool ShouldCaptureFrame(double elapsedSeconds, double lastCapturedSeconds, int frameRate)
            => elapsedSeconds - lastCapturedSeconds >= 1.0 / frameRate;

        // Capped below 70 - actual progress can outrun the rough estimate; 70-80 is reserved for
        // flushing any not-yet-written frame backlog to disk and 80-100 for the encoding phase.
        public static double CapturePercentage(int frameCount, int estimatedTotalFrames)
            => Math.Min(70, frameCount / (double)estimatedTotalFrames * 70);

        public static double DrainPercentage(int framesWritten, int frameCount)
            => Math.Min(80, 70 + (framesWritten / (double)frameCount) * 10);

        public static double EncodePercentage(double elapsedSeconds, double videoDurationSeconds)
            => videoDurationSeconds > 0
                ? Math.Clamp(80 + elapsedSeconds / videoDurationSeconds * 20, 80, 99)
                : 85;

        // Input-level trim (-ss/-t), which must precede the -i it applies to in a multi-input
        // ffmpeg command. trimEnd of 0 (or not past trimStart) means "no explicit end - keep
        // whatever -ss already gave us, through the source's natural end".
        public static string BuildTrimArgs(double trimStart, double trimEnd)
        {
            string args = "";
            if (trimStart > 0)
                args += $"-ss {trimStart.ToString(CultureInfo.InvariantCulture)} ";
            if (trimEnd > trimStart)
                args += $"-t {(trimEnd - trimStart).ToString(CultureInfo.InvariantCulture)} ";
            return args;
        }

        // Gives each captured frame its own real duration (the gap to the next frame's actual
        // timestamp), instead of assuming every frame is spaced by a fixed 1/framerate interval -
        // that's what lets the video's internal timing track wall-clock time exactly.
        public static List<FrameDurationEntry> BuildFrameDurations(IReadOnlyList<double> frameTimestamps, double totalElapsedSeconds)
        {
            List<FrameDurationEntry> entries = new(frameTimestamps.Count);
            for (int i = 0; i < frameTimestamps.Count; i++)
            {
                double duration = i + 1 < frameTimestamps.Count
                    ? frameTimestamps[i + 1] - frameTimestamps[i]
                    : Math.Max(0.001, totalElapsedSeconds - frameTimestamps[i]);
                entries.Add(new FrameDurationEntry($"frame_{i:D4}.bmp", duration));
            }
            return entries;
        }

        // The concat demuxer ignores the last entry's own duration line, so without repeating the
        // final frame's name once more, it would flash for ~0 seconds instead of holding for its
        // share of the capture - the standard workaround.
        public static string LastFrameName(int frameCount) => $"frame_{frameCount - 1:D4}.bmp";
    }
}
