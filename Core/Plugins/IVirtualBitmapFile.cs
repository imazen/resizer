// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.Drawing;

namespace ImageResizer.Plugins
{
    /// <summary>
    ///     For virtual files who want to provide their data in Bitmap form (like a PSD reader or gradient generator). Plugins
    ///     should never assume this interface will be used, .Open() must also be implemented.
    /// </summary>
    public interface IVirtualBitmapFile : IVirtualFile
    {
        /// <summary>
        ///     Returns a Bitmap instance of the file's contents
        /// </summary>
        /// <returns></returns>
        Bitmap GetBitmap();
    }
}