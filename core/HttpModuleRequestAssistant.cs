using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Web;
using System.Web.Hosting;
using ImageResizer.Configuration.Performance;
using ImageResizer.ExtensionMethods;
using ImageResizer.Plugins;
using ImageResizer.Plugins.Basic;
using ImageResizer.Resizing;
using ImageResizer.Util;

namespace ImageResizer.Configuration
{
    public class HttpModuleRequestAssistant
    {
        private HttpContext context;
        private IPipelineConfig conf;
        private IHttpModule sender;

        public HttpModuleRequestAssistant(HttpContext context, IPipelineConfig config, IHttpModule sender)
        {
            conf = config;
            this.sender = sender;
            this.context = context;
        }

        public enum PostAuthorizeResult
        {
            /// <summary>
            ///     If this occurs, no data is available. The request is not for us to interfere with.
            /// </summary>
            UnrelatedFileType,

            /// <summary>
            ///     Authorization was denied. The user should be shown a 403.
            /// </summary>
            AccessDenied403,

            /// <summary>
            ///     Refer to RewrittenQueryHasDirective, ProcessingIndicated, CachingIndicated, RewrittenMappedPath, in order to decide
            ///     what to do.
            /// </summary>
            Complete
        }

        public string RewrittenVirtualPath { get; private set; }

        public NameValueCollection RewrittenQuery { get; private set; }

        public Instructions RewrittenInstructions { get; private set; }

        public string RewrittenMappedPath { get; private set; }
        public bool RewrittenQueryHasDirective { get; private set; }
        public bool ProcessingIndicated { get; private set; }
        public bool CachingIndicated { get; private set; }
        public bool RewrittenVirtualPathIsAcceptedImageType { get; private set; }

        public PostAuthorizeResult PostAuthorize()
        {
            conf.FirePostAuthorizeRequest(sender as IHttpModule, context);

            conf.FireHeartbeat();

            //Allow handlers of the above event to change filePath/pathinfo so we can successfully test the extension
            var originalPath = conf.PreRewritePath;

            //Trim fake extensions so IsAcceptedImageType will work properly
            var filePath = conf.TrimFakeExtensions(originalPath);

            //Is this an image request? Checks the file extension for .jpg, .png, .tiff, etc.
            if (!(conf.SkipFileTypeCheck || conf.IsAcceptedImageType(filePath)))
                return PostAuthorizeResult.UnrelatedFileType;

            //Copy the querystring so we can mod it to death without messing up other stuff.
            var queryCopy = new NameValueCollection(conf.ModifiedQueryString);

            GlobalPerf.Singleton.PreRewriteQuery(queryCopy);

            //Call URL rewriting events
            var ue = new UrlEventArgs(filePath, queryCopy);
            conf.FireRewritingEvents(sender, context, ue);

            //Pull data back out of event object, resolving app-relative paths
            RewrittenVirtualPath = PathUtils.ResolveAppRelativeAssumeAppRelative(ue.VirtualPath);
            RewrittenQuery = ue.QueryString;
            RewrittenInstructions = new Instructions(RewrittenQuery);

            //Store the modified querystring in request for use by VirtualPathProviders during URL Authorization
            conf.ModifiedQueryString = RewrittenQuery;

            RewrittenVirtualPathIsAcceptedImageType = conf.IsAcceptedImageType(RewrittenVirtualPath);
            RewrittenQueryHasDirective = conf.HasPipelineDirective(RewrittenQuery);
            RewrittenMappedPath = HostingEnvironment.MapPath(RewrittenVirtualPath);

            ProcessingIndicated = false;
            CachingIndicated = false;

            if (RewrittenQueryHasDirective)
            {
                //By default, we process it if is both (a) a recognized image extension, and (b) has a resizing directive (not just 'cache').
                //Check for resize directive by removing ('non-resizing' items from the current querystring) 
                ProcessingIndicated = RewrittenInstructions.Process == ProcessWhen.Always ||
                                      ((RewrittenInstructions.Process == ProcessWhen.Default ||
                                        RewrittenInstructions.Process == null) &&
                                       RewrittenVirtualPathIsAcceptedImageType &&
                                       conf.HasPipelineDirective(RewrittenInstructions.Exclude("cache", "process",
                                           "useresizingpipeline", "404", "404.filterMode", "404.except")));

                CachingIndicated = RewrittenInstructions.Cache == ServerCacheMode.Always ||
                                   ((RewrittenInstructions.Cache == ServerCacheMode.Default ||
                                     RewrittenInstructions.Cache == null) && ProcessingIndicated);

                //Resolve the 'cache' setting to 'no' unless we want it cache. TODO: Understand this better
                if (!CachingIndicated) RewrittenInstructions.Cache = ServerCacheMode.No;

                GlobalPerf.Singleton.QueryRewrittenWithDirective(RewrittenVirtualPath);
            }


            if (RewrittenQueryHasDirective || conf.AuthorizeAllImages)
            {
                var authEvent = new UrlAuthorizationEventArgs(RewrittenVirtualPath,
                    new NameValueCollection(RewrittenQuery), true);

                //Allow user code to deny access, but not modify the URL or querystring.
                conf.FireAuthorizeImage(sender, context, authEvent);

                if (!authEvent.AllowAccess) return PostAuthorizeResult.AccessDenied403;
            }

            return PostAuthorizeResult.Complete;
        }

