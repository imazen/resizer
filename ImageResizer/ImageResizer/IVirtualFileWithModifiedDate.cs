/**
 * Written by Nathanael Jones 
 * http://nathanaeljones.com
 * nathanael.jones@gmail.com
 * 
 * This is a discontinued version of the Image Resizer, and is being replaced by Version 3.
 * Visit http://imageresizing.net/ to download version 3.
 * Complete migration docs are available at: http://imageresizing.net/docs/2to3/
 */

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
