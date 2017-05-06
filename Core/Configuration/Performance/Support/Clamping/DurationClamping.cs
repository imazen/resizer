using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace ImageResizer.Configuration.Performance
{

    /// <summary>
    /// Deals in microseconds
    /// </summary>
    class DurationClamping : SegmentClamping
    {
        public static long TicksPerMicrosecond { get; } = Stopwatch.Frequency / 1000000;

        public DurationClamping()
        {
            MaxValue = 600 * 1000 * 1000; //600 seconds
        }

        /// <summary>
        /// Creates a 0-600second range with under 760 distinct values
        /// </summary>
        /// <returns></returns>
        public static DurationClamping Default600Seconds()
        {
            var d = new DurationClamping();
            d.MaxValue = 600 * 1000 * 1000;
            d.Segments = new[]
            {
                new SegmentPrecision { Above = 0, Loss = 100}, //0.0-20.0ms (0.1ms) (200 distinct)
                new SegmentPrecision { Above = 20000, Loss = 1000}, //20-200ms (1ms) (180 distinct)
                new SegmentPrecision { Above = 200000, Loss = 10000}, //200-1000ms (10ms) (80 distinct)
                new SegmentPrecision { Above = 1000000, Loss = 50000}, //1000ms-10sec? (50ms) (200 distinct)
                new SegmentPrecision { Above = 10000000, Loss = 1000000}, //10s-100s (1000ms) (90 distinct)
                new SegmentPrecision { Above = 100000000, Loss = 50000000}, //100s-600s? (50s) (10 distinct)
            };
            d.Sort();
            d.Validate();
            return d;
        }

        public long ClampMicroseconds(long microseconds)
        {
            return this.Clamp(microseconds);
        }

        public long ClampStopwatchTicks(long ticks)
        {
            return ClampMicroseconds((ticks + TicksPerMicrosecond - 1) / TicksPerMicrosecond);
        }
    }
    
}
