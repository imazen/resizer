/* Copyright (c) 2011 Nathanael Jones. See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Caching;
using ImageResizer.Configuration;
using System.Collections.Specialized;
using System.Web;

namespace ImageResizer.Configuration {
    public enum VppUsageOption {
        Fallback, Never, Always
    }


    public delegate void RequestEventHandler(IHttpModule sender, HttpContext context);
    public delegate void UrlRewritingEventHandler(IHttpModule sender, HttpContext context, IUrlEventArgs e);
    public delegate void UrlEventHandler(IHttpModule sender, HttpContext context, IUrlEventArgs e);
    public delegate void PreHandleImageEventHandler(IHttpModule sender, HttpContext context, IResponseArgs e);
    public delegate void CacheSelectionHandler(object sender, ICacheSelectionEventArgs e);


    public interface IPipelineConfig {
        /// <summary>
        /// True if the specified extension is one that the pipeline can handle
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        bool IsAcceptedImageType(string filePath);
        /// <summary>
        /// True if the querystring contains any directives that are understood by the pipeline
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        bool HasPipelineDirective(NameValueCollection q);

        /// <summary>
        /// The key in Context.Items to store the modified querystring (i.e, post-rewrite). 
        /// Allows VirtualPathProviders to access the rewritten data.
        /// </summary>
        string ModifiedQueryStringKey { get;  }

        /// <summary>
        /// The key in Context.Items to store the IResponseArgs object
        /// </summary>
        string ResponseArgsKey { get; }

        /// <summary>
        ///  The key in Context.Items to access a the path to use instead of Request.path
        /// </summary>
         string ModifiedPathKey { get; }
        /// <summary>
        /// The behavior to use when accessing the file system.
        /// </summary>
        VppUsageOption VppUsage { get; }


        /// <summary>
        /// Removes the first fake extension detected at the end of 'path' (like image.jpg.ashx -> image.jpg).
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        string TrimFakeExtensions(string path);

        /// <summary>
        /// Returns an ImageBuilder instance to use for image processing.
        /// </summary>
        /// <returns></returns>
        ImageBuilder GetImageBuilder();

        /// <summary>
        /// Returns a ICacheProvider instance that provides caching system selection and creation.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="virtualPathAndQuerystring"></param>
        /// <returns></returns>
        ICacheProvider GetCacheProvider();

        object GetFile(string virtualPath, NameValueCollection queryString);

        /// <summary>
        /// Returns true if (a) A registered IVirtualImageProvider says it exists, or (b) if the VirtualPathProvider chain says it exists.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        bool FileExists(string virtualPath, NameValueCollection queryString);


        void FirePostAuthorizeRequest(IHttpModule sender, System.Web.HttpContext httpContext);

        void FireRewritingEvents(IHttpModule sender, System.Web.HttpContext context, IUrlEventArgs ue);

        void FirePostAuthorizeImage(IHttpModule sender, System.Web.HttpContext context, IUrlEventArgs urlEventArgs);

        void FirePreHandleImage(IHttpModule sender, System.Web.HttpContext context, IResponseArgs e);


        void FireImageMissing(IHttpModule sender, System.Web.HttpContext context, IUrlEventArgs urlEventArgs);

    }
}
