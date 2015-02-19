// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins.RequestLimiting {
    public enum Scope {
        IPAddress,
        Session,
        All
    }
    public class Limit {
        TimeSpan period;
        Scope scope;
        long maxRequests;

    }
}
