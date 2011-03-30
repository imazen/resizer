using System;
using System.Collections.Generic;
using System.Text;
using fbs.ImageResizer.Caching;
using fbs.ImageResizer.Configuration;
using System.Collections.Specialized;
using System.Web;

namespace fbs.ImageResizer.Configuration {
    public enum VppUsageOption {
        Fallback, Never, Always
    }

    public class UrlEventArgs : EventArgs, IUrlEventArgs {
        protected string _virtualPath;
        protected NameValueCollection _queryString;

        public UrlEventArgs(string virtualPath, NameValueCollection queryString) {
            this._virtualPath = virtualPath;
            this._queryString = queryString;
        }

        public NameValueCollection QueryString {
            get { return _queryString; }
            set { _queryString = value; }
        }
        public string VirtualPath {
            get { return _virtualPath; }
            set { _virtualPath = value; }
        }
    }
    public interface IUrlEventArgs {
        NameValueCollection QueryString { get; set; }
        string VirtualPath { get; set; }
    }

    public delegate void RequestHook(IHttpModule sender, HttpContext context);
    public delegate void UrlRewritingHook(IHttpModule sender, HttpContext context, IUrlEventArgs e);
    public delegate void PreHandleImageHook(IHttpModule sender, HttpContext context, IResponseArgs e);

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
        /// The fake extension to strip from incoming requests before verifying they are the correct type of request for the pipeline to process.
        /// Should include a leading ".";
        /// </summary>
        string FakeExtension { get; }

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
