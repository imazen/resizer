/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

// Contains interfaces for caching plugins, and implementations of IResponseArgs and IResponseHeadres.
namespace ImageResizer.Caching {
    /// <summary>
    /// Provides caching behavior
    /// </summary>
    public interface ICache {
        /// <summary>
        /// Returns false if the cache is unable to process the request. If false, the caller should fall back to a different cache
        /// </summary>
        /// <param name="current"></param>
        /// <param name="e"></param>
        bool CanProcess(HttpContext current, IResponseArgs e);
        /// <summary>
        /// Must update the cache if needed, then either rewrite, redirect or serve the cached data.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="e"></param>
        void Process(HttpContext current, IResponseArgs e);
    }
}
