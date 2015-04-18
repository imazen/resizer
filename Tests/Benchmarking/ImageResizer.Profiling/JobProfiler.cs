// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using Imazen.Profiling;
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
                WaitStart(segmentName, threadsTotal);
            }
            base.Start(segmentName, allowRecursion);
        }

        public override void Stop(string segmentName, bool assertStarted = true, bool stopChildren = false)
        {
            base.Stop(segmentName, assertStarted, stopChildren);

            int threadsTotal = 0;
            if (JoinThreadsAfterSegmentsEnd != null && JoinThreadsAfterSegmentsEnd.TryGetValue(segmentName, out threadsTotal))
            {
                WaitStop(segmentName, threadsTotal);
            }
        }


        private static ConcurrentDictionary<string, Barrier> startBarriers = new ConcurrentDictionary<string, Barrier>(8, 128, StringComparer.Ordinal);
        private static ConcurrentDictionary<string, Barrier> stopBarriers = new ConcurrentDictionary<string, Barrier>(8, 128, StringComparer.Ordinal);
        private void WaitStart(string key, int threads)
        {
            if (threads < 2) return;
            var b = startBarriers.GetOrAdd(key, s => new Barrier(threads));
            b.SignalAndWait();
        }

        private void WaitStop(string key, int threads)
        {
            if (threads < 2) return;
            var b = stopBarriers.GetOrAdd(key, s => new Barrier(threads));
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
