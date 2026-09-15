using System;

namespace Cryptforge.Core
{
    // Frame times over a window, summarised the way a performance budget reads them: the average and typical frame, the
    // slow tail, the longest frame and how many frames ran past a deadline. It keeps up to its capacity of samples for
    // the percentiles and counts every frame for the rest, and sorts into its own scratch buffer, so summarising a window
    // allocates nothing. Pure, so its arithmetic is tested without a device.
    public sealed class FrameStats
    {
        private readonly float[] _samples;
        private readonly float[] _sorted;

        public int Count { get; private set; }
        public float TotalSeconds { get; private set; }
        public float MaxSeconds { get; private set; }
        public float AverageSeconds => Count > 0 ? TotalSeconds / Count : 0f;

        public FrameStats(int capacity)
        {
            if (capacity < 1)
                throw new ArgumentOutOfRangeException(nameof(capacity));
            _samples = new float[capacity];
            _sorted = new float[capacity];
        }

        // Records one frame's duration; a negative, infinite or missing duration is not a frame and is ignored.
        public void Add(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0f)
                return;

            if (Count < _samples.Length)
                _samples[Count] = seconds;
            Count++;
            TotalSeconds += seconds;
            if (seconds > MaxSeconds)
                MaxSeconds = seconds;
        }

        public void Clear()
        {
            Count = 0;
            TotalSeconds = 0f;
            MaxSeconds = 0f;
        }

        // The recorded frames longer than the deadline, among the kept samples.
        public int CountOver(float seconds)
        {
            int over = 0;
            int kept = Math.Min(Count, _samples.Length);
            for (int i = 0; i < kept; i++)
            {
                if (_samples[i] > seconds)
                    over++;
            }
            return over;
        }

        // The nearest-rank percentile of the kept samples: the shortest frame that at least this fraction of frames do not
        // exceed. 0 when nothing was recorded.
        public float Percentile(float fraction)
        {
            if (float.IsNaN(fraction) || fraction <= 0f || fraction > 1f)
                throw new ArgumentOutOfRangeException(nameof(fraction));

            int kept = Math.Min(Count, _samples.Length);
            if (kept == 0)
                return 0f;
            Array.Copy(_samples, _sorted, kept);
            Array.Sort(_sorted, 0, kept);
            int rank = (int)Math.Ceiling(fraction * kept);
            return _sorted[Math.Max(1, Math.Min(kept, rank)) - 1];
        }
    }
}
