using System;
using System.Collections.Generic;
using System.Text;

namespace fbs.ImageResizer
{
    public interface IVirtualFileWithModifiedDate
    {
         DateTime ModifiedDateUTC { get; }
  
    }
}
