using NAudio.Wave;
using OpenBoardAnim.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace OpenBoardAnim.Utils
{
    // Decodes an audio file into a small array of per-bucket peak amplitudes (0..1) for
    // waveform rendering, off the UI thread. Keyed by file path and cached for the process
    // lifetime - the same voiceover/background-music file is asked for repeatedly every time
    // the timeline recomputes its segments.
    public static class WaveformCache
    {
        private const int PeakCount = 2000;

        private static readonly ConcurrentDictionary<string, Task<float[]>> Cache = new();

        public static Task<float[]> GetPeaksAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
                return Task.FromResult(Array.Empty<float>());

            return Cache.GetOrAdd(path, p => Task.Run(() => ComputePeaks(p)));
        }

        private static float[] ComputePeaks(string path)
        {
            try
            {
                using WaveStream reader = Path.GetExtension(path).Equals(".wav", StringComparison.OrdinalIgnoreCase)
                    ? new WaveFileReader(path)
                    : new MediaFoundationReader(path);

                ISampleProvider sampleProvider = reader.ToSampleProvider();
                int channels = sampleProvider.WaveFormat.Channels;
                long totalMonoSamples = reader.WaveFormat.BlockAlign > 0 ? reader.Length / reader.WaveFormat.BlockAlign : 0;
                if (totalMonoSamples <= 0)
                    return Array.Empty<float>();

                long samplesPerBucket = Math.Max(1, totalMonoSamples / PeakCount);
                List<float> peaks = new(PeakCount + 1);
                float[] buffer = new float[channels * 4096];
                long samplesInCurrentBucket = 0;
                float currentBucketPeak = 0f;

                int read;
                while ((read = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < read; i += channels)
                    {
                        float sample = 0f;
                        for (int c = 0; c < channels && i + c < read; c++)
                            sample = Math.Max(sample, Math.Abs(buffer[i + c]));

                        currentBucketPeak = Math.Max(currentBucketPeak, sample);
                        samplesInCurrentBucket++;

                        if (samplesInCurrentBucket >= samplesPerBucket)
                        {
                            peaks.Add(currentBucketPeak);
                            currentBucketPeak = 0f;
                            samplesInCurrentBucket = 0;
                        }
                    }
                }
                if (samplesInCurrentBucket > 0)
                    peaks.Add(currentBucketPeak);

                return peaks.ToArray();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to decode waveform for '{path}': {ex.Message}");
                return Array.Empty<float>();
            }
        }
    }
}
