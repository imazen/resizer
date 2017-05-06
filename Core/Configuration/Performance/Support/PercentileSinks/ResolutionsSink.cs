using System.Collections.Generic;

namespace ImageResizer.Configuration.Performance
{
    /// <summary>
    /// 
    /// </summary>
    class ResolutionsSink: IPercentileProviderSink
    {
        public ResolutionsSink()
        {
            clamp = new SegmentClamping()
            {
                MinValue = 0,
                MaxValue = 16000,
                Segments = new SegmentPrecision[]
                {
                    new SegmentPrecision { Above = 0, Loss = 8 },
                    new SegmentPrecision { Above = 600, Loss = 16 },
                    new SegmentPrecision { Above = 3200, Loss = 100 }
                }
            };
            clamp.Sort();
            clamp.Validate();
        }
        SegmentClamping clamp;
        CountMinSketch<AddMulModHash> table = new CountMinSketch<AddMulModHash>(379, 2, AddMulModHash.DeterministicDefault());

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
