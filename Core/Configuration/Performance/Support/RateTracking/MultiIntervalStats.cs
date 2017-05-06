using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ImageResizer.Configuration.Performance
{

    class MultiIntervalStats
    {
        PerIntervalSampling[] set;
        NamedInterval[] intervals;
        long[] max;
        long[] min;
        long[] total;
        long[] callbackCount;

        long recordedTotal = 0;

        Func<long> getTimestampNow = Stopwatch.GetTimestamp;
        public MultiIntervalStats(NamedInterval[] intervals): this(intervals, Stopwatch.GetTimestamp) { }

        public MultiIntervalStats(NamedInterval[] intervals, Func<long> getTimestampNow)
        {
            this.getTimestampNow = getTimestampNow;
            this.intervals = intervals;
            set = new PerIntervalSampling[intervals.Length];
            max = new long[intervals.Length];
            min = new long[intervals.Length];
            total = new long[intervals.Length];
            callbackCount = new long[intervals.Length];
            for (var i = 0; i < intervals.Length; i++)
            {
                var index = i;
                set[index] = new PerIntervalSampling(intervals[index], (count) => OnResult(index, count), getTimestampNow);
            }
        }
        void OnResult(int intervalIndex, long count)
        {
            Utilities.InterlockedMax(ref max[intervalIndex], count);
            Utilities.InterlockedMin(ref min[intervalIndex], count);
            Interlocked.Add(ref total[intervalIndex], count);
            Interlocked.Increment(ref callbackCount[intervalIndex]);
        }

        public bool Record(long timestamp, long count)
        {
            Interlocked.Add(ref recordedTotal, count);
            bool success = true;
            for (var i = 0; i < set.Length; i++)
            {
                if (!set[i].Record(timestamp, count))
                {
                    success = false;
                }
            }
            return success;
        }

        public IEnumerable<IntervalStat> GetStats()
        {
            for (var i = 0; i < set.Length; i++)
            {
                yield return new IntervalStat()
                {
                    Interval = set[i].Interval,
                    Min = min[i],
                    Max = max[i],
                    Avg = callbackCount[i] > 0 ? total[i] / callbackCount[i] : 0
                };
            }
        }
        public long RecordedTotal
        {
            get
            {
                return Interlocked.Read(ref recordedTotal);
            }
        }
    }
}
