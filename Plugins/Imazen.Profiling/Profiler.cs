using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Imazen.Profiling
{
    public class Profiler : Imazen.Profiling.IProfilingAdapter
    {
        public Profiler() : this("op") { }
        public Profiler(string rootNodeName)
        {
            RootNode = new ProfilingNode() { SegmentName = rootNodeName };
        }

        Stack<ProfilingNode> callstack = new Stack<ProfilingNode>();

        public ProfilingNode RootNode { get; private set; }

        public bool Active
        {
            get { return true; }
        }

        public IEnumerable<ProfilingNode> VisibleCallstack
        {
            get{
                foreach (var e in callstack)
                {
                    yield return e;
                    if (e.Isolate) yield break;
                }
                yield break;
            }
        }

        public virtual void Start(string segmentName, bool allowRecursion = false)
        {
            if (!allowRecursion && IsRunning(segmentName))
                throw new InvalidOperationException(string.Format("The given profiling segment {0} has already been started, and allowRecursion=false", segmentName));

            callstack.Push(ProfilingNode.StartNew(segmentName));
        }

        public bool IsRunning(string segmentName)
        {
            return VisibleCallstack.Any((n) => n.SegmentName == segmentName);
        }

        public virtual void Stop(string segmentName, bool assertStarted = true, bool stopChildren = false)
        {
            if (stopChildren){
                var topmost = VisibleCallstack.First((n) => n.SegmentName == segmentName);
                if (topmost != null)
                {
                    var children = VisibleCallstack.TakeWhile((n) => n.SegmentName != segmentName).ToArray();
                    children.Select((n) => { Stop(n.SegmentName, true, false); return n; });
                    Stop(segmentName, assertStarted, false);
                }else if (assertStarted) throw new InvalidOperationException(string.Format("The given profiling segment {0} is not running anywhere in the callstack; it cannot be stopped.", segmentName));
            
            }else{
                if (callstack.Peek().SegmentName == segmentName){
                    var n = callstack.Pop();
                    n.Stop();
                    if (n.Drop) return; 
                    if (callstack.Count > 0)
                        callstack.Peek().AddChild(n);
                    else if (RootNode.SegmentName == n.SegmentName && !RootNode.HasChildren)
                        RootNode = n; //Replace the root node on the very first call, *if* the segment name matches.
                    else
                        RootNode.AddChild(n);

                }else if (assertStarted) throw new InvalidOperationException(string.Format("The given profiling segment {0} is not running at the top of the callstack; it cannot be stopped.", segmentName));
            }
        }
    
        public virtual IProfilingAdapter Create(string rootNodeName)
        {
            return new Profiler(rootNodeName);
        }
    }

}
