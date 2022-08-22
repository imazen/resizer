// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

namespace ImageResizer.Plugins
{
    /// <summary>
    ///     This interface has nothing to do with output caching. This allows VirtualFile instances to override the default
    ///     cache key (.VirtualPath) for source caching of VirtualFile instances.
    ///     See IVirtualFileCache
    /// </summary>
    public interface IVirtualFileSourceCacheKey
    {
        string GetCacheKey(bool includeModifiedDate);
    }
}