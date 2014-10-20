using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Hosting;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace ImageResizer.Plugins
{
    /// <summary>
    /// Implement this if you are caching files provided by a virtual image provider (For example, remote or s3-hosted images).
    /// </summary>
    public interface IVirtualFileCacheAsync
    {
        /// <summary>
        /// Returns a cached copy of virtual file if it is cached, and if caching is desired.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <param name="original"></param>
        /// <returns></returns>
        Task<IVirtualFileAsync> GetFileIfCachedAsync(string virtualPath, NameValueCollection queryString, IVirtualFileAsync original);
    }
}
