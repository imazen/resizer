using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Imazen.Profiling
{
    public class ConcurrencyResultFormatter
    {
        public ConcurrencyResultFormatter()
        {
            DeltaAbnormalRatio = .80;
            DeltaSignificantRatio = .5;
            ExclusiveTimeSignificantMs = 5;
            Indentation = "  ";

        }

        /// <summary>
        /// If the delta between executions exceeds this percentage (0..1), min/max values will be displayed instead of just min values.
        /// </summary>
        public double DeltaSignificantRatio { get; set; }

        /// <summary>
        /// When the delta for exclusive time (betwen min/max) exceeds this percent, show all runtimes.
        /// </summary>
        public double DeltaAbnormalRatio { get; set; }

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
        public string GetTimingInfo(IConcurrencyResults r)
        {

            var sincl = r.SequentialRuns.InclusiveMs();
            var sexcl = r.SequentialRuns.ExclusiveMs();
            var pincl = r.ParallelRuns.InclusiveMs();
            var pexcl = r.ParallelRuns.ExclusiveMs();
            var suffix = string.Format(" {0:F}%||", r.GetStats().ParallelConcurrencyPercent);

            if (r.Invocations().Max > 1) suffix += string.Format(" calls: {0}", r.Invocations().ToString(0));

            if (sexcl.DeltaRatio > DeltaAbnormalRatio)
            {
                var runTimes = r.AllRuns().Select(n => n.TicksExclusiveTotal).ToMilliseconds().Select(m => Math.Round(m));
                return string.Format("runs exclusive: {1} {0}", suffix, String.Join("  ", runTimes ));
            } else if (r.SequentialRuns.First().HasChildren)
            {
                return string.Format("exclusive: {0} ||{1}||   inclusive: {2} ||{3} {4}",
                    sexcl.ToString(DeltaSignificantRatio),
                    pexcl.ToString(DeltaSignificantRatio),
                    sincl.ToString(DeltaSignificantRatio),
                    pincl.ToString(DeltaSignificantRatio), suffix);
            }
            else
            {
                return string.Format("{0} ||{1} {2}", sincl.ToString(DeltaSignificantRatio), pincl.ToString(DeltaSignificantRatio), suffix); ;
            }
        }



        public string PrintCallTree(IConcurrencyResults r)
        {
            return PrintStats(r);
        }

        private string PrintStats(IConcurrencyResults r, string prefix = "")
        {
            if (r == null || !r.HasRuns()) return null;
            var sb = new StringBuilder();

            if (r.MaxExclusiveMs() >= ExclusiveTimeSignificantMs)
            {
                sb.AppendFormat("{0}{1} {2}\n", prefix, r.SegmentName, GetTimingInfo(r));
                sb.Append(string.Join("", r.CollectChildSets().Select(s => PrintStats(s, prefix + Indentation))));
            }
            else
            {
                sb.Append(string.Join("", r.CollectChildSets().Select(s => PrintStats(s, prefix + r.SegmentName + ">"))));
            }
            return sb.ToString();
        }

    }
}
