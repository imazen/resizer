
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Imazen.Profiling
{

    public class ProfilingResultNode
    {
        public ProfilingResultNode(string name, long invocations, long exclusive, long inclusive, long firstStart, long lastStop, IList<ProfilingResultNode> children)
        {
            SegmentName = name;
            TicksExclusiveTotal = exclusive;
            Invocations = invocations;
            TicksInclusiveTotal = inclusive;
            FirstStartAt = firstStart;
            LastStopAt = lastStop;
            Children = children;
        }
        public string SegmentName { get; private set; }


        public long Invocations { get; private set; }
        public long TicksExclusiveTotal { get; private set; }
        public long TicksInclusiveTotal { get; private set; }

        public long FirstStartAt { get; private set; }
        public long LastStopAt { get; private set; }

        public IList<ProfilingResultNode> Children { get; private set; }

        public bool HasChildren
        {
            get { return Children != null && Children.Count() > 0; }
        }

        public IEnumerable<string> GetSegmentNamesRecursive()
        {
            var self =Enumerable.Repeat(SegmentName,1);
            return HasChildren ? Enumerable.Concat(self, Children.SelectMany(n => n.GetSegmentNamesRecursive())) : self;
        }

        public ProfilingResultNode WithStartStopTime(long startTicks, long stopTicks)
        {
            return new ProfilingResultNode(SegmentName, Invocations, TicksExclusiveTotal, TicksInclusiveTotal, startTicks, stopTicks, Children.ToList());
        }
        //TODO - add revalidation of inclusive/exclusive times
    }

}
