/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Collections.Specialized;

namespace ImageResizer.Caching {
    
    public delegate void ApplyResponseHeadersDelegate(IResponseHeaders headers, HttpContext context);
    /// <summary>
    /// Allows customization of response headers for a processed image, as well as configuration of the caching system.
    /// </summary>
    public interface IResponseHeaders {
        /// <summary>
        /// The mime-type of the output data. Defaults to null.
        /// </summary>
        string ContentType { get; set; }
        /// <summary>
        /// The cache setting. Defaults to ServerAndPrivate
        /// </summary>
        HttpCacheability CacheControl { get; set; }
        /// <summary>
        /// The UTC time at which the cached data should expire. 
        /// Browsers generally don't re-request resources until the they have expired (unlike modififeddate).
        /// If MinValue, will be ignored.
        /// </summary>
        DateTime Expires { get; set; }
        /// <summary>
        /// The UTC modified date send with the response. Used by browsers with If-Modified-Since to check a cached value is still valid.
        /// If = MinValue, will be ignored.
        /// </summary>
        DateTime LastModified { get; set; }
        /// <summary>
        /// When true: If a client requests a refresh, the response will *still* be served from the server cache.
        /// Defaults to false
        /// </summary>
        bool ValidUntilExpires { get; set; }
        /// <summary>
        /// ASP.Net sometimes sends Vary: * which obliterates caching. Vary is to be avoided anyhow.
        /// Defaults to true
        /// </summary>
        bool SuppressVaryHeader { get; set; }

        /// <summary>
        /// These headers should be applied first, prior to the application of other settings
        /// </summary>
        NameValueCollection DefaultHeaders { get; set; }
        /// <summary>
        /// These headers are applied after applying all of the other settings. (and they will overwrite exisiting values).
        /// </summary>
        NameValueCollection Headers { get; set; }

        /// <summary>
        /// Returns a collection of dependencies used for invalidating the server cache. 
        /// Note, having items here will disable kernel-mode caching. Perhaps it is better to simply use LastModified
        /// </summary>
        /// <returns></returns>
        List<CacheDependency> ServerCacheDependencies { get; set; }

        /// <summary>
        /// A delegate method to apply the values stored in IResponseHeaders to the specified HttpContext.
        /// </summary>
        ApplyResponseHeadersDelegate ApplyToResponse { get; set; }
        /// <summary>
        /// True if the application should automatically execute ApplyToResponse() during the PreSendRequestHeaders event.
        /// </summary>
        bool ApplyDuringPreSendRequestHeaders { get; set; }
    }
}
