// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration;
using System.Web;

namespace ImageResizer.Plugins {
    /// <summary>
    /// Allows multi-tenancy support. The 'root' config only needs one plugin, which implements this interface.
    /// </summary>
    public interface ICurrentConfigProvider {
        /// <summary>
        /// Returns a Config instance appropriate for the current request. If null is returned, the default/root instance will be used.
        /// Implementations MUST return the same instance of Config for two identical requests. Multiple Config instances per tenant/area will cause problems.
        /// MUST be thread-safe, concurrent calls WILL occur, and WILL occur during initial call.
        /// </summary>
        /// <returns></returns>
        Config GetCurrentConfig();
    }
}
