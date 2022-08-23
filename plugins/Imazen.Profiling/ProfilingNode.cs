// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Imazen.Profiling
{
    public class ProfilingNode
    {
        public long StartTicks { get; set; }
        public long StopTicks { get; set; }

        public long TicksExclusive
        {
            get
            {
                return ElapsedTicks - (CompletedChildren != null ? CompletedChildren.Sum((c) => c.TicksInclusive) : 0);
            }
        }

        public long ElapsedTicks => StopTicks - StartTicks;
        public long TicksInclusive => ElapsedTicks;

        public void Start()
        {
            StartTicks = Stopwatch.GetTimestamp();
        }

        public void Stop()
        {
            StopAt(-1);
        }

        public void StopAt(long ticks)
        {
            StopTicks = ticks > 0 ? ticks : Stopwatch.GetTimestamp();
            if (StopTicks < StartTicks) StopTicks = StartTicks;
        }

        /// <summary>
        ///     Indicates that child profiling operations should be isolated (permits controlled recursion)
        /// </summary>
        public bool Isolate { get; set; }

        /// <summary>
        ///     Indicates that this node and its child nodes should be discarded.
        /// </summary>
        public bool Drop { get; set; }

        public string SegmentName { get; set; }

        public static ProfilingNode StartNew(string name)
        {
            return StartNewAt(name, -1);
        }

        public static ProfilingNode StartNewAt(string name, long ticks)
        {
            var n = new ProfilingNode();

            if (name.IndexOf("[drop]", StringComparison.OrdinalIgnoreCase) > -1)
            {
                n.Drop = true;
                name = name.Replace("[drop]", "");
            }

            if (name.IndexOf("[isolate]", StringComparison.OrdinalIgnoreCase) > -1)
            {
                n.Isolate = true;
                name = name.Replace("[isolate]", "");
            }

            n.SegmentName = name.Trim();
            if (ticks > 0)
                n.StartTicks = ticks;
            else
                n.Start();
            return n;
        }

        public static string NormalizeNodeName(string name)
        {
            if (name.IndexOf("[", StringComparison.OrdinalIgnoreCase) > -1)
                name = name.Replace("[drop]", "").Replace("[isolate]", "").Trim();
            return name;
        }


        public void AddChild(ProfilingNode n)
        {
            if (n == this) throw new InvalidOperationException("You cannot add a parent as a child of itself.");
            CompletedChildren = CompletedChildren ?? new List<ProfilingNode>();
            CompletedChildren.Add(n);
        }

        public bool HasChildren => CompletedChildren != null && CompletedChildren.Count() > 0;

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
            if (names.Count() > 1)
                throw new ArgumentException("All invocations must be for the same segment name.", "invocations");
            else if (names.Count() < 1)
                throw new ArgumentException("One or more invocations is required.", "invocations");


            return new ProfilingResultNode(
                names.First(),
                invocations.Count(),
                invocations.Aggregate<ProfilingNode, long>(0, (d, n) => d + n.TicksExclusive),
                invocations.Aggregate<ProfilingNode, long>(0, (d, n) => d + n.TicksInclusive),
                invocations.Select(n => n.StartTicks).Min(),
                invocations.Select(n => n.StopTicks).Max(),
                invocations.SelectMany(n => n.NonNullChildren).GroupBy(n => n.SegmentName)
                    .Select(group => CollapseInvocations(group)).ToList()
            );
        }

        public ProfilingResultNode ToProfilingResultNode()
        {
            return CollapseInvocations(new[] { this });
        }

        public override string ToString()
        {
            return string.Format("{0} {1}ms ({2})", SegmentName, ElapsedTicks / Stopwatch.Frequency,
                string.Join(" ", NonNullChildren.Select(c => c.ToString())));
        }
    }
}