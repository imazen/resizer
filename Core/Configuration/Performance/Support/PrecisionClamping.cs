using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace ImageResizer.Configuration.Performance
{
    struct SegmentPrecision
    {
        /// <summary>
        /// Inclusive (microseconds, 1 millionth of a second)
        /// </summary>
        public long Above { get; set; }
        public long Loss { get; set; }
    }

    class SegmentClamping
    {
        public long MaxPossibleValues { get; set; } = 100000;
        public long MinValue { get; set; } = 0;
        public long MaxValue { get; set; } = long.MaxValue;
        public SegmentPrecision[] Segments { get; set; }

        public void Sort()
        {
            if (this.Segments == null) throw new ArgumentNullException(".Segments field is null. Finish configuration!");

            Segments = Segments.OrderByDescending(p => p.Above).ToArray();
        }

        public void Validate()
        {
            Sort();
            if ((MaxValue % Segments[0].Loss) > 0)
            {
                throw new ArgumentException("MaxValue must be a multiple of the greatest Loss value");
            }
            if ((MinValue < Segments.Last().Above))
            {
                throw new ArgumentException("MinValue must be >= the smallest Above value");
            }
            var count = SegmentsPossibleValuesCountInternal().Sum();
            if (count > MaxPossibleValues)
            {
                throw new ArgumentException("This clamping function produces over " + count + " unique values, which exceeds your configured MaxPossibleValues limt");
            }
            hasValidated = true;
        }
        bool hasValidated = false; 
        public void EnsureValidates()
        {
            if (!hasValidated) Validate();
        }
        public long Clamp(long value)
        {
            EnsureValidates();

            var bounded = Math.Max(Math.Min(value, MaxValue), MinValue);
            for (var ix = 0; ix < Segments.Length; ix++)
            {
                if (bounded >= Segments[ix].Above)
                {
                    var loss = Segments[ix].Loss;
                    return ((bounded + loss - 1) / loss) * loss;
                }
            }
            throw new ArgumentException("Invalid PrecisionClamping state");
        }

        public IEnumerable<long> SegmentsPossibleValuesCount()
        {
            EnsureValidates();

            return SegmentsPossibleValuesCountInternal();
        }
        IEnumerable<long> SegmentsPossibleValuesCountInternal()
        {
            for (var ix = 0; ix < Segments.Length; ix++)
            {
                var stopBefore = ix == 0 ? MaxValue : Segments[ix - 1].Above;
                yield return (stopBefore - Segments[ix].Above) / Segments[ix].Loss;
            }
        }

        //public IEnumerable<long> PossibleValuesCount()
        //{
        //    for (var ix = 0; ix < Segments.Length; ix++)
        //    {
        //        var stopBefore = ix == 0 ? MaxValue : Segments[ix - 1].Above;
        //        yield return (stopBefore - Segments[ix].Above) / Segments[ix].Loss;
        //    }
        //}

        public IEnumerable<long> PossibleValues()
        {
            EnsureValidates();

            for (var ix = 0; ix < Segments.Length; ix++)
            {
                var stopBefore = ix == 0 ? MaxValue : Segments[ix - 1].Above;
                var step = Segments[ix].Loss;
                for (var v = Segments[ix].Above; v < stopBefore; v += step)
                {
                    yield return v;
                }
            }
        }
    }
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

    public class SignificantDigitsClamping
    {
        public long MinValue { get; set; } = 0;
        public long MaxValue { get; set; } = long.MaxValue;

        public int SignificantDigits { get; set; } = 2;
        public SignificantDigitsClamping()
        {

        }

        public double RoundPositiveValueToDigits(double n, int count)
        {
            if (n == 0) return n;
            var mult = Math.Pow(10, count - Math.Floor(Math.Log(n) / Math.Log(10)) - 1);
            return Math.Round(Math.Round(n * mult) / mult);
        }

        public long Clamp(long value)
        {
            var bounded = Math.Max(Math.Min(value, MaxValue), MinValue);
            return (long)RoundPositiveValueToDigits(bounded, SignificantDigits);
        }
    }
    public class SignificantDigitsClampingFloat
    {
        public float MinValue { get; set; } = 0;
        public float MaxValue { get; set; } = float.MaxValue;

        public int SignificantDigits { get; set; } = 2;
        public SignificantDigitsClampingFloat()
        {

        }

        public double RoundPositiveValueToDigits(double n, int count)
        {
            if (n == 0) return n;
            var mult = Math.Pow(10, count - Math.Floor(Math.Log(n) / Math.Log(10)) - 1);
            return Math.Round(Math.Round(n * mult) / mult);
        }

        public float Clamp(float value)
        {
            var bounded = Math.Max(Math.Min(value, MaxValue), MinValue);
            return (float)RoundPositiveValueToDigits(bounded, SignificantDigits);
        }
    }
}
