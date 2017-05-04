using System;
using System.Diagnostics;

namespace ImageResizer.Configuration.Performance
{
    struct NamedInterval
    {
        public string Name { get; set; }
        public string Unit { get; set; }
        public long TicksDuration { get; set; }
    }

    /// <summary>
    /// 4 overlapping windows are used
    /// </summary>
    class PerIntervalSampling
    {
        CircularTimeBuffer[] rings;
        long[] offsets;
        const int ringCount = 4;
        Action<long> resultCallback;
        Func<long> getTimestampNow;

        public NamedInterval Interval { get; private set; }
        long intervalTicks = 0;

        public PerIntervalSampling(NamedInterval interval, Action<long> resultCallback, Func<long> getTimestampNow)
        {
            this.Interval = interval;
            this.intervalTicks = interval.TicksDuration;
            this.getTimestampNow = getTimestampNow;

            offsets = new long[ringCount];
            rings = new CircularTimeBuffer[ringCount];

            this.resultCallback = resultCallback;
            // 3 seconds minimum to permit delayed reporting
            var buckets = (int)Math.Max(2, Stopwatch.Frequency * 3 / intervalTicks);
            for (var ix = 0; ix < ringCount; ix++)
            {
                var offset = (long)Math.Round(ix * 0.1 * intervalTicks);
                offsets[ix] = offset;
                rings[ix] = new CircularTimeBuffer(intervalTicks, buckets);
            }
        }

        public void FireCallbackEvents()
        {
            for (var ix = 0; ix < ringCount; ix++)
            {
                TimeSlotResult result;
                do
                {
                    result = rings[ix].DequeueResult();
                    if (result.IsEmpty)
                    {
                        break;
                    }
                    else
                    {
                        resultCallback(result.Value.Value);
                    }
                } while (true);
            }
        }

        public bool Record(long timestamp, long count)
        {
            if (timestamp - intervalTicks > this.getTimestampNow())
            {
                return false; //Too far future, would break current values 
            }

            bool success = true;
            for (var ix = 0; ix < ringCount; ix++)
            {
                if (!rings[ix].Record(timestamp + offsets[ix], count))
                {
                    success = false;
                }
            }
            FireCallbackEvents();
            return success;
        }
    }

}
