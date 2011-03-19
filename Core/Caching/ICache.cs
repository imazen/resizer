using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace fbs.ImageResizer.Caching {
    /// <summary>
    /// Manages updating the cache and serving data from it.
    /// </summary>
    public interface ICache {

        /// <summary>
        /// Must update the cache if needed, then either rewrite, redirect or serve the cached data.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="e"></param>
        void Process(HttpContext current, CacheEventArgs e);
    }
}
