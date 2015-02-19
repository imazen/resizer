// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
﻿using System;
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
