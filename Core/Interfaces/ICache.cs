using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace fbs.ImageResizer {
    public interface ICache {
        /// <summary>
        /// Must update the cache if needed, then either rewrite, redirect or stream the cached value.
        /// </summary>
        /// <param name="current"></param>
        /// <param name="e"></param>
        void Process(HttpContext current, CacheEventArgs e);
    }
}
