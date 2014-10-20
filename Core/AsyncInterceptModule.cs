using ImageResizer.Caching;
using ImageResizer.Configuration;
using ImageResizer.Encoding;
using ImageResizer.Plugins;
using ImageResizer.Plugins.Basic;
using ImageResizer.Util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Security;
using ImageResizer.ExtensionMethods;
using System.Globalization;

namespace ImageResizer
{
    public class AsyncInterceptModule : IHttpModule
    {

        public class AsyncResponsePlan:IAsyncResponsePlan{

            public string EstimatedContentType { get; set; }

            public string EstimatedFileExtension { get; set; }

            public string RequestCachingKey { get; set; }

            public NameValueCollection RewrittenQuerystring { get; set; }

            public ReadStreamAsyncDelegate OpenSourceStreamAsync{get;set;}

            public WriteResultAsyncDelegate CreateAndWriteResultAsync{get;set;}
        }
        public void Dispose()
        {
            
        }

        public void Init(HttpApplication context)
        {
            var helper = new EventHandlerTaskAsyncHelper(CheckRequest_PostAuthorizeRequest_Async);
            context.AddOnPostAuthorizeRequestAsync(helper.BeginEventHandler, helper.EndEventHandler);
            conf.ModuleInstalled = true;
        }

        protected IPipelineConfig conf { get { return Config.Current.Pipeline; } }


        protected async Task CheckRequest_PostAuthorizeRequest_Async(object sender, EventArgs e)
        {
            //Skip requests if the Request object isn't populated
            HttpApplication app = sender as HttpApplication;
            if (app == null) return;
            if (app.Context == null) return;
            if (app.Context.Request == null) return;

            conf.FirePostAuthorizeRequest(this, app.Context);



            //Allow handlers of the above event to change filePath/pathinfo so we can successfull test the extension
            string originalPath = conf.PreRewritePath;

            //Trim fake extensions so IsAcceptedImageType will work properly
            string filePath = conf.TrimFakeExtensions(originalPath);


            //Is this an image request? Checks the file extension for .jpg, .png, .tiff, etc.
            if (conf.SkipFileTypeCheck || conf.IsAcceptedImageType(filePath))
            {
                //Copy the querystring so we can mod it to death without messing up other stuff.
                NameValueCollection q = conf.ModifiedQueryString;

                //Call URL rewriting events
                UrlEventArgs ue = new UrlEventArgs(filePath, q);
                conf.FireRewritingEvents(this, app.Context, ue);

                //Pull data back out of event object, resolving app-relative paths
                string virtualPath = PathUtils.ResolveAppRelativeAssumeAppRelative(ue.VirtualPath);
                q = ue.QueryString;

                //Store the modified querystring in request for use by VirtualPathProviders
                conf.ModifiedQueryString = q; // app.Context.Items["modifiedQueryString"] = q;

                //See if resizing is wanted (i.e. one of the querystring commands is present).
                //Called after processPath so processPath can add them if needed.
                //Checks for thumnail, format, width, height, maxwidth, maxheight and a lot more
                if (conf.HasPipelineDirective(q) || conf.AuthorizeAllImages)
                {
                    //Who's the user
                    IPrincipal user = app.Context.User as IPrincipal;

                    // no user (must be anonymous...).. Or authentication doesn't work for this suffix. Whatever, just avoid a nullref in the UrlAuthorizationModule
                    if (user == null)
                        user = new GenericPrincipal(new GenericIdentity(string.Empty, string.Empty), new string[0]);

                    //Do we have permission to call UrlAuthorizationModule.CheckUrlAccessForPrincipal?
                    bool canCheckUrl = System.Security.SecurityManager.IsGranted(new System.Security.Permissions.SecurityPermission(System.Security.Permissions.PermissionState.Unrestricted));


                    //Run the rewritten path past the auth system again, using the result as the default "AllowAccess" value
                    bool isAllowed = true;
                    if (canCheckUrl && !app.Context.SkipAuthorization) try
                        {
                            isAllowed = UrlAuthorizationModule.CheckUrlAccessForPrincipal(virtualPath, user, "GET");
                        }
                        catch (NotImplementedException) { } //For MONO support


                    IUrlAuthorizationEventArgs authEvent = new UrlAuthorizationEventArgs(virtualPath, new NameValueCollection(q), isAllowed);

                    //Allow user code to deny access, but not modify the url or querystring.
                    conf.FireAuthorizeImage(this, app.Context, authEvent);

                    if (!authEvent.AllowAccess) throw new ImageProcessingException(403, "Access denied", "Access denied");
                }

                if (conf.HasPipelineDirective(q))
                {
                    
                    //Does the file exist physically? (false if VppUsage=always or file is missing)
                    bool existsPhysically = (conf.VppUsage != VppUsageOption.Always);
                    if (existsPhysically){
                        existsPhysically = File.Exists(HostingEnvironment.MapPath(virtualPath));
                   }

                    //If not present physically (and VppUsage!=never), try to get the virtual file. Null indicates a missing file
                    IVirtualFileAsync vf = null;
                    
                    if (conf.VppUsage != VppUsageOption.Never && !existsPhysically){
                        vf = await conf.GetFileAsync(virtualPath,q);
                    }
                    
                    //Only process files that exist
                    if (existsPhysically || vf != null)
                    {
                        try
                        {
                            await HandleRequest(app.Context, virtualPath, q, vf);
                            //Catch not found exceptions
                        }
                        catch (System.IO.FileNotFoundException notFound)
                        { //Some VPPs are optimisitic , or could be a race condition
                            FileMissing(app.Context, virtualPath, q);
                            throw new ImageMissingException("The specified resource could not be located", "File not found", notFound);
                        }
                        catch (System.IO.DirectoryNotFoundException notFound)
                        {
                            FileMissing(app.Context, virtualPath, q);
                            throw new ImageMissingException("The specified resource could not be located", "File not found", notFound);
                        }
                    }
                    else
                        FileMissing(app.Context, virtualPath, q);

                }
            }
        }


        /// <summary>
        /// Generates the resized image to disk (if needed), then rewrites the request to that location.
        /// Perform 404 checking before calling this method. Assumes file exists.
        /// Called during PostAuthorizeRequest
        /// </summary>
        /// <param name="context"></param>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <param name="vf"></param>
        protected virtual async Task HandleRequest(HttpContext context, string virtualPath, NameValueCollection queryString, IVirtualFileAsync vf)
        {
            Stopwatch s = new Stopwatch();
            s.Start();


            ResizeSettings settings = new ResizeSettings(queryString);

            bool isCaching = settings.Cache == ServerCacheMode.Always;
            bool isProcessing = settings.Process == ProcessWhen.Always;

            //By default, we process it if is both (a) a recognized image extension, and (b) has a resizing directive (not just 'cache').
            if (settings.Process == ProcessWhen.Default)
            {
                //Check for resize directive by removing ('non-resizing' items from the current querystring) 
                NameValueCollection copy = new NameValueCollection(queryString);
                copy.Remove("cache"); copy.Remove("process"); copy.Remove("useresizingpipeline"); copy.Remove("404");
                copy.Remove("404.filterMode"); copy.Remove("404.except");
                //If the 'copy' still has directives, and it's an image request, then let's process it.
                isProcessing = conf.IsAcceptedImageType(virtualPath) && conf.HasPipelineDirective(copy);
            }

            //By default, we only cache it if we're processing it. 
            if (settings.Cache == ServerCacheMode.Default && isProcessing)
                isCaching = true;

            //Resolve the 'cache' setting to 'no' unless we want it cache.
            if (!isCaching) settings.Cache = ServerCacheMode.No;


            //If we are neither processing nor caching, don't do anything more with the request
            if (!isProcessing && !isCaching) return;
            context.Items[conf.ResponseArgsKey] = ""; //We are handling the requests

            //Communicate to the MVC plugin this request should not be affected by the UrlRoutingModule.
            context.Items[conf.StopRoutingKey] = true;


            IEncoder guessedEncoder = null;
            //Only use an encoder to determine extension/mime-type when it's an image extension or when we've set process = always.
            if (isProcessing)
            {
                guessedEncoder = conf.GetImageBuilder().EncoderProvider.GetEncoder(settings, virtualPath);
                if (guessedEncoder == null) throw new ImageProcessingException("Image Resizer: No image encoder was found for the request.");
            }

            //Determine the file extenson for the caching system to use if we aren't processing the image
            //Use the exsiting one if is an image extension. If not, use "unknown". 
            // We don't want to suggest writing .exe or .aspx files to the cache! 
            string fallbackExtension = PathUtils.GetFullExtension(virtualPath).TrimStart('.');
            if (!conf.IsAcceptedImageType(virtualPath)) fallbackExtension = "unknown";

            //Determine the mime-type if we aren't processing the image.
            string fallbackContentType = "application/octet-stream";
            //Support jpeg, png, gif, bmp, tiff mime-types. Otherwise use "application/octet-stream". 
            //We can't set it to null - it will default to text/html
            System.Drawing.Imaging.ImageFormat recognizedExtension = DefaultEncoder.GetImageFormatFromExtension(fallbackExtension);
            if (recognizedExtension != null) fallbackContentType = DefaultEncoder.GetContentTypeFromImageFormat(recognizedExtension);


            //Build CacheEventArgs
            var e = new AsyncResponsePlan();
            e.RequestCachingKey = virtualPath + PathUtils.BuildQueryString(queryString);

            //Add the modified date to the request key, if present.
            var modDate = (vf == null) ? System.IO.File.GetLastWriteTimeUtc(HostingEnvironment.MapPath(virtualPath)) :
                (vf is IVirtualFileWithModifiedDateAsync ? await ((IVirtualFileWithModifiedDateAsync)vf).GetModifiedDateUTCAsync() : DateTime.MinValue);

            if (modDate != DateTime.MinValue && modDate != DateTime.MaxValue)
            {
                e.RequestCachingKey += "|" + modDate.Ticks.ToString(NumberFormatInfo.InvariantInfo);
            };


            e.RewrittenQuerystring = settings;
            e.EstimatedContentType = isProcessing ? guessedEncoder.MimeType : fallbackContentType;
            e.EstimatedFileExtension = isProcessing ? guessedEncoder.Extension : fallbackExtension;


            //A delegate for accessing the source file
            e.OpenSourceStreamAsync = async delegate()
            {
                return (vf != null) ? await vf.OpenAsync() : File.Open(HostingEnvironment.MapPath(virtualPath), FileMode.Open, FileAccess.Read, FileShare.Read);
            };

            //Add delegate for writing the data stream
            e.CreateAndWriteResultAsync = async delegate(System.IO.Stream stream, IAsyncResponsePlan plan)
            {
                //This runs on a cache miss or cache invalid. This delegate is preventing from running in more
                //than one thread at a time for the specified cache key
                try
                {
                    if (!isProcessing)
                    {
                        //Just duplicate the data
                        using (Stream source = await e.OpenSourceStreamAsync())
                            await source.CopyToAsync(stream); //4KiB buffer

                    }
                    else
                    {   
                        var j = new ImageJob();
                        j.Instructions = new Instructions(settings);
                        j.SourcePathData = vf != null ? vf.VirtualPath : virtualPath;

                        MemoryStream inBuffer = null;
                        MemoryStream outBuffer = new MemoryStream(32 * 1024);
                        using(var sourceStream = vf != null ? await vf.OpenAsync() : File.Open(HostingEnvironment.MapPath(virtualPath), FileMode.Open, FileAccess.Read, FileShare.Read)){
                            inBuffer = new MemoryStream(sourceStream.CanSeek ? (int)sourceStream.Length : 128 * 1024);
                            await sourceStream.CopyToAsync(inBuffer);
                        }
                        inBuffer.Seek(0, SeekOrigin.Begin);

                        j.Source = inBuffer;
                        j.Dest = outBuffer;
                        
                        await Task.Run( delegate(){ conf.GetImageBuilder().Build(j);});
                        outBuffer.Seek(0,  SeekOrigin.Begin);
                        await outBuffer.CopyToAsync(stream);
                    }
                    //Catch not found exceptions
                }
                catch (System.IO.FileNotFoundException notFound)
                {
                    //This will be called later, if at all. 
                    FileMissing(context, virtualPath, queryString);
                    throw new ImageMissingException("The specified resource could not be located", "File not found", notFound);
                }
                catch (System.IO.DirectoryNotFoundException notFound)
                {
                    FileMissing(context, virtualPath, queryString);
                    throw new ImageMissingException("The specified resource could not be located", "File not found", notFound);
                }
            };


            //All bad from here down....
            context.Items[conf.ResponseArgsKey] = e; //store in context items

            //Fire events (for client-side caching plugins)
            //conf.FirePreHandleImage(this, context, e);

            //Pass the rest of the work off to the caching module. It will handle rewriting/redirecting and everything. 
            //We handle request headers based on what is found in context.Items
            IAsyncTyrantCache cache = conf.GetAsyncCacheFor(context, e);

            //Verify we have a caching system
            if (cache == null) throw new ImageProcessingException("Image Resizer: No async caching plugin was found for the request");

            await cache.ProcessAsync(context, e);



            s.Stop();
            context.Items["ResizingTime"] = s.ElapsedMilliseconds;

        }


        protected void FileMissing(HttpContext httpContext, string virtualPath, NameValueCollection q)
        {
            //Fire the event (for default image redirection, etc) 
            conf.FireImageMissing(this, httpContext, new UrlEventArgs(virtualPath, new NameValueCollection(q)));

            //Remove the image from context items so we don't try to write response headers.
            httpContext.Items[conf.ResponseArgsKey] = null;
        }
    }
}
