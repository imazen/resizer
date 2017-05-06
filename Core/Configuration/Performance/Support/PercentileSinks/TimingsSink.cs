using System;
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
    class TimingsSink : IPercentileProviderSink
    {
        DurationClamping clamp = DurationClamping.Default600Seconds();
        CountMinSketch<AddMulModHash> table = new CountMinSketch<AddMulModHash>(1279, 4, AddMulModHash.DeterministicDefault());

        public void Report(long ticks)
        {
            table.InterlockedAdd((uint)clamp.ClampStopwatchTicksToMicroseconds(ticks), 1);
        }

        public void ReportMicroseconds(long microseconds)
        {
            table.InterlockedAdd((uint)clamp.ClampMicroseconds(microseconds), 1);
        }

        public long[] GetPercentiles(IEnumerable<float> percentiles)
        {
            return table.GetPercentiles(percentiles, clamp);
        }

        public long GetPercentile(float percentile)
        {
            return table.GetPercentile(percentile, clamp);
        }

        public long[] GetAllValues()
        {
            return table.GetAllValues(clamp);
        }

        public string GetTableDebug()
        {
            return table.DebugTable();
        }

    }
}

