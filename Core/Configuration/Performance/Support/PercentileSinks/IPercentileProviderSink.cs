using System.Collections.Generic;

namespace ImageResizer.Configuration.Performance
{
    internal interface IPercentileProviderSink
    {
        void Report(long value);
        long GetPercentile(float percentile);
        long[] GetPercentiles(IEnumerable<float> percentiles);
    }
}