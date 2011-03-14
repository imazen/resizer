using System;
using System.Collections.Generic;
using System.Text;

namespace fbs.ImageResizer
{
    public interface IVirtualFileWithModifiedDate
    {
         DateTime ModifiedDateUTC { get; }
         string VirtualPath { get; }
    }
    public interface IVirtualBitmapFile
    {
        System.Drawing.Bitmap GetBitmap();
        string VirtualPath { get; }
    }
}
