using Imazen.Profiling;
using ImageResizer.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace Bench
{
    public class JobProfiler:Profiler, IProfiler
    {
        public JobProfiler() { 

        }
        

        public override void Start(string segmentName, bool allowRecursion = false)
        {
            int threadsTotal = 0;
            if (JoinThreadsBeforeSegmentsStart != null && JoinThreadsBeforeSegmentsStart.TryGetValue(segmentName, out threadsTotal)){
                Wait(segmentName + "_start", threadsTotal);
            }
            base.Start(segmentName, allowRecursion);
        }

        public override void Stop(string segmentName, bool assertStarted = true, bool stopChildren = false)
        {
            base.Stop(segmentName, assertStarted, stopChildren);

            int threadsTotal = 0;
            if (JoinThreadsAfterSegmentsEnd != null && JoinThreadsAfterSegmentsEnd.TryGetValue(segmentName, out threadsTotal))
            {
                Wait(segmentName + "_stop", threadsTotal);
            }
        }


        private static ConcurrentDictionary<string, Barrier> barriers = new ConcurrentDictionary<string, Barrier>(StringComparer.Ordinal);
        private void Wait(string key, int threads)
        {
            if (threads == 1) return;
            var b = barriers.GetOrAdd(key, s => new Barrier(threads));
            b.SignalAndWait();
        }

        public JobProfiler JoinThreadsAroundSegment(string name, int threadCount)
        {
            if (JoinThreadsBeforeSegmentsStart == null)
                JoinThreadsBeforeSegmentsStart = new Dictionary<string, int>(StringComparer.Ordinal);

            JoinThreadsBeforeSegmentsStart.Add(name,threadCount);
            if (JoinThreadsAfterSegmentsEnd == null)
                JoinThreadsAfterSegmentsEnd = new Dictionary<string, int>(StringComparer.Ordinal);

            JoinThreadsAfterSegmentsEnd.Add(name, threadCount);
            return this;
        }

        private Dictionary<string, int> JoinThreadsBeforeSegmentsStart;

        private Dictionary<string,int> JoinThreadsAfterSegmentsEnd;

        public JobProfiler(string rootNodeName):base(rootNodeName){}

        public override IProfilingAdapter Create(string rootNodeName)
        {
            return new JobProfiler(rootNodeName);
        }
    }
}
