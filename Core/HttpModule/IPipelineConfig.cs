using System;
using System.Collections.Generic;
using System.Text;
using fbs.ImageResizer.Caching;
using fbs.ImageResizer.Configuration;
using System.Collections.Specialized;

namespace fbs.ImageResizer.HttpModule {
    public enum VppUsageOption {
        Fallback, Never, Always
    }
    public interface IPipelineConfig {
        /// <summary>
        /// The fake extension to strip from incoming requests before verifying they are the correct type of request for the pipeline to process.
        /// Should include a leading ".";
        /// </summary>
        string FakeExtension { get; }

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
        /// The key in Context.Items to store the ICacheEventArgs object
        /// </summary>
        string RequestCacheArgsKey { get; }

        /// <summary>
        /// The behavior to use when accessing the file system.
        /// </summary>
        VppUsageOption VppUsage { get; }

        /// <summary>
        /// Returns an ImageBuilder instance to use for image processing.
        /// </summary>
        /// <returns></returns>
        ImageBuilder GetImageBuilder();
        /// <summary>
        /// Returns a caching module for the specified request
        /// </summary>
        /// <param name="context"></param>
        /// <param name="virtualPathAndQuerystring"></param>
        /// <returns></returns>
        ICache GetCachingModule(System.Web.HttpContext context, ICacheEventArgs args);


        void FireRewritingEvents(InterceptModule interceptModule, UrlEventArgs ue);

        void FirePostAuthorize(InterceptModule interceptModule, Configuration.UrlEventArgs urlEventArgs);


        void FirePreProcessRequest(System.Web.HttpContext context, ICacheEventArgs args);

    }
}
