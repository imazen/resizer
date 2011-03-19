using System;
using System.Collections.Generic;
using System.Text;

namespace fbs.ImageResizer {
    public interface IVirtualBitmapFile {
        System.Drawing.Bitmap GetBitmap();
        string VirtualPath { get; }
    }
}
