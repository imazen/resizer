// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
namespace ImageResizer.Configuration {
    public interface IUrlAuthorizationEventArgs {
        bool AllowAccess { get; set; }
        System.Collections.Specialized.NameValueCollection QueryString { get; }
        string VirtualPath { get; }
    }
}
