// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Specialized;

namespace ImageResizer.Configuration
{
    /// <summary>
    /// Used by URL rewriting and authorization event handlers in ImageResizer
    /// </summary>
    public interface IUrlEventArgs
    {
        NameValueCollection QueryString { get; set; }

        /// <summary>
        ///     Domain-relative path which also includes the path info portion. I.e, '/app/folder/file.aspx/path/info' could be a
        ///     valid value. Relative to the domain root, not the site or app root.
        /// </summary>
        string VirtualPath { get; set; }
    }
}