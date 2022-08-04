// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
 using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ImageResizer.Plugins {
    /// <summary>
    /// A virtual file to support IVirtualImageProvider
    /// </summary>
    public interface IVirtualFile {
        /// <summary>
        /// The virtual path of the file (relative to the domain, like /app/folder/file.ext)
        /// </summary>
        string VirtualPath { get; }
        /// <summary>
        /// Returns an opened stream to the file contents.
        /// </summary>
        /// <returns></returns>
        Stream Open();
    }
}
