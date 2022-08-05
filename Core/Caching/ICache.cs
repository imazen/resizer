// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Web;

// Contains interfaces for caching plugins, and implementations of IResponseArgs and IResponseHeaders.
namespace ImageResizer.Caching
{
    /// <summary>
    ///     Provides caching behavior
    /// </summary>
    [Obsolete("Use IAsyncTyrantCache")]
    public interface ICache
    {
        /// <summary>
        ///     Returns false if the cache is unable to process the request. If false, the caller should fall back to a different
        ///     cache
        /// </summary>
        /// <param name="current"></param>
        /// <param name="e"></param>
        bool CanProcess(HttpContext current, IResponseArgs e);

        /// <summary>
        ///     Must update the cache if needed, then either rewrite, redirect or serve the cached data.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="e"></param>
        void Process(HttpContext current, IResponseArgs e);
    }
}