using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Hosting;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace ImageResizer.Plugins
{
    /// <summary>
    /// Implement this to allow your class (or VirtualPathProvider subclass) to be used without registering it with the whole ASP.NET system.
    /// </summary>
    public interface IVirtualImageProviderAsync
    {
        /// <summary>
        /// Returns true if the specified request should be handled by this virtual image provider
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        Task<bool> FileExistsAsync(string virtualPath, NameValueCollection queryString);
        /// <summary>
        /// Returns a virtual file instance for the specified path and querystring.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        Task<IVirtualFileAsync> GetFileAsync(string virtualPath, NameValueCollection queryString);
    }
}
