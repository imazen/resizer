// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Specialized;
using System.Threading.Tasks;
using System.Web;
using ImageResizer.Caching;
using ImageResizer.Plugins;

namespace ImageResizer.Configuration
{
    public enum VppUsageOption
    {
        Fallback,
        Never,
        Always
    }


    public delegate void RequestEventHandler(IHttpModule sender, HttpContext context);

    public delegate void UrlRewritingEventHandler(IHttpModule sender, HttpContext context, IUrlEventArgs e);

    public delegate void UrlEventHandler(IHttpModule sender, HttpContext context, IUrlEventArgs e);

    public delegate void UrlAuthorizationEventHandler(IHttpModule sender, HttpContext context,
        IUrlAuthorizationEventArgs e);

    public delegate void PreHandleImageEventHandler(IHttpModule sender, HttpContext context, IResponseArgs e);

    public delegate void PreHandleImageAsyncEventHandler(IHttpModule sender, HttpContext context, IAsyncResponsePlan e);

    public delegate void HeartbeatHandler(IPipelineConfig sender, Config c);

    public interface IPipelineConfig : IVirtualImageProvider, IPluginModifiesRequestCacheKey
    {
        /// <summary>
        ///     True if the specified extension is one that the pipeline can handle
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        bool IsAcceptedImageType(string filePath);

        /// <summary>
        ///     True if the querystring contains any directives that are understood by the pipeline
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        bool HasPipelineDirective(NameValueCollection q);

        /// <summary>
        ///     The key in Context.Items to store the modified querystring (i.e, post-rewrite).
        ///     Allows VirtualPathProviders to access the rewritten data.
        /// </summary>
        string ModifiedQueryStringKey { get; }

        /// <summary>
        ///     The key in Context.Items to store the IResponseArgs object
        /// </summary>
        string ResponseArgsKey { get; }

        /// <summary>
        ///     The key in Context.Items to set if we want to cancel MVC routing for the request
        /// </summary>
        string StopRoutingKey { get; }

        /// <summary>
        ///     The key in Context.Items to access a the path to use instead of Request.path
        /// </summary>
        string ModifiedPathKey { get; }

        /// <summary>
        ///     The behavior to use when accessing the file system.
        /// </summary>
        VppUsageOption VppUsage { get; }

        string SkipFileTypeCheckKey { get; }

        /// <summary>
        ///     Get or sets whether the file extension check should be applied to the current request. Defaults to true.
        ///     If set to true, will only affect the current request, and will only cause the Resizer to evaluate the rewriting
        ///     rules on the request.
        ///     Processing may still not occur if no querystring values are specified. Add 'cache=always' to force caching to
        ///     occur.
        /// </summary>
        bool SkipFileTypeCheck { get; }

        /// <summary>
        ///     True once the InterceptModule has been installed.
        /// </summary>
        bool ModuleInstalled { get; set; }

        /// <summary>
        ///     True if we know that InterceptModuleAsync is registered. Null if we don't know.
        /// </summary>
        bool? UsingAsyncMode { get; set; }

        /// <summary>
        ///     Returns the value of Context.Items["resizer.newPath"] if present. If not, returns FilePath + PathInfo.
        ///     Sets Context.Items["resizer.newPath"].
        ///     Only useful during the Pipeline.PostAuthorizeRequestStart event.
        /// </summary>
        string PreRewritePath { get; }


        /// <summary>
        ///     Removes the first fake extension detected at the end of 'path' (like image.jpg.ashx -> image.jpg).
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        string TrimFakeExtensions(string path);

        /// <summary>
        ///     Returns an ImageBuilder instance to use for image processing.
        /// </summary>
        /// <returns></returns>
        ImageBuilder GetImageBuilder();
        
        
        /// <summary>
        /// Returns a mime-type and file extension pair expected to be produced by the given query string and path
        /// </summary>
        /// <param name="commands"></param>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        ImageFileType GuessOutputFileTypeIfSupported(Instructions commands, string virtualPath);


        IAsyncTyrantCache GetAsyncCacheFor(HttpContext context, IAsyncResponsePlan plan);

        /// <summary>
        ///     Returns an IVirtualFile instance if the specified file exists.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        new IVirtualFile GetFile(string virtualPath, NameValueCollection queryString);

        /// <summary>
        ///     Returns an IVirtualFileAsync instance if the specified file can be provided by an async provider
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        Task<IVirtualFileAsync> GetFileAsync(string virtualPath, NameValueCollection queryString);

        /// <summary>
        ///     Returns true if (a) A registered IVirtualImageProvider says it exists, or (b) if the VirtualPathProvider chain says
        ///     it exists.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        new bool FileExists(string virtualPath, NameValueCollection queryString);

        /// <summary>
        ///     Returns true if any registered IVirtualImageProviderAsync says it exists.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        Task<bool> FileExistsAsync(string virtualPath, NameValueCollection queryString);


        /// <summary>
        ///     If true, AuthorizeImage will be called for all image requests, not just those with command directives.
        /// </summary>
        bool AuthorizeAllImages { get; set; }


        void FirePostAuthorizeRequest(IHttpModule sender, HttpContext httpContext);

        void FireRewritingEvents(IHttpModule sender, HttpContext context, IUrlEventArgs ue);

        void FireAuthorizeImage(IHttpModule sender, HttpContext context, IUrlAuthorizationEventArgs urlEventArgs);

        void FirePreHandleImage(IHttpModule sender, HttpContext context, IResponseArgs e);

        void FirePreHandleImageAsync(IHttpModule sender, HttpContext context, IAsyncResponsePlan e);

        void FireImageMissing(IHttpModule sender, HttpContext context, IUrlEventArgs urlEventArgs);

        void FireHeartbeat();

        NameValueCollection ModifiedQueryString { get; set; }

        bool IsAppDomainUnrestricted();

        string DropQuerystringKeys { get; set; }
    }
}