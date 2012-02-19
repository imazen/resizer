using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Collections.Specialized;
using ImageResizer.Plugins;
using ImageResizer.Configuration;
using System.Web;
using ImageResizer.Util;
using System.Security.Principal;
using System.Web.Security;
using System.Web.Hosting;
using System.Diagnostics;
using ImageResizer.Plugins.Basic;
using ImageResizer.Caching;
using System.IO;
using ImageResizer.Encoding;

namespace ImageResizer.Mvc {
    /// <summary>
    /// This action result is NOT a replacement for the InterceptModule and MvcRoutingShim. It cannot hope to achieve the same performance, compatibility, or flexibility.
    /// It does not support all the plugins, nor all the Pipeline events. 
    /// It does not support the CloudFront, Image404, or ImageHandlerSyntax plugins.  RemoteReader, SqlReader, S3Reader, , Image404, ClientCache, 
    /// It doesn't even work yet.
    /// </summary>
    internal class ImageRequestAction: ActionResult {

        public ImageRequestAction(string virtualPath, NameValueCollection query, IPipelineConfig conf = null) {
            this.conf = (conf == null) ? Config.Current.Pipeline : conf;
            ReauthorizeFinalPath = true;
            Source = virtualPath;
            Settings = new ResizeSettings(query);
        }

        protected IPipelineConfig conf = null;
        /// <summary>
        /// If true, and if the app has Full Trust, the UrlAuthorizationModule will be run against the final virtual path before execution. Defaults to true.
        /// Pipeline.AuthorizeImage will be fired regardless.
        /// </summary>
        public bool ReauthorizeFinalPath{get;set;}

        /// <summary>
        /// The source object (Bitmap, path, stream, IVirtualFile, etc..) for the image to process and/or cache and/or serve.
        /// </summary>
        public object Source{get;set;}

        /// <summary>
        /// The resizing/processing commands to apply.
        /// </summary>
        public ResizeSettings Settings { get; set; }

        public ImageRequestAction(object source, NameValueCollection query, string cachingKey, IPipelineConfig conf = null) {
            this.conf = (conf == null) ? Config.Current.Pipeline : conf;
            this.Source = source;
            this.Settings = new ResizeSettings(query);

        }


        public override void ExecuteResult(ControllerContext context) {
            throw new NotImplementedException();
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="virtualPath"></param>
        /// <param name="settings"></param>
        public void ExecuteVirtualPathResult(ControllerContext context, string virtualPath, NameValueCollection settings) {

            var app = (HttpApplication) context.HttpContext.GetService(typeof(HttpApplication));
    
            NameValueCollection q = settings;

            //Call URL rewriting events
            UrlEventArgs ue = new UrlEventArgs(virtualPath, q);
            conf.FireRewritingEvents(null, app.Context,ue);

            //Pull data back out of event object, resolving app-relative paths
            virtualPath = PathUtils.ResolveAppRelativeAssumeAppRelative(ue.VirtualPath);
            q = ue.QueryString;

            //Store the modified querystring in request for use by VirtualPathProviders
            conf.ModifiedQueryString = q;

            bool isAllowed = true;
            if (this.ReauthorizeFinalPath){
                //Who's the user
                IPrincipal user = app.Context.User as IPrincipal;
                // no user (must be anonymous...).. Or authentication doesn't work for this suffix. Whatever, just avoid a nullref in the UrlAuthorizationModule
                if (user == null)  user = new GenericPrincipal(new GenericIdentity(string.Empty, string.Empty), new string[0]);
                //Do we have permission to call UrlAuthorizationModule.CheckUrlAccessForPrincipal?
                bool canCheckUrl = System.Security.SecurityManager.IsGranted(new System.Security.Permissions.SecurityPermission(System.Security.Permissions.PermissionState.Unrestricted));
                //Run the rewritten path past the auth system again, using the result as the default "AllowAccess" value
                if (canCheckUrl) try {
                            isAllowed = UrlAuthorizationModule.CheckUrlAccessForPrincipal(virtualPath, user, "GET");
                        } catch (NotImplementedException) { } //For MONO support

            }
                 
            //Allow user code to deny access, but not modify the url or querystring.
            IUrlAuthorizationEventArgs authEvent = new UrlAuthorizationEventArgs(virtualPath, new NameValueCollection(q), isAllowed);
            conf.FireAuthorizeImage(null, app.Context, authEvent);

            if (!authEvent.AllowAccess) throw new ImageProcessingException(403, "Access denied", "Access denied");

                    
                    
            //Does the file exist physically? (false if VppUsage=always or file is missing)
            bool existsPhysically = (conf.VppUsage != VppUsageOption.Always) && System.IO.File.Exists(HostingEnvironment.MapPath(virtualPath));

            //If not present physically (and VppUsage!=never), try to get the virtual file. Null indicates a missing file
            IVirtualFile vf = (conf.VppUsage != VppUsageOption.Never && !existsPhysically) ? conf.GetFile(virtualPath, q) : null;

            //Only process files that exist
            if (existsPhysically || vf != null) {
                try{
                    ExecuteVirtualFileResult(app.Context, virtualPath, q, vf);
                    //Catch not found exceptions
                } catch (System.IO.FileNotFoundException notFound) { //Some VPPs are optimisitic , or could be a race condition
                    FileMissing(app.Context, virtualPath, q);
                    throw new ImageMissingException("The specified resource could not be located", "File not found", notFound);
                }
            } else
                FileMissing(app.Context, virtualPath, q);

        }

        
        /// <summary>
        /// 
        /// Generates the resized image to disk (if needed), then rewrites the request to that location.
        /// Perform 404 checking before calling this method. Assumes file exists.
        /// Called during PostAuthorizeRequest
        /// </summary>
        /// <param name="context"></param>
        /// <param name="virtualPath"></param>
		/// <param name="queryString"></param>
		/// <param name="vf"></param>
        public void ExecuteVirtualFileResult(HttpContext context, string virtualPath, NameValueCollection queryString, IVirtualFile vf) {
            Stopwatch s = new Stopwatch();
            s.Start();
            

            ResizeSettings settings = new ResizeSettings(queryString);

            bool isCaching = settings.Cache == ServerCacheMode.Always;
            bool isProcessing = settings.Process == ProcessWhen.Always;

            //By default, we process it if is both (a) a recognized image extension, and (b) has a resizing directive (not just 'cache').
            if (settings.Process == ProcessWhen.Default){
                //Check for resize directive by removing ('non-resizing' items from the current querystring) 
                NameValueCollection copy = new NameValueCollection(queryString);
                copy.Remove("cache"); copy.Remove("process"); copy.Remove("useresizingpipeline"); copy.Remove("404");
                //If the 'copy' still has directives, and it's an image request, then let's process it.
                isProcessing = conf.IsAcceptedImageType(virtualPath) &&  conf.HasPipelineDirective(copy);
            }

            //By default, we only cache it if we're processing it. 
            if (settings.Cache == ServerCacheMode.Default && isProcessing) 
                isCaching = true;

            //Resolve the 'cache' setting to 'no' unless we want it cache.
            if (!isCaching) settings.Cache = ServerCacheMode.No;

            context.Items[conf.ResponseArgsKey] = ""; //We are handling the request


            //Find out if we have a modified date that we can work with
            bool hasModifiedDate = (vf == null) || vf is IVirtualFileWithModifiedDate;
            DateTime modDate = DateTime.MinValue;
            if (hasModifiedDate && vf != null) {
                modDate = ((IVirtualFileWithModifiedDate)vf).ModifiedDateUTC;
                if (modDate == DateTime.MinValue || modDate == DateTime.MaxValue) {
                    hasModifiedDate = false; //Skip modified date checking if the file has no modified date
                }
            }


            IEncoder guessedEncoder = null;
            //Only use an encoder to determine extension/mime-type when it's an image extension or when we've set process = always.
            if (isProcessing) {
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
            ResponseArgs e = new ResponseArgs();
            e.RequestKey = virtualPath + PathUtils.BuildQueryString(queryString);
            e.RewrittenQuerystring = settings;
            e.ResponseHeaders.ContentType = isProcessing ? guessedEncoder.MimeType : fallbackContentType; 
            e.SuggestedExtension = isProcessing ? guessedEncoder.Extension : fallbackExtension;
            e.HasModifiedDate = hasModifiedDate;
            //Add delegate for retrieving the modified date of the source file. 
            e.GetModifiedDateUTC = new ModifiedDateDelegate(delegate() {
                if (vf == null)
                    return System.IO.File.GetLastWriteTimeUtc(HostingEnvironment.MapPath(virtualPath));
                else if (hasModifiedDate)
                    return modDate;
                else return DateTime.MinValue; //Won't be called, no modified date available.
            });

            //Add delegate for writing the data stream
            e.ResizeImageToStream = new ResizeImageDelegate(delegate(System.IO.Stream stream) {
                //This runs on a cache miss or cache invalid. This delegate is preventing from running in more
                //than one thread at a time for the specified cache key
                try {
                    if (!isProcessing) {
                        //Just duplicate the data
                        using (Stream source = (vf != null) ? vf.Open(): 
                                        File.Open(HostingEnvironment.MapPath(virtualPath), FileMode.Open, FileAccess.Read, FileShare.Read)) {
                            Utils.copyStream(source, stream);
                        }
                    } else {
                        //Process the image
                        if (vf != null)
                            conf.GetImageBuilder().Build(vf, stream, settings);
                        else
                            conf.GetImageBuilder().Build(HostingEnvironment.MapPath(virtualPath), stream, settings); //Use a physical path to bypass virtual file system
                    }

                    //Catch not found exceptions
                } catch (System.IO.FileNotFoundException notFound) {
                    //This will be called later, if at all. 
                    FileMissing(context, virtualPath, queryString);
                    throw new ImageMissingException("The specified resource could not be located","File not found", notFound);
                }
            });


            context.Items[conf.ResponseArgsKey] = e; //store in context items

            //Fire events (for client-side caching plugins)
            conf.FirePreHandleImage(null, context, e);

            //Pass the rest of the work off to the caching module. It will handle rewriting/redirecting and everything. 
            //We handle request headers based on what is found in context.Items
            ICache cache = conf.GetCacheProvider().GetCachingSystem(context, e);

            //Verify we have a caching system
            if (cache == null) throw new ImageProcessingException("Image Resizer: No caching plugin was found for the request");
            
            if (cache is NoCache){
            }else{
            }
            

            s.Stop();
            context.Items["ResizingTime"] = s.ElapsedMilliseconds;

        }


         protected void FileMissing(HttpContext httpContext, string virtualPath, NameValueCollection q) {
            //Fire the event (for default image redirection, etc) 
            conf.FireImageMissing(null, httpContext, new UrlEventArgs( virtualPath, new NameValueCollection(q)));

            //Remove the image from context items so we don't try to write response headers.
            httpContext.Items[conf.ResponseArgsKey] = null;
         }

        /* To support S3Reader properly, we need to cache unprocessed images: 
         * 
        void Pipeline_RewriteDefaults(System.Web.IHttpModule sender, System.Web.HttpContext context, Configuration.IUrlEventArgs e) {
            //Always request caching for this VPP. Will not override existing values.
            if (vpp.IsPathVirtual(e.VirtualPath)) e.QueryString["cache"] = ServerCacheMode.Always.ToString();
        }*/
        /* To support SqlReader properly, we need to skip file type checks when requested, cache unmodified files, and reencode data when requested.
         * 
                };
         */


    }
}
