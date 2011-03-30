using System;
using System.Collections.Generic;
using System.Text;

namespace fbs.ImageResizer.Plugins {
    public interface IVirtualBitmapFile {
        System.Drawing.Bitmap GetBitmap();
        string VirtualPath { get; }
    }
}
