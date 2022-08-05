using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ImageResizer.Configuration.Performance
{
    internal class MultiIntervalStats
    {
        private readonly PerIntervalSampling[] set;
        private readonly long[] max;
        private readonly long[] min;
        private readonly long[] total;
        private readonly long[] callbackCount;

        private long recordedTotal;

        public MultiIntervalStats(IReadOnlyList<NamedInterval> intervals) : this(intervals, Stopwatch.GetTimestamp)
        {
        }

        public MultiIntervalStats(IReadOnlyList<NamedInterval> intervals, Func<long> getTimestampNow)
        {
            set = new PerIntervalSampling[intervals.Count];
            max = new long[intervals.Count];
            min = new long[intervals.Count];
            total = new long[intervals.Count];
            callbackCount = new long[intervals.Count];
            for (var i = 0; i < intervals.Count; i++)
            {
                var index = i;
                set[index] =
                    new PerIntervalSampling(intervals[index], count => OnResult(index, count), getTimestampNow);
            }
        }

        private void OnResult(int intervalIndex, long count)
        {
            Utilities.InterlockedMax(ref max[intervalIndex], count);
            Utilities.InterlockedMin(ref min[intervalIndex], count);
            Interlocked.Add(ref total[intervalIndex], count);
            Interlocked.Increment(ref callbackCount[intervalIndex]);
        }

        public bool Record(long timestamp, long count)
        {
            Interlocked.Add(ref recordedTotal, count);
            var success = true;
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < set.Length; i++)
                if (!set[i].Record(timestamp, count))
                    success = false;
            return success;
        }

        public IEnumerable<IntervalStat> GetStats()
        {
            return set.Select((t, i) => new IntervalStat()
            {
                Interval = t.Interval,
                Min = min[i],
                Max = max[i],
                Avg = callbackCount[i] > 0 ? total[i] / callbackCount[i] : 0
            });
        }

        public long RecordedTotal => Interlocked.Read(ref recordedTotal);
    }
}