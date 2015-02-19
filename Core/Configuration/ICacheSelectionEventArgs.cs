// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿
using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Caching;
using System.Web;

namespace ImageResizer.Configuration {
    public interface ICacheSelectionEventArgs {
        HttpContext Context {get;}
        IResponseArgs ResponseArgs { get; }
        ICache SelectedCache { get; set; }
    }
}
