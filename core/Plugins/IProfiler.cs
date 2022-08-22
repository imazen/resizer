// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

namespace ImageResizer.Plugins
{
    public interface IProfiler
    {
        bool Active { get; }
        void Start(string segmentName, bool allowRecursion = false);
        bool IsRunning(string segmentName);

        void Stop(string segmentName, bool assertRunning = true, bool stopChildren = false);

        void LogStart(long ticks, string segmentName, bool allowRecursion = false);
        void LogStop(long ticks, string segmentName, bool assertRunning = true, bool stopChildren = false);
    }
}