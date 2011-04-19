/* Copyright (c) 2011 Nathanael Jones. See license.txt */
using System;
using ImageResizer.Configuration;
using System.Web;
using System.Collections.Specialized;
using System.Security.Principal;
using System.Web.Security;
using System.Web.Hosting;
using System.Diagnostics;
using ImageResizer.Plugins;
using ImageResizer.Encoding;
using ImageResizer.Caching;
using ImageResizer.Util;
using System.Collections.Generic;
namespace ImageResizer {

    /// <summary>
    /// Monitors incoming image requests. Image requests that request resizing are processed, cached, and served.
    /// </summary>
    public class InterceptModule : IHttpModule {

        /// <summary>
        /// Called when the app is initialized
        /// </summary>
        /// <param name="context"></param>
        void IHttpModule.Init(System.Web.HttpApplication context) {
            //We wait until after URL auth happens for security. (although we authorize again since we are doing URL rewriting)
            context.PostAuthorizeRequest += CheckRequest_PostAuthorizeRequest;

            //This is where we set content-type and caching headers. content-type often don't match the
            //file extension, so we have to override them
            context.PreSendRequestHeaders += context_PreSendRequestHeaders;

        }
        void IHttpModule.Dispose() { }
        /// <summary>
        /// Current configuration. Same as Config.Current.Pipeline
        /// </summary>
        protected IPipelineConfig conf { get { return Config.Current.Pipeline; } }

        /// <summary>
        /// This is where we filter requests and intercept those that want resizing performed.
        /// We first strip FakeExtension, then verify the remaining file extension is supported for decoding.
        /// We fire URL rewriting events. If the result includes any supported querystring params afterwards, we process the request. Otherwise we let it fall back to IIS/ASP.NET.
        /// If the file doesn't exist, we also ignore the request. They're going to cause a 404 anyway.
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void CheckRequest_PostAuthorizeRequest(object sender, EventArgs e) {
            //Skip requests if the Request object isn't populated
            HttpApplication app = sender as HttpApplication;
            if (app == null) return;
            if (app.Context == null) return;
            if (app.Context.Request == null) return;

            conf.FirePostAuthorizeRequest(this, app.Context);

            //Copy FilePath so we can modify it. Add PathInfo back on so we can support directories with dots in them.
            string filePath = app.Context.Request.FilePath + app.Context.Request.PathInfo;

            //Allows users to append .ashx to all their image URLs instead of doing wildcard mapping.
            IList<string> altExtensions = conf.FakeExtensions; //pipeline.fakeExtensions


            //Remove extensions from filePath, since otherwise IsAcceptedImageType() will fail.
            foreach (string s in altExtensions) {
                if (filePath.EndsWith(s, StringComparison.OrdinalIgnoreCase)) {
                    filePath = filePath.Substring(0, filePath.Length - s.Length).TrimEnd('.');
                    break;
                }
            }
            

            //Is this an image request? Checks the file extension for .jpg, .png, .tiff, etc.
            if (conf.IsAcceptedImageType(filePath)) {
                //Copy the querystring so we can mod it to death without messing up other stuff.
                NameValueCollection q = new NameValueCollection(app.Context.Request.QueryString);

                //Call events to do resize(w,h,f)/ parsing and any other custom syntax.
                UrlEventArgs ue = new UrlEventArgs(filePath, q);
                conf.FireRewritingEvents(this, app.Context,ue);

                //Pull data back out of event object, resolving app-relative paths
                string virtualPath = fixPath(ue.VirtualPath);
                q = ue.QueryString;

                //See if resizing is wanted (i.e. one of the querystring commands is present).
                //Called after processPath so processPath can add them if needed.
                //Checks for thumnail, format, width, height, maxwidth, maxheight and a lot more
                if (conf.HasPipelineDirective(q)) {
                    //Who's the user
                    IPrincipal user = app.Context.User as IPrincipal;

                    // no user (must be anonymous...).. Or authentication doesn't work for this suffix. Whatever, just avoid a nullref in the UrlAuthorizationModule
                    if (user == null)
                        user = new GenericPrincipal(new GenericIdentity(string.Empty, string.Empty), new string[0]);

                    //Run the rewritten path past the auth system again
                    if (!UrlAuthorizationModule.CheckUrlAccessForPrincipal(virtualPath, user, "GET")) throw new HttpException(403, "Access denied.");

                    //Allow user code to deny access, but not modify the url or querystring.
                    conf.FirePostAuthorizeImage(this, app.Context, new UrlEventArgs(virtualPath, new NameValueCollection(q)));

                    //Store the modified querystring in request for use by VirtualPathProviders
                    app.Context.Items[conf.ModifiedQueryStringKey] = q; // app.Context.Items["modifiedQueryString"] = q;

                    
                    //Does the file exist?
                    bool existsPhysically = (conf.VppUsage != VppUsageOption.Always) && System.IO.File.Exists(HostingEnvironment.MapPath(virtualPath));

                    //Mutually exclusive with existsPhysically. 
                    bool existsVirtually = (conf.VppUsage != VppUsageOption.Never && !existsPhysically) && HostingEnvironment.VirtualPathProvider.FileExists(virtualPath);
                    //Create the virtual file instance only if (a) VppUsage=always, and it exists virtually, or (b) VppUsage=fallback, and it only exists virtually
                    VirtualFile vf = existsVirtually ? HostingEnvironment.VirtualPathProvider.GetFile(virtualPath) : null;

                    //Only process files that exists
                    if (existsPhysically || existsVirtually)
                        ResizeRequest(app.Context, virtualPath, q, vf);


                }
            }

        }

        /// <summary>
        /// Turns relative paths into domain-relative paths.
        /// Turns app-relative paths into domain relative paths.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        protected String fixPath(string virtualPath) {

            if (virtualPath.StartsWith("~")) return HostingEnvironment.ApplicationVirtualPath.TrimEnd('/') + "/" + virtualPath.TrimStart('/');
            if (!virtualPath.StartsWith("/")) return HostingEnvironment.ApplicationVirtualPath.TrimEnd('/') + "/" + virtualPath;
            return virtualPath;
        }




        /// <summary>
        /// Generates the resized image to disk (if needed), then rewrites the request to that location.
        /// Perform 404 checking before calling this method. Assumes file exists.
        /// Called during PostAuthorizeRequest
        /// </summary>
        /// <param name="context"></param>
        /// <param name="current"></param>
        protected virtual void ResizeRequest(HttpContext context, string virtualPath, NameValueCollection queryString, VirtualFile vf) {
            Stopwatch s = new Stopwatch();
            s.Start();


            //Find out if we have a modified date that we can work with
            bool hasModifiedDate = (vf == null) || vf is IVirtualFileWithModifiedDate;
            DateTime modDate = DateTime.MinValue;
            if (hasModifiedDate && vf != null) {
                modDate = ((IVirtualFileWithModifiedDate)vf).ModifiedDateUTC;
                if (modDate == DateTime.MinValue || modDate == DateTime.MaxValue) {
                    hasModifiedDate = false; //Skip modified date checking if the file has no modified date
                }
            }

            ResizeSettings settings =new ResizeSettings(queryString);
            IEncoder guessedEncoder = conf.GetImageBuilder().EncoderProvider.GetEncoder(null,settings);

            //Build CacheEventArgs
            ResponseArgs e = new ResponseArgs();
            e.RequestKey = virtualPath + Utils.toQuerystring(queryString);
            e.RewrittenQuerystring = settings;
            e.ResponseHeaders.ContentType = guessedEncoder.MimeType;
            e.SuggestedExtension = guessedEncoder.Extension;
            e.HasModifiedDate = hasModifiedDate;
            //Add delegate for retrieving the modified date of the source file. s
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
                //than one thread at a time for the specified source file (current.Local)

                if (vf != null)
                    conf.GetImageBuilder().Build(vf, stream, settings);
                else
                    conf.GetImageBuilder().Build(HostingEnvironment.MapPath(virtualPath), stream, settings);

            });
            
            
            context.Items[conf.ResponseArgsKey] = e; //store in context items

            //Fire events (for client-side caching plugins)
            conf.FirePreHandleImage(this, context, e);

            //Pass the rest of the work off to the caching module. It will handle rewriting/redirecting and everything. 
            //We handle request headers based on what is found in context.Items
            conf.GetCacheProvider().GetCachingSystem(context, e).Process(context, e);

            s.Stop();
            context.Items["ResizingTime"] = s.ElapsedMilliseconds;

        }

        private void PreProcess(HttpContext context, ResponseArgs e) {
            context.Items[conf.ResponseArgsKey] = e; //store in context items


            ////Determine the client caching settings (server time zone, not utc)
            //string cacheSetting = conf.get("clientcache.minutes", null);
            //if (!string.IsNullOrEmpty(cacheSetting)) {
            //    double f;
            //    if (double.TryParse(cacheSetting, out f) && f > 0)
            //        e.ResponseHeaders.Expires = DateTime.Now.AddMinutes(f);
            //} else {
            //    e.ResponseHeaders.Expires = DateTime.Now.AddHours(24); //Default to 24 hours
            //}

            ////Set last modified date
            //if (e.HasModifiedDate) e.ResponseHeaders.LastModified = e.GetModifiedDateUTC();

            conf.FirePreHandleImage(this,context,e);
        }
        /// <summary>
        /// We don't actually send the data - but we still want to control the headers on the data.
        /// PreSendRequestHeaders allows us to change the content-type and cache headers at excatly the last
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void context_PreSendRequestHeaders(object sender, EventArgs e) {
            //Skip requests if the Request object isn't populated
            HttpApplication app = sender as HttpApplication;
            if (app == null) return;
            if (app.Context == null) return;
            if (app.Context.Items == null) return;
            if (app.Context.Request == null) return;
            HttpContext context = app.Context;
            //Skip requests if we don't have an object to work with.
            if (context.Items[conf.ResponseArgsKey] == null) return;
            //Try to cast the object.
            IResponseArgs obj = context.Items[conf.ResponseArgsKey] as IResponseArgs;
            if (obj == null) return;
            //Apply the headers
            if (obj.ResponseHeaders.ApplyDuringPreSendRequestHeaders)
                obj.ResponseHeaders.ApplyToResponse(obj.ResponseHeaders, app.Context);


        }

    }
}
