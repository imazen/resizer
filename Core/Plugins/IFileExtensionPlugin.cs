/* Copyright (c) 2011 Nathanael Jones. See license.txt */
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
