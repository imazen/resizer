using System;
using System.Collections.Generic;
using System.Linq;

namespace ImageResizer.Configuration.Performance
{
    internal class SegmentClamping
    {
        public long MaxPossibleValues { get; set; } = 100000;
        public long MinValue { get; set; } = 0;
        public long MaxValue { get; set; } = long.MaxValue;
        public SegmentPrecision[] Segments { get; set; }

        public void Sort()
        {
            if (Segments == null)
                throw new ArgumentNullException("Segments", "Segments field is null. Finish configuration!");

            Segments = Segments.OrderByDescending(p => p.Above).ToArray();
        }

        public void Validate()
        {
            Sort();
            if (MaxValue % Segments[0].Loss > 0)
                throw new ArgumentException("MaxValue must be a multiple of the greatest Loss value");
            if (MinValue < Segments.Last().Above)
                throw new ArgumentException("MinValue must be >= the smallest Above value");
            var count = SegmentsPossibleValuesCountInternal().Sum();
            if (count > MaxPossibleValues)
                throw new ArgumentException("This clamping function produces over " + count +
                                            " unique values, which exceeds your configured MaxPossibleValues limit");
            hasValidated = true;
        }

        private bool hasValidated = false;

        public void EnsureValidates()
        {
            if (!hasValidated) Validate();
        }

        public long Clamp(long value)
        {
            EnsureValidates();

            var bounded = Math.Max(Math.Min(value, MaxValue), MinValue);
            for (var ix = 0; ix < Segments.Length; ix++)
                if (bounded >= Segments[ix].Above)
                {
                    var loss = Segments[ix].Loss;
                    return (bounded + loss - 1) / loss * loss;
                }

            throw new ArgumentException("Invalid PrecisionClamping state");
        }

        public IEnumerable<long> SegmentsPossibleValuesCount()
        {
            EnsureValidates();

            return SegmentsPossibleValuesCountInternal();
        }

        private IEnumerable<long> SegmentsPossibleValuesCountInternal()
        {
            for (var ix = 0; ix < Segments.Length; ix++)
            {
                var stopBefore = ix == 0 ? MaxValue : Segments[ix - 1].Above;
                yield return (stopBefore - Segments[ix].Above) / Segments[ix].Loss;
            }
        }

        public IEnumerable<long> PossibleValues()
        {
            EnsureValidates();

            for (var ix = 0; ix < Segments.Length; ix++)
            {
                var stopBefore = ix == 0 ? MaxValue : Segments[ix - 1].Above;
                var step = Segments[ix].Loss;
                for (var v = Segments[ix].Above; v < stopBefore; v += step) yield return v;
            }
        }
    }
}