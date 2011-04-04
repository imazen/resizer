using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace ImageResizer.Caching {
    /// <summary>
    /// Manages updating the cache and serving data from it.
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
