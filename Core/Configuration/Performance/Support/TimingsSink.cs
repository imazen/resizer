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

        public long[] GetAllValues()
        {
            return table.GetAllValues(clamp);
        }

        public string GetTableDebug()
        {
            return table.DebugTable();
        }

    }

    static class PercentileExtensions
    {
        public static long GetPercentile(this long[] data, float percentile)
        {
            if (data.Length == 0)
            {
                return 0;
            }
            float index = Math.Max(0, percentile * data.Length + 0.5f);

            return (data[(int)Math.Max(0, Math.Ceiling(index - 1.5))] +
                    data[(int)Math.Min(Math.Ceiling(index - 0.5), data.Length - 1)]) / 2;


        }

        //public static long GetPercentile2(this long[] data, float percentile)
        //{
        //    if (data.Length == 0 || percentile <= 0.0f)
        //    {
        //        return 0;
        //    }
        //    // from http://onlinestatbook.com/2/introduction/percentiles.html


        //    float index = percentile * (data.Length + 1);
        //    float fractionalIndex = index - (float)Math.Floor(index);
        //    int integerIndex = (int)Math.Floor(index);
            
        //    long a = data[Math.Max(0, Math.Min(data.Length - 1, integerIndex + 1))];
        //    long b = data[Math.Max(0, Math.Min(data.Length - 1, integerIndex + 2))];

        //    return (long)Math.Round(a + fractionalIndex * (b - a));

        //}
    }
}

