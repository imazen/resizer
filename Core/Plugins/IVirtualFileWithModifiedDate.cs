using System;
using System.Collections.Generic;
using System.Text;

namespace fbs.ImageResizer.Plugins
{
    public interface IVirtualFileWithModifiedDate
    {
         DateTime ModifiedDateUTC { get; }
         string VirtualPath { get; }
    }
}
