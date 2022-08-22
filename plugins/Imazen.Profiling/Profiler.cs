// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Imazen.Profiling
{
    public class Profiler : IProfilingAdapter
    {
        public Profiler() : this("op")
        {
        }

        public Profiler(string rootNodeName)
        {
            RootNode = new ProfilingNode() { SegmentName = rootNodeName };
        }

        private Stack<ProfilingNode> callstack = new Stack<ProfilingNode>();

        public ProfilingNode RootNode { get; private set; }

        public bool Active => true;

        public IEnumerable<ProfilingNode> VisibleCallstack
        {
            get
            {
                foreach (var e in callstack)
                {
                    yield return e;
                    if (e.Isolate) yield break;
                }

                yield break;
            }
        }


        public virtual void LogStart(long ticks, string segmentName, bool allowRecursion = false)
        {
            if (!allowRecursion && IsRunning(segmentName))
                throw new InvalidOperationException(string.Format(
                    "The given profiling segment {0} has already been started, and allowRecursion=false", segmentName));

            if (VisibleCallstack.Count() > 0)
            {
                var top = VisibleCallstack.First();
                if (top.StartTicks > ticks)
                    throw new InvalidOperationException(string.Format(
                        "You cannot log a profiling segment {0} {1} that starts before its parent {1} {2} ",
                        segmentName, ticks, top.SegmentName, top.StartTicks));
            }

            callstack.Push(ProfilingNode.StartNewAt(segmentName, ticks));
        }

        public virtual void LogStop(long ticks, string segmentName, bool assertRunning = true,
            bool stopChildren = false)
        {
            StopAt(ticks, segmentName, assertRunning, stopChildren);
        }


        public virtual void Start(string segmentName, bool allowRecursion = false)
        {
            if (!allowRecursion && IsRunning(segmentName))
                throw new InvalidOperationException(string.Format(
                    "The given profiling segment {0} has already been started, and allowRecursion=false", segmentName));

            callstack.Push(ProfilingNode.StartNew(segmentName));
        }

        public bool IsRunning(string segmentName)
        {
            var name = ProfilingNode.NormalizeNodeName(segmentName);
            return VisibleCallstack.Any((n) => n.SegmentName == name);
        }

        public virtual void Stop(string segmentName, bool assertStarted = true, bool stopChildren = false)
        {
            StopAt(-1, segmentName, assertStarted, stopChildren);
        }

        public virtual void StopAt(long ticks, string segmentName, bool assertStarted = true, bool stopChildren = false)
        {
            var normalized_name = ProfilingNode.NormalizeNodeName(segmentName);
            if (stopChildren)
            {
                var topmost = VisibleCallstack.First((n) => n.SegmentName == normalized_name);
                if (topmost != null)
                {
                    var children = VisibleCallstack.TakeWhile((n) => n.SegmentName != normalized_name).ToArray();
                    children.Select((n) =>
                    {
                        StopAt(ticks, n.SegmentName, true, false);
                        return n;
                    });
                    StopAt(ticks, segmentName, assertStarted, false);
                }
                else if (assertStarted)
                {
                    throw new InvalidOperationException(string.Format(
                        "The given profiling segment {0} is not running anywhere in the callstack; it cannot be stopped.",
                        normalized_name));
                }
            }
            else
            {
                if (callstack.Peek().SegmentName == normalized_name)
                {
                    var n = callstack.Pop();
                    n.StopAt(ticks);
                    if (n.Drop) return;
                    if (callstack.Count > 0)
                        callstack.Peek().AddChild(n);
                    else if (RootNode.SegmentName == n.SegmentName && !RootNode.HasChildren)
                        RootNode = n; //Replace the root node on the very first call, *if* the segment name matches.
                    else
                        RootNode.AddChild(n);
                }
                else if (assertStarted)
                {
                    throw new InvalidOperationException(string.Format(
                        "The given profiling segment {0} is not running at the top of the callstack; it cannot be stopped.",
                        normalized_name));
                }
            }
        }

        public virtual IProfilingAdapter Create(string rootNodeName)
        {
            return new Profiler(rootNodeName);
        }
    }
}