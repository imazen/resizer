// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;

namespace Imazen.Profiling
{
    public interface IConcurrencyResults
    {
        string SegmentName { get; }
        IEnumerable<ProfilingResultNode> SequentialRuns { get; }

        IEnumerable<ProfilingResultNode> ParallelRuns { get; }
        int ParallelThreads { get; }
    }
}