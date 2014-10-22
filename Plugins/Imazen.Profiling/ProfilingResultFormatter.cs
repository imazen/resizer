using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Imazen.Profiling
{

    public class ProfilingResultFormatter
    {
        public ProfilingResultFormatter()
        {
            DeltaAbnormalPercent = 80;
            DeltaSignificantPercent = 5;
            ExclusiveTimeSignificantMs = 5;
            Indentation = "  ";

        }

        /// <summary>
        /// If the delta between executions exceeds this percentage (0..100), min/max values will be displayed instead of just min values.
        /// </summary>
        public double DeltaSignificantPercent { get; set; }

        /// <summary>
        /// When the delta for exclusive time (betwen min/max) exceeds this percent, show all runtimes.
        /// </summary>
        public double DeltaAbnormalPercent { get; set; }

        /// <summary>
        /// Segments with less exclusive time than this will not be rendered
        /// </summary>
        public double ExclusiveTimeSignificantMs { get; set; }

        public string Indentation { get; set; }



        /// <summary>
        /// Returns a anonymous timing string for the given set of nodes (non-recursive)
        /// </summary>
        /// <param name="runs"></param>
        /// <returns></returns>
        public string GetTimingInfo(IEnumerable<ProfilingResultNode> runs)
        {
            runs.ValidateSet("");


            double f = (double)Stopwatch.Frequency / 1000.0;

            var iMin = runs.Min(n => n.TicksInclusiveTotal) / f;
            var iMax = runs.Max(n => n.TicksInclusiveTotal) / f;
            var iDelta = (iMax - iMin) * 100 / iMin;

            var eMin = runs.Min(n => n.TicksExclusiveTotal) / f;
            var eMax = runs.Max(n => n.TicksExclusiveTotal) / f;
            var eDelta = (eMax - eMin) * 100 / eMin;

            var invocations = runs.First().Invocations > 1 ? string.Format(" calls: {0}", runs.First().Invocations) : "";

            if (eDelta > DeltaAbnormalPercent)
            {
                return string.Format("runs exclusive: {1} {0}", invocations, String.Join("  ", runs.Select(n => n.TicksExclusiveTotal * 1000 / Stopwatch.Frequency)));
            }

            string exclusive = eDelta > DeltaSignificantPercent ? string.Format("{0:F} .. {1:F}ms", eMin, eMax) : string.Format("{0:F}ms", eMin);
            string inclusive = iDelta > DeltaSignificantPercent ? string.Format("{0:F} .. {1:F}ms", iMin, iMax) : string.Format("{0:F}ms", iMin);


            if (runs.First().HasChildren)
            {
                return string.Format("inclusive: {0}     exclusive: {1} {2}", inclusive, exclusive, invocations);
            }
            else
            {
                return string.Format("{0} {1}", inclusive, invocations); ;
            }
        }



        public string PrintCallTree(IEnumerable<ProfilingResultNode> runs)
        {
            return PrintStats(runs);
        }

        private string PrintStats(IEnumerable<ProfilingResultNode> runs, string prefix = "")
        {
            if (runs == null || runs.Count() < 1) return null;
            var sb = new StringBuilder();

            if (runs.Max(n => n.TicksExclusiveTotal) >= ExclusiveTimeSignificantMs * Stopwatch.Frequency / 1000.0)
            {
                sb.AppendFormat("{0}{1} {2}\n", prefix, runs.First().SegmentName, GetTimingInfo(runs));
                sb.Append(string.Join("", runs.CollectChildSets().Select(s => PrintStats(s, prefix + Indentation))));
            }
            else
            {
                sb.Append(string.Join("", runs.CollectChildSets().Select(s => PrintStats(s, prefix + runs.First().SegmentName + ">"))));
            }
            return sb.ToString();
        }

    }
}
