using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Imazen.Profiling
{
    public class OpBenchmarkResults : IConcurrencyResults
    {
        public int ThrowawayThreads { get; set; }

        public int ParallelThreads { get; set; }


        public IEnumerable<ProfilingResultNode> ThrowawayRuns { get; set; }

        public IEnumerable<ProfilingResultNode> SequentialRuns { get; set; }

        /// <summary>
        /// Includes houskeeping, setup, teardown, GC.Collect() and Thread.Sleep
        /// </summary>
        public long SequentialWallTicks { get; set; }

        public double SequentialWallMs { get {return SequentialWallTicks * 1000 / (double)Stopwatch.Frequency;} }

        public double SequentialHouskeepingMs{get{ return SequentialWallMs - SequentialRuns.ExclusiveMs().Sum;}}

        public IEnumerable<ProfilingResultNode> ParallelRuns { get; set; }

        public long ParallelWallTicks { get; set; }

        public double ParallelWallMs { get {return ParallelWallTicks * 1000 / (double)Stopwatch.Frequency;} }

        public long ParallelUniqueTicks{get;set;}

        public double ParallelUniqueMs { get {return ParallelUniqueTicks * 1000 / (double)Stopwatch.Frequency;} }


        public double ParallelHouskeepingMs{get{ return ParallelWallMs - ParallelUniqueMs;}}


        /// <summary>
        /// Number of virtual cores on the machine
        /// </summary>
        public int CoreCount { get; set; }






        public string SegmentName { get; set; }



        public long PrivateBytesBefore { get; set; }
        public long PrivateBytesWarm { get; set; }
        public long PrivateBytesAfter { get; set; }

        public long ManagedBytesBefore { get; set; }

        public long ManagedBytesWarm { get; set; }
        public long ManagedBytesAfter { get; set; }
    }
}
