using System.Collections.Generic;

namespace ImageResizer.Configuration.Performance
{
    internal class PixelCountSink : IPercentileProviderSink
    {
        public PixelCountSink()
        {
            clamp = new SegmentClamping()
            {
                MinValue = 0,
                MaxValue = 1000000 * 400, //400 megapixels
                Segments = new[]
                {
                    new SegmentPrecision { Above = 0, Loss = 100000 }, // 0.1mp up to 8mp (80)
                    new SegmentPrecision { Above = 8000000, Loss = 500000 }, // 0.5mp up to 40mp (64)
                    new SegmentPrecision { Above = 40000000, Loss = 5000000 } //5mp (~100)
                }
            };
            clamp.Sort();
            clamp.Validate();
        }

        private readonly SegmentClamping clamp;

        private readonly CountMinSketch<AddMulModHash> table =
            new CountMinSketch<AddMulModHash>(379, 3, AddMulModHash.DeterministicDefault());

        public void Report(long value)
        {
            table.InterlockedAdd((uint)clamp.Clamp(value), 1);
        }

        public long[] GetPercentiles(IEnumerable<float> percentiles)
        {
            return table.GetPercentiles(percentiles, clamp);
        }

        public long GetPercentile(float percentile)
        {
            return table.GetPercentile(percentile, clamp);
        }
    }
}