// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.Linq;
using Xunit;

namespace Imazen.Profiling.Tests
{
    public class TestProfiler
    {
        [Fact]
        public void TestIsolatedRecursion()
        {
            var a = new Profiler();

            a.Start("op");
            a.Start("a");
            a.Start("b");
            a.Start("wrapper [isolate]");
            ;
            a.Start("a");
            a.Start("b");
            a.Stop("b");
            a.Stop("a");
            a.Stop("wrapper [isolate]");
            a.Stop("b");
            a.Stop("a");
            a.Start("c");
            a.Start("a");
            a.Stop("a");
            a.Stop("c");
            a.Start("d");
            a.Start("d", true);
            a.Stop("d");
            a.Stop("d");

            a.Start("d [drop]");
            a.Start("d", true);
            a.Stop("d");
            a.Stop("d [drop]");

            a.Stop("op");

            var result = a.RootNode.ToProfilingResultNode();
            var r = new[] { result };

            var depthList = string.Join(",", r.Traverse(false).Select(n => n.First().SegmentName));
            Assert.Equal("op,a,b,wrapper,a,b,c,a,d,d", depthList);

            var breadthList = string.Join(",", r.Traverse(true).Select(n => n.First().SegmentName));
            Assert.Equal("op,a,c,d,b,a,d,wrapper,a,b", breadthList);
        }
    }
}