using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Imazen.Profiling;

namespace Imazen.Profiling.Tests
{
    public class TestProfiler
    {
        [Fact]
        public void TestRecursion()
        {
            var a = new Profiler();

            a.Start("op");
                a.Start("a");
                    a.Start("b");
                        a.Start("a",true);
                            a.Start("b",true);
                             a.Stop("b");
                        a.Stop("a");
                    a.Stop("b");
                a.Stop("a");
                a.Start("c");
                    a.Start("a");
                    a.Stop("a");
                a.Stop("c");
            a.Stop("op");

            var result = a.RootNode.ToProfilingResultNode();
            var r = new ProfilingResultNode[] { result };

            var depthList = string.Join(",",r.Traverse(false).Select(n => n.First().SegmentName));
            Assert.Equal("op,a,b,a,b,c,a", depthList);

            var breadthList = string.Join(",", r.Traverse(true).Select(n => n.First().SegmentName));
            Assert.Equal("op,a,c,b,a,a,b", breadthList);
        }

        

    }
}
