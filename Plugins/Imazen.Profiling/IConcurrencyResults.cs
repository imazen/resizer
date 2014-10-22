using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Imazen.Profiling
{
    public interface IConcurrencyResults
    {

        string SegmentName { get; }
        IEnumerable<ProfilingResultNode> SequentialRuns { get; }

        IEnumerable<ProfilingResultNode> ParallelRuns { get; }
        int ParallelThreads { get;  }

    }
}
