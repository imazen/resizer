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


    public delegate void RequestHook(IHttpModule sender, HttpContext context);
    public delegate void UrlRewritingHook(IHttpModule sender, HttpContext context, IUrlEventArgs e);
    public delegate void PreHandleImageHook(IHttpModule sender, HttpContext context, IResponseArgs e);
    public delegate void CacheSelectionDelegate(object sender, ICacheSelectionEventArgs e);


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
        /// The behavior to use when accessing the file system.
        /// </summary>
        VppUsageOption VppUsage { get; }

        /// <summary>
        /// A list of fake extensions to strip from incoming requests before verifying they are the correct type of request for the pipeline to process.
        /// Should include leading periods;
        /// </summary>
        IList<string> FakeExtensions { get; }

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


        void FirePostAuthorizeRequest(IHttpModule sender, System.Web.HttpContext httpContext);

        void FireRewritingEvents(IHttpModule sender, System.Web.HttpContext context, IUrlEventArgs ue);

        void FirePostAuthorizeImage(IHttpModule sender, System.Web.HttpContext context, IUrlEventArgs urlEventArgs);

        void FirePreHandleImage(IHttpModule sender, System.Web.HttpContext context, IResponseArgs e);

    }
}
