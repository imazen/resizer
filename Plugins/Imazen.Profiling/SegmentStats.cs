using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Imazen.Profiling
{

    public class SegmentStats
    {
        public string SegmentName { get; set; }
        public Stat<double> SequentialMs { get; set; }

        public Stat<double> ParallelMs { get; set; }
        public Stat<double> SequentialExclusiveMs { get; set; }

        public Stat<double> ParallelExclusiveMs { get; set; }

        public int ParallelThreads { get; set; }

        public double ParallelRealMs { get; set; }

        public Stat<double> ParallelLatencyIncreases
        {
            get
            {
                var avgDiff = ParallelMs.Sum / (double)ParallelMs.Count - SequentialMs.Sum / (double)SequentialMs.Count;
                var count = Math.Min(ParallelMs.Count, SequentialMs.Count);
                return new Profiling.Stat<double>(ParallelMs.Min - SequentialMs.Min, ParallelMs.Max - SequentialMs.Max,
                    avgDiff * (double)count, count, "ms", "F");
            }
        }

        /// <summary>
        /// On average, how much longer does each task take to complete in Parallel?
        /// </summary>
        public double ParallelLatencyPercentAvg
        {
            get
            {
                return (ParallelMs.Avg - SequentialMs.Avg) / SequentialMs.Avg * 100;
            }
        }

        public double ParallelThroughputIncreasePercent
        {
            get
            {
                var samplingFactor = ((double)SequentialMs.Count / (double)ParallelMs.Count);
                return (SequentialMs.Sum - ParallelRealMs * samplingFactor) / SequentialMs.Sum * 100;
            }
        }

        /// <summary>
        /// 0% represents serialized execution with no overhead. -15% means parallel is 15% slower than serialized execution.
        /// 100% means parallel executions takes perfect advantage of cores provided with no overhead.
        /// </summary>
        public double ParallelConcurrencyPercent
        {
            get
            {
                var degree = (double)Math.Min(ParallelThreads, Environment.ProcessorCount);
                if (degree < 2 || ParallelMs.Count < degree) return double.NaN;

                //How long does a run take (adjusting for thread/core count)
                var parallelTime = degree * ParallelRealMs / (double)ParallelMs.Count;
                var sequentialTime = SequentialMs.Avg;

                var serialized = sequentialTime * degree;

                return 100 * (serialized - parallelTime) /
                        (serialized - (parallelTime <= serialized ? sequentialTime : 0));
            }
        }


    }

}
