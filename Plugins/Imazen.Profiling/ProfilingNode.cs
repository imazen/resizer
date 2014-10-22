using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Imazen.Profiling
{
    public class ProfilingNode
    {

        public long start;
        public long stopped;

        public long TicksExclusive
        {
            get
            {
                return ElapsedTicks - (CompletedChildren != null ? CompletedChildren.Sum((c) => c.TicksInclusive) : 0);
            }
        }

        public long ElapsedTicks
        {
            get
            {
                return stopped - start;
            }
        }
        public long TicksInclusive { get { return ElapsedTicks; } }

        public void Start()
        {
            start = Stopwatch.GetTimestamp();
        }
        public void Stop()
        {
            stopped = Stopwatch.GetTimestamp();
            if (stopped < start) stopped = start;
        }


        public string SegmentName { get; set; }

        public static ProfilingNode StartNew(string name)
        {
            var n = new ProfilingNode();
            n.SegmentName = name;
            n.Start();
            return n;
        }

        public void AddChild(ProfilingNode n)
        {
            if (n == this) throw new InvalidOperationException("You cannot add a parent as a child of itself.");
            CompletedChildren = CompletedChildren ?? new List<ProfilingNode>();
            CompletedChildren.Add(n);
        }

        public bool HasChildren
        {
            get
            {
                return (CompletedChildren != null && CompletedChildren.Count() > 0);
            }
        }

        public List<ProfilingNode> CompletedChildren { get; set; }

        public IEnumerable<ProfilingNode> NonNullChildren
        {
            get
            {
                if (CompletedChildren == null) return Enumerable.Empty<ProfilingNode>();
                else return CompletedChildren.Where(n => n != null);
            }
        }

        public static ProfilingResultNode CollapseInvocations(IEnumerable<ProfilingNode> invocations)
        {
            var names = invocations.Select(n => n.SegmentName).Distinct();
            if (names.Count() > 1) throw new ArgumentException("All invocations must be for the same segment name.", "invocations");
            else if (names.Count() < 1) throw new ArgumentException("One or more invocations is required.", "invocations");


            return new ProfilingResultNode(
                names.First(),
                invocations.Count(),
                invocations.Aggregate<ProfilingNode, long>(0, (d, n) => d + n.TicksExclusive),
                invocations.Aggregate<ProfilingNode, long>(0, (d, n) => d + n.TicksInclusive),
                invocations.Select(n => n.start).Min(),
                invocations.Select(n => n.stopped).Max(),
                invocations.SelectMany(n => n.NonNullChildren).GroupBy(n => n.SegmentName).Select(group => CollapseInvocations(group)).ToList()
                );
        }

        public ProfilingResultNode ToProfilingResultNode()
        {
            return CollapseInvocations(new ProfilingNode[] { this });
        }

        public override string ToString()
        {
            return string.Format("{0} {1}ms ({2})", SegmentName, ElapsedTicks / Stopwatch.Frequency, 
                string.Join(" ", NonNullChildren.Select(c => c.ToString())));
        }
    }

}
