using Imazen.Profiling;
using ImageResizer.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bench
{
    public class JobProfiler:Profiler, IProfiler
    {
        public JobProfiler() { }
        private string rootNodeName;

        public JobProfiler(string rootNodeName):base(rootNodeName){}

        public override IProfilingAdapter Create(string rootNodeName)
        {
            return new JobProfiler(rootNodeName);
        }
    }
}
