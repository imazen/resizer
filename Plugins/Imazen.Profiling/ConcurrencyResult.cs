// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;

namespace Imazen.Profiling
{
    public class ConcurrencyResult : IConcurrencyResults
    {
        public ConcurrencyResult(IEnumerable<ProfilingResultNode> sequential,
            IEnumerable<ProfilingResultNode> parallel,
            int threads,
            string segmentName)
        {
            SequentialRuns = sequential;
            ParallelRuns = parallel;
            ParallelThreads = threads;
            SegmentName = segmentName;
        }

        public IEnumerable<ProfilingResultNode> SequentialRuns { get; private set; }

        public IEnumerable<ProfilingResultNode> ParallelRuns { get; private set; }

        public int ParallelThreads { get; private set; }

        public string SegmentName { get; private set; }

        public override string ToString()
        {
            return string.Format("ConcurrencyResult \"{0}\"", SegmentName);
        }
    }
}