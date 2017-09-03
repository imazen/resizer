// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins {
    /// <summary>
    /// For plugins that add support for new source file image extensions.
    /// </summary>
    public interface IFileExtensionPlugin {
        /// <summary>
        /// If the plugin adds support for new file extensions (such as "psd"), they should be returned by this method.
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetSupportedFileExtensions();
    }
}
