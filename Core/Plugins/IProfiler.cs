using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Plugins
{
    public interface IProfiler
    {
        bool Active{get;}
        void Start(string segmentName, bool allowRecursion = false);
        bool IsRunning(string segmentName);

        void Stop(string segmentName, bool assertRunning = true, bool stopChildren = false);
    }
}