        public string GenerateRequestCachingKey(DateTime? modifiedData)
        {
            var modified =
                modifiedData != null && modifiedData != DateTime.MinValue && modifiedData != DateTime.MaxValue
                    ? modifiedData.Value.Ticks.ToString(NumberFormatInfo.InvariantInfo)
                    : "";
            var baseKey = $"{RewrittenVirtualPath}{PathUtils.BuildQueryString(RewrittenQuery)}|{modified}";

            return conf.ModifyRequestCacheKey(baseKey, RewrittenVirtualPath, RewrittenQuery);
        }


        public string EstimatedContentType { get; private set; }
        public string EstimatedFileExtension { get; private set; }
        public IVirtualFileAsync VirtualFile { get; set; }
        public bool IsVirtualFile => VirtualFile != null;

        public void EstimateResponseInfo()
        {
           
            ImageFileType guessedOutputType = null;
            //Only use an encoder to determine extension/mime-type when it's an image extension or when we've set process = always.
            if (ProcessingIndicated)
            {
                guessedOutputType = conf.GuessOutputFileTypeIfSupported(RewrittenInstructions, RewrittenVirtualPath);
                if (guessedOutputType == null)
                {

                    var guessedEncoder = conf.GetImageBuilder()
                        .EncoderProvider.GetEncoder(new ResizeSettings(RewrittenInstructions), RewrittenVirtualPath);
                    if (guessedEncoder == null)
                        throw new ImageProcessingException(
                            "Image Resizer: No image encoder was found for the request.");
                    guessedOutputType = new ImageFileType(){ MimeType = guessedEncoder.MimeType, Extension = guessedEncoder.Extension };
                }
            }

            //Determine the file extension for the caching system to use if we aren't processing the image
            //Use the existing one if is an image extension. If not, use "unknown".
            // We don't want to suggest writing .exe or .aspx files to the cache! 
            var fallbackExtension = PathUtils.GetFullExtension(RewrittenVirtualPath).TrimStart('.');
            if (!conf.IsAcceptedImageType(RewrittenVirtualPath)) fallbackExtension = "unknown";

            //Determine the mime-type if we aren't processing the image.
            var fallbackContentType = "application/octet-stream";
            //Support JPEG, PNG, GIF, BMP, TIFF mime-types. Otherwise use "application/octet-stream".
            //We can't set it to null - it will default to text/html
            var recognizedExtension = DefaultEncoder.GetImageFormatFromExtension(fallbackExtension);
            if (recognizedExtension != null)
                fallbackContentType = DefaultEncoder.GetContentTypeFromImageFormat(recognizedExtension);


            EstimatedContentType = ProcessingIndicated ? guessedOutputType.MimeType : fallbackContentType;
            EstimatedFileExtension = ProcessingIndicated ? guessedOutputType.Extension : fallbackExtension;

            GlobalPerf.Singleton.IncrementCounter("module_response_ext_" + EstimatedFileExtension);
        }

        public void FireMissing()
        {
            //Fire the event (for default image redirection, etc) 
            conf.FireImageMissing(sender, context,
                new UrlEventArgs(RewrittenVirtualPath, new NameValueCollection(RewrittenQuery)));

            //Remove the image from context items so we don't try to write response headers.
            context.Items[conf.ResponseArgsKey] = null;
            GlobalPerf.Singleton.IncrementCounter("postauth_404_");
        }

        private IHttpHandler CreateSFH()
        {
            var type = typeof(HttpApplication).Assembly.GetType("System.Web.StaticFileHandler", true);
            return (IHttpHandler)Activator.CreateInstance(type, true);
        }

        public void ApplyRewrittenPath()
        {
            var currentPath = context.Request.FilePath + context.Request.PathInfo;
            if (RewrittenVirtualPath != currentPath)
                context.RewritePath(RewrittenVirtualPath +
                                    PathUtils.BuildQueryString(
                                        RewrittenQuery)); //Apply the new querystring also, or it would be lost
        }

        public void AssignSFH()
        {
            context.RemapHandler(CreateSFH());
        }

        internal void FireAccessDenied()
        {
            GlobalPerf.Singleton.IncrementCounter("postauthjob_403");
        }

        internal void FireJobSuccess()
        {
            GlobalPerf.Singleton.IncrementCounter("postauthjob_ok");
        }

        internal void FirePostAuthorizeSuccess()
        {
            GlobalPerf.Singleton.IncrementCounter("postauth_ok");
        }


        internal void FirePostAuthorizeRequestException(Exception ex)
        {
            GlobalPerf.Singleton.IncrementCounter("postauth_errors_" + ex.GetType().Name);
            GlobalPerf.Singleton.IncrementCounter("postauth_errors");
        }

        internal void FireJobException(Exception ex)
        {
            GlobalPerf.Singleton.IncrementCounter("postauthjob_errors_" + ex.GetType().Name);
            GlobalPerf.Singleton.IncrementCounter("postauthjob_errors");
        }
    }
}