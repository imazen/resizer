// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Configuration.Issues {
    public enum IssueSeverity {
        /// <summary>
        /// Security and stability issues.
        /// </summary>
        Critical,
        /// <summary>
        /// Behavioral issues, such as having no registered image encoders
        /// </summary>
        Error,
        /// <summary>
        /// Errors in the module configuration
        /// </summary>
        ConfigurationError,
        /// <summary>
        /// Non-optimal settings
        /// </summary>
        Warning

    }
}
