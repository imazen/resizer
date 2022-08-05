// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;

namespace ImageResizer.Plugins.RequestLimiting
{
    public enum Scope
    {
        IPAddress,
        Session,
        All
    }

    public class Limit
    {
        private TimeSpan period;
        private Scope scope;
        private long maxRequests;
    }
}