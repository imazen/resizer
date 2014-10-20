/* Copyright (c) 2014 Imazen See license.txt */
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
