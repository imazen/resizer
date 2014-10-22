using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Imazen.Profiling
{
    public static class ConcurrencyResultExtensions
    {

        public static IEnumerable<double> ToMilliseconds(this IEnumerable<long> ticks)
        {
            return ticks.Select(t => t * 1000.0 / (double)Stopwatch.Frequency);
        }

        public static IEnumerable<Tuple<IConcurrencyResults, SegmentStats>> GetSegmentStatsRecursively(this IConcurrencyResults r)
        {
            return r.Traverse(true).Select(n => new Tuple<IConcurrencyResults, SegmentStats>(n, n.GetStats()));
        }
        public static IConcurrencyResults FindLeastConcurrentSegment(this IConcurrencyResults r)
        {
            return r.GetSegmentStatsRecursively().OrderByDescending(p => 
                        p.Item2.ParallelConcurrencyPercent).First().Item1;
        }
        public static IConcurrencyResults FindSlowestSegment(this IConcurrencyResults r)
        {
            return r.GetSegmentStatsRecursively().OrderByDescending(p =>
                        p.Item2.ParallelExclusiveMs.Avg).First().Item1;
        }

        public static bool HasRuns(this IConcurrencyResults r)
        {
            return r.ParallelRuns.Count() > 0 || r.SequentialRuns.Count() > 0;
        }

        public static IEnumerable<ProfilingResultNode> AllRuns(this IConcurrencyResults r)
        {
            return Enumerable.Concat(r.SequentialRuns, r.ParallelRuns);
        }

        public static double MaxExclusiveMs(this IConcurrencyResults r){
            return r.AllRuns().Max(n => n.TicksExclusiveTotal) * 1000.0 / (double)Stopwatch.Frequency;
        }

        public static Stat<long> Invocations(this IConcurrencyResults r)
        {
            return r.SequentialRuns.Invocations().Combine(r.ParallelRuns.Invocations());
        }
        /// <summary>
        /// Traverses the tree and grabs a set of maching subtrees with the given segment name.
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static IConcurrencyResults FindSet(this IConcurrencyResults r, string segmentName = "op", bool expectOnlyOne = true)
        {
            var results = FindSets(r, segmentName);
            if (results.Count() > 1) throw new ArgumentOutOfRangeException("tree", "More than one matching set found in tree");
            return results.First();
        }



        public static IEnumerable<IConcurrencyResults> FindSets(this IConcurrencyResults tree, string segmentName = "op")
        {
            return tree.TraverseTree(n => n.CollectChildSets(), c => c.SegmentName == segmentName, true);
        }


        /// <summary>
        /// Provides an enumerator which traverses the tree (either depth-first or breadth-first)
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="breadthFirst"></param>
        /// <param name="includeRoot"></param>
        /// <returns></returns>
        public static IEnumerable<IConcurrencyResults> Traverse(this IConcurrencyResults tree, bool breadthFirst)
        {
            return tree.TraverseTree(n => n.CollectChildSets(), null, breadthFirst);
        }


        public static SegmentStats GetStats(this IConcurrencyResults r)
        {
            var activeWallTime = r.ParallelRuns.DeduplicateTime();


            var s = new SegmentStats();
            s.SegmentName = r.SegmentName;
            s.SequentialMs = r.SequentialRuns.InclusiveMs();
            s.SequentialExclusiveMs = r.SequentialRuns.ExclusiveMs();
            s.ParallelMs = r.ParallelRuns.InclusiveMs();
            s.ParallelExclusiveMs = r.ParallelRuns.ExclusiveMs();
            s.ParallelThreads = r.ParallelThreads;
            s.ParallelRealMs = activeWallTime * 1000 / (double)Stopwatch.Frequency;

            return s;
        }

        public static double FastestSequentialMs(this IConcurrencyResults r)
        {
            return r.GetStats().SequentialMs.Min;
        }

        public static double ParallelRealMs(this IConcurrencyResults r)
        {
            return r.GetStats().ParallelRealMs;
        }

        public static SegmentStats StatsForSegment(this IConcurrencyResults r, string segmentName = "op")
        {
            return FindSet(r, segmentName).GetStats();
        }

        public static void Validate(this IConcurrencyResults r)
        {
            r.AllRuns().ValidateSet("Within " + r.SegmentName);
        }



        public static IEnumerable<IConcurrencyResults> CollectChildSets(this IConcurrencyResults r)
        {
            var all = r.AllRuns().ToArray();
            List<IConcurrencyResults> childSets = new List<IConcurrencyResults>();
            if (!all.Any(n => n.HasChildren)) return childSets; //Return empty set when there are no children;
            var setCount = all.Max(n => n.Children.Count());
            for (var i = 0; i < setCount; i++)
            {
                if (all.Any(ins => ins.Children.Count() != setCount)) throw new ArgumentOutOfRangeException("All run trees must have identical structure - mismatched children within segment" + r.SegmentName, "runs");

                var childSeq = r.SequentialRuns.Select(instance => instance.Children[i]).ToArray();
                var childPar = r.ParallelRuns.Select(instance => instance.Children[i]).ToArray();

                var name = (childSeq.FirstOrDefault() ?? childPar.FirstOrDefault()).SegmentName;

                var child = new ConcurrencyResult(childSeq, childPar, r.ParallelThreads, name);
                child.Validate();

                childSets.Add(child);
            }
            return childSets;
        }


        public static string DebugPrintTree(this IConcurrencyResults r)
        {
            var f = new ConcurrencyResultFormatter() { DeltaAbnormalRatio = 100, DeltaSignificantRatio = 0, ExclusiveTimeSignificantMs = 0 };
            var s = f.PrintCallTree(r);
            Debug.Write(s);
            return s;
        }

    }
}
