/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins {
    /// <summary>
    /// For virtual files who want to provide their data in Bitmap form (like a PSD reader or gradient generator). Plugins should never assume this interface will be used, .Open() must also be implemented.
    /// </summary>
    public interface IVirtualBitmapFile:IVirtualFile {
        /// <summary>
        /// Returns a Bitmap instance of the file's contents
        /// </summary>
        /// <returns></returns>
        System.Drawing.Bitmap GetBitmap();
    }
}
