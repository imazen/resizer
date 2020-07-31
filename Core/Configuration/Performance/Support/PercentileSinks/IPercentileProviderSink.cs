using System.Collections.Generic;

namespace ImageResizer.Configuration.Performance
{
    interface IPercentileProviderSink
    {
        void Report(long value);
        long GetPercentile(float percentile);
        long[] GetPercentiles(IEnumerable<float> percentiles);
    }
}