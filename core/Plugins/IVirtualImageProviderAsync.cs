// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Specialized;
using System.Threading.Tasks;

namespace ImageResizer.Plugins
{
    /// <summary>
    ///     Implement this to allow ImageResizer to access your custom blob store
    /// </summary>
    public interface IVirtualImageProviderAsync
    {
        /// <summary>
        ///     Returns true if the specified request should be handled by this virtual image provider
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        Task<bool> FileExistsAsync(string virtualPath, NameValueCollection queryString);

        /// <summary>
        ///     Returns a virtual file instance for the specified path and querystring.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        Task<IVirtualFileAsync> GetFileAsync(string virtualPath, NameValueCollection queryString);
    }
}