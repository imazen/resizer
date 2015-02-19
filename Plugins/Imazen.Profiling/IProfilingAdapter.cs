// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
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
