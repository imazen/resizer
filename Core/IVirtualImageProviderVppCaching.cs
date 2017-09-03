// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Caching;

namespace ImageResizer.Plugins
{
    public interface IVirtualImageProviderVppCaching:IVirtualImageProviderVpp
    {
        CacheDependency VppGetCacheDependency(string virtualPath,
          IEnumerable virtualPathDependencies,
          DateTime utcStart);
        string VppGetFileHash(string virtualPath, IEnumerable virtualPathDependencies);
    }
}
