// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.IO;
using System.Threading.Tasks;

namespace ImageResizer.Plugins
{
    /// <summary>
    ///     A virtual file to support IVirtualImageProvider
    /// </summary>
    public interface IVirtualFileAsync : IVirtualFile
    {
        /// <summary>
        ///     Returns an opened stream to the file contents.
        /// </summary>
        /// <returns></returns>
        Task<Stream> OpenAsync();
    }
}