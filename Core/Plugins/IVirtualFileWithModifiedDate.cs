/* Copyright (c) 2011 Nathanael Jones. See license.txt */
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
