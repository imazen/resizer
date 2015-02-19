// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;

namespace ImageResizer.Plugins.CustomOverlay {
    public interface IOverlayProvider:IQuerystringPlugin {
        /// <summary>
        /// Provides a collection of Overlay objects for the specified request parameters
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        IEnumerable<Overlay> GetOverlays(string virtualPath, NameValueCollection query);
    }
}
