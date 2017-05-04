using System.Collections.Generic;

namespace ImageResizer.Configuration.Performance
{
    /// <summary>
    /// Lifetime timing percentiles
    /// 
    /// Fixed-lifetime timing percentiles
    /// 
    /// Defaults to a 20kb backing table
    /// </summary>
    class TimingsSink: IPercentileProviderSink
    {
        DurationClamping clamp = DurationClamping.Default600Seconds();
        CountMinSketch<AddMulModHash> table = new CountMinSketch<AddMulModHash>(1279, 4, AddMulModHash.DeterministicDefault());

        public void Report(long ticks)
        {
            table.InterlockedAdd((uint)clamp.ClampStopwatchTicks(ticks), 1);
        }

        public long[] GetPercentiles(IEnumerable<float> percentiles)
        {
            return table.GetPercentiles(percentiles, clamp);
        }

        public long GetPercentile(float percentile)
        {
            return table.GetPercentile(percentile, clamp);
        }

        public string GetTableDebug()
        {
            return table.DebugTable();
        }

    }
}
