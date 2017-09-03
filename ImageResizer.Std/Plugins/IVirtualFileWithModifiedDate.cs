// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.
﻿
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins
{
    /// <summary>
    /// Always implement this if possible. Allows caching systems to detect changes to source files and invalidate cached data properly.
    /// </summary>
    public interface IVirtualFileWithModifiedDate :IVirtualFile
    {
        /// <summary>
        /// The modified (last write time) of the source file, in UTC form. 
        /// </summary>
         DateTime ModifiedDateUTC { get; }
    }
}
