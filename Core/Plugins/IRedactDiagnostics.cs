// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration.Xml;

namespace ImageResizer.Plugins {
    /// <summary>
    /// Permits plugins to redact data from the diagnostics page, like passwords
    /// </summary>
    public interface IRedactDiagnostics {
         Node RedactFrom(Node resizer);
    }
}
