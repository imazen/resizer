using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins {
    /// <summary>
    /// This interface has nothing to do with output caching. This allows VirtualFile instances to override the default cache key (.VirtualPath) for source caching of VirtualFile instances.
    /// See IVirtualFileCache
    /// </summary>
    public interface IVirtualFileSourceCacheKey {
        string GetCacheKey(bool includeModifiedDate);
    }
}
