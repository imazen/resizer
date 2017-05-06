using System.Collections.Generic;

namespace ImageResizer.Configuration.Performance
{
    class FlatSink : IPercentileProviderSink
    {
        public FlatSink(long MaxValue)
        {

            clamp = new SegmentClamping()
            {
                MinValue = 0,
                MaxValue = MaxValue, //400 megapixels
                Segments = new SegmentPrecision[] {
                    new SegmentPrecision { Above = 0, Loss = 1 }
                }
            };
            clamp.Validate();
        }
        SegmentClamping clamp;
        CountMinSketch<AddMulModHash> table = new CountMinSketch<AddMulModHash>(1279, 4, AddMulModHash.DeterministicDefault());

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
