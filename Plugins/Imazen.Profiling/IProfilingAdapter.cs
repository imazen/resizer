using System;
namespace Imazen.Profiling
{
    public interface IProfilingAdapter
    {

        IProfilingAdapter Create(string rootNodeName);
        ProfilingNode RootNode { get; }

        void Start(string segmentName, bool allowRecursion = false);
        void Stop(string segmentName, bool assertStarted = true, bool stopChildren = false);
    }
}
