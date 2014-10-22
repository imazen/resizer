using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Imazen.Profiling
{
    public class ConcurrencyResult :IConcurrencyResults
    {
        public ConcurrencyResult(IEnumerable<ProfilingResultNode> sequential, 
                                IEnumerable<ProfilingResultNode> parallel, 
                                int threads, 
            string segmentName)
        {
            this.SequentialRuns = sequential;
            this.ParallelRuns = parallel;
            this.ParallelThreads = threads;
            this.SegmentName = segmentName;
        }
        public IEnumerable<ProfilingResultNode> SequentialRuns { get; private set; }

        public IEnumerable<ProfilingResultNode> ParallelRuns { get; private set; }

        public int ParallelThreads { get; private set; }

        public string SegmentName { get; private set; }

        public override string ToString()
        {
            return String.Format("ConcurrencyResult \"{0}\"", SegmentName);
        }
    }
}
