using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Imazen.Profiling
{
    public static class ProfilingResultExtensions
    {

        public static string PrintCallTree(this IEnumerable<ProfilingResultNode> s)
        {
            return new ProfilingResultFormatter().PrintCallTree(s);
        }
        public static string TimingString(this IEnumerable<ProfilingResultNode> s)
        {
            return new ProfilingResultFormatter().GetTimingInfo(s);
        }

        public static Stat<double> ExclusiveMs(this IEnumerable<ProfilingResultNode> s)
        {
            if (s == null || s.Count() == 0) return ZeroStat();
            return new Stat<double>(s.Min(n => n.TicksExclusiveTotal), s.Max(n => n.TicksExclusiveTotal), s.Sum(n => n.TicksExclusiveTotal), s.Count(), "ms", "F").
                ConvertTo<double>(n => n * 1000 / Stopwatch.Frequency);
        }

        public static Stat<double> InclusiveMs(this IEnumerable<ProfilingResultNode> s)
        {
            if (s == null || s.Count() == 0) return ZeroStat();
            return new Stat<double>(s.Min(n => n.TicksInclusiveTotal), s.Max(n => n.TicksInclusiveTotal), s.Sum(n => n.TicksInclusiveTotal), s.Count(), "ms", "F").
                ConvertTo<double>(n => n * 1000 / Stopwatch.Frequency);
        }

        public static Stat<long> Invocations(this IEnumerable<ProfilingResultNode> s)
        {
            if (s == null || s.Count() == 0) return new Stat<long>(0, 0, 0, 0, "(n/a)", "");
            return new Stat<long>(s.Min(n => n.Invocations), s.Max(n => n.Invocations), s.Sum(n => n.Invocations), s.Count(), "", "");
        }

        public static Stat<double> ZeroStat()
        {
            return new Stat<double>(0, 0, 0, 0, "(n/a)", "F");
        }

        /// <summary>
        /// Return the number of (unique) ticks spent within the given profiling set. (uses inclusive values)
        /// </summary>
        /// <param name="nodeSet"></param>
        /// <returns></returns>
        public static long DeduplicateTime(this IEnumerable<ProfilingResultNode> nodeSet)
        {
            if (nodeSet == null || nodeSet.Count() == 0) return 0;
            var times = nodeSet.Select(n => new Tuple<long, long>(n.FirstStartAt, n.LastStopAt)).ToList();
            times.Add(new Tuple<long, long>(0, 0)); //Add accumulator seed
            times.Sort((a, b) => a.Item1.CompareTo(b.Item1));

            return times.Aggregate((acc, elem) => new Tuple<long, long>(
                acc.Item1 + (elem.Item2 - (acc.Item2 > elem.Item1 ? Math.Min(elem.Item2, acc.Item2) : elem.Item1)),
                Math.Max(acc.Item2, elem.Item2))).Item1;
        }


        /// <summary>
        /// Provides an enumerator which traverses the tree (either depth-first or breadth-first)
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="breadthFirst"></param>
        /// <param name="includeRoot"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<ProfilingResultNode>> Traverse(this IEnumerable<ProfilingResultNode> tree, bool breadthFirst)
        {
            return tree.TraverseTree(n => n.CollectChildSets(), null, breadthFirst);
        }

        public static IEnumerable<T> TraverseTree<T>(this T node, Func<T, IEnumerable<T>> getChildren, Func<T,bool> filter, bool breadthFirst, bool filterAffectsSubtrees = false)
        {
            
            if (breadthFirst)
            {
                var q = new Queue<T>();
                q.Enqueue(node);
                while (q.Count() > 0)
                {
                    var n = q.Dequeue();
                    if (filter == null || filter(n)) yield return n;
                    foreach (var c in getChildren(n))
                        q.Enqueue(c);
                }
            }
            else
            {
                if (filter == null || filter(node)) yield return node;

                foreach (var c in getChildren(node))
                    foreach (var cc in c.TraverseTree(getChildren, null, false, filterAffectsSubtrees))
                        if (filter == null || filter(cc))
                            yield return cc;
            }
            yield break;
        }

        public static IEnumerable<IEnumerable<ProfilingResultNode>> CollectChildSets(this IEnumerable<ProfilingResultNode> runs)
        {
            List<IEnumerable<ProfilingResultNode>> childSets = new List<IEnumerable<ProfilingResultNode>>();
            if (!runs.Any(n => n.HasChildren)) return childSets; //Return empty set when there are no children;
            var setCount = runs.Max(n => n.Children.Count());
            for (var i = 0; i < setCount; i++)
            {
                if (runs.Any(ins => ins.Children.Count() != setCount)) throw new ArgumentOutOfRangeException("All run trees must have identical structure - mismatched children within segment" + runs.First().SegmentName,"runs" );

                var childSet = runs.Select(instance => instance.Children[i]).ToArray();

                ValidateSet (childSet, "Within " + runs.First().SegmentName);

                childSets.Add(childSet);
            }
            return childSets;
        }

        public static void ValidateSet(this IEnumerable<ProfilingResultNode> set, string moreInfo)
        {
            if (set.Count() < 1) return;

            if (set.Select(n => n.SegmentName).Distinct().Count() != 1)
                throw new ArgumentOutOfRangeException("runs", "All run trees must have identical structure (name mismatch). " + moreInfo);

            if (set.Select(n => n.Invocations).Distinct().Count() != 1)
                throw new ArgumentOutOfRangeException("runs", "All run trees must have identical structure (invocation count mismatch). " + moreInfo);

        }

        /// <summary>
        /// Traverses the tree and grabs a set of maching subtrees with the given segment name. Returns null if the segment cannot be found.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IEnumerable<ProfilingResultNode> FindSegment(this IEnumerable<ProfilingResultNode> tree, string name, bool expectOnlyOne = true)
        {
            var results = FindSegments(tree, name);
            if (results.Count() > 1) throw new ArgumentOutOfRangeException("tree", "More than one matching segment found in tree");
            return results.First();
        }

        public static IEnumerable<IEnumerable<ProfilingResultNode>> FindSegments(this IEnumerable<ProfilingResultNode> tree, string name)
        {
            return tree.TraverseTree(n => n.CollectChildSets(), c =>c.Count() > 0 && c.First().SegmentName == name,true);
        }

        public static string DebugPrintTree(this  IEnumerable<ProfilingResultNode> r)
        {
            var f = new ProfilingResultFormatter() { ExclusiveTimeSignificantMs = 0 };
            var s = f.PrintCallTree(r);
            Debug.Write(s);
            return s;
        }
    }
}
