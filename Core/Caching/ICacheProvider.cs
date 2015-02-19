// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Caching {
    /// <summary>
    /// Provides cache selection logic
    /// </summary>
    public interface ICacheProvider {
        /// <summary>
        /// Selects a caching system for the specified request and response
        /// </summary>
        /// <param name="context"></param>
        /// <param name="responseArgs"></param>
        /// <returns></returns>
        ICache GetCachingSystem(System.Web.HttpContext context, IResponseArgs responseArgs);
    }
}
