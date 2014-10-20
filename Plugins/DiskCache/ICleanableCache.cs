using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Plugins.DiskCache
{
    public interface ICleanableCache
    {
         event CacheResultHandler CacheResultReturned; 
         CacheIndex Index { get; }
         string PhysicalCachePath { get;  }

         ILockProvider Locks { get;}
    }
}
