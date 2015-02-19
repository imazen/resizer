// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Hosting;
using System.Collections.Specialized;

namespace ImageResizer.Plugins {
    /// <summary>
    /// Implement this if you are caching files provided by a virtual image provider (For example, remote or s3-hosted images).
    /// </summary>
    public interface IVirtualFileCache {
        /// <summary>
        /// Returns a cached copy of virtual file if it is cached, and if caching is desired.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <param name="original"></param>
        /// <returns></returns>
        IVirtualFile GetFileIfCached(string virtualPath, NameValueCollection queryString, IVirtualFile original);
    }
}
