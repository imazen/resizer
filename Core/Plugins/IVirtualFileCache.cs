using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Hosting;
using System.Collections.Specialized;

namespace ImageResizer.Plugins {
    /// <summary>
    /// Implement this if you are caching files provided by a virtual image provider (For example, remote or s3-hosted images).
    /// </summary>
    public interface IVirtualFileCache {

        IVirtualFile GetFileIfCached(string virtualPath, NameValueCollection queryString, IVirtualFile original);
    }
}
