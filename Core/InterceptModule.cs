/**
 * Written by Nathanael Jones 
 * http://nathanaeljones.com
 * nathanael.jones@gmail.com
 * 
 * Although I typically release my components for free, I decided to charge a 
 * 'download fee' for this one to help support my other open-source projects. 
 * Don't worry, this component is still open-source, and the license permits 
 * source redistribution as part of a larger system. However, I'm asking that 
 * people who want to integrate this component purchase the download instead 
 * of ripping it out of another open-source project. My free to non-free LOC 
 * (lines of code) ratio is still over 40 to 1, and I plan on keeping it that 
 * way. I trust this will keep everybody happy.
 * 
 * By purchasing the download, you are permitted to 
 * 
 * 1) Modify and use the component in all of your projects. 
 * 
 * 2) Redistribute the source code as part of another project, provided 
 * the component is less than 5% of the project (in lines of code), 
 * and you keep this information attached.
 * 
 * 3) If you received the source code as part of another open source project, 
 * you cannot extract it (by itself) for use in another project without purchasing a download 
 * from http://nathanaeljones.com/. If nathanaeljones.com is no longer running, and a download
 * cannot be purchased, then you may extract the code.
 * 
 * Disclaimer of warranty and limitation of liability continued at http://nathanaeljones.com/11151_Image_Resizer_License
 **/

//Clear the image cache before upgrading.. .not sure why it's needed, but images appear corrupted...

using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Drawing;
using fbs;
using System.IO;
using System.Web.Hosting;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using System.Security.Principal;
using fbs.ImageResizer.Caching;
using fbs.ImageResizer.Configuration;
using fbs.ImageResizer.Configuration;
using fbs.ImageResizer.Encoding;
using fbs.ImageResizer.Plugins;


namespace fbs.ImageResizer {

    /// <summary>
    /// Monitors incoming image requests. Image requests that request resizing are processed, cached, and served.
    /// </summary>
    public class InterceptModule : System.Web.IHttpModule {

        /// <summary>
        /// Called when the app is initialized
        /// </summary>
        /// <param name="context"></param>
        void System.Web.IHttpModule.Init(System.Web.HttpApplication context) {
            //We wait until after URL auth happens for security. (although we authorize again since we are doing URL rewriting)
            context.PostAuthorizeRequest += new EventHandler(CheckRequest_PostAuthorizeRequest);

            //This is where we set content-type and caching headers. content-type often don't match the
            //file extension, so we have to override them
            context.PreSendRequestHeaders += new EventHandler(context_PreSendRequestHeaders);

        }
        void System.Web.IHttpModule.Dispose() { }
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

            //Copy FilePath so we can modify it
            string filePath = app.Context.Request.FilePath; //Doesn't include pathInfo

            //Allows users to append .ashx to all their image URLs instead of doing wildcard mapping.
            string altExtension = conf.FakeExtension; // Configuration.get("rewriting.fakeExtension", ".ashx");


            //Remove extension from filePath, since otherwise IsAcceptedImageType() will fail.
            if (!string.IsNullOrEmpty(altExtension) && filePath.EndsWith(altExtension, StringComparison.OrdinalIgnoreCase))
                filePath = filePath.Substring(0, filePath.Length - altExtension.Length).TrimEnd('.');


            //Is this an image request? Checks the file extension for .jpg, .png, .tiff, etc.
            if (conf.IsAcceptedImageType(filePath)) {
                //Copy the querystring so we can mod it to death without messing up other stuff.
                NameValueCollection q = new NameValueCollection(app.Context.Request.QueryString);

                //Call events to do resize(w,h,f)/ parsing and any other custom syntax.
                UrlEventArgs ue = new UrlEventArgs(filePath + app.Context.Request.PathInfo, q); //Includes pathinfo
                conf.FireRewritingEvents(this, app.Context,ue);

                //Pull data back out of event object
                string basePath = ue.VirtualPath;
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
                    if (!UrlAuthorizationModule.CheckUrlAccessForPrincipal(basePath, user, "GET")) throw new HttpException(403, "Access denied.");

                    //Allow user code to deny access.
                    conf.FirePostAuthorizeImage(this, app.Context, new UrlEventArgs(basePath, new NameValueCollection(q)));

                    //Store the modified querystring in request for use by VirtualPathProviders
                    app.Context.Items[conf.ModifiedQueryStringKey] = q; // app.Context.Items["modifiedQueryString"] = q;

                    //Build a URL using the new basePath and the new Querystring 
                    yrl current = new yrl(basePath);
                    current.QueryString = q;

                    //If the file or virtual file exists, resize it
                    if (conf.VppUsage != VppUsageOption.Always && current.FileExists ||
                        (conf.VppUsage != VppUsageOption.Never && HostingEnvironment.VirtualPathProvider.FileExists(getVPPSafePath(current))))
                        ResizeRequest(app.Context, current);

                }
            }

        }


        protected String getVPPSafePath(yrl y) {
            return yrl.GetAppFolderName().TrimEnd(new char[] { '/' }) + "/" + y.BaseFile;
        }




        /// <summary>
        /// Generates the resized image to disk (if needed), then rewrites the request to that location.
        /// Perform 404 checking before calling this method. Assumes file exists.
        /// Called during PostAuthorizeRequest
        /// </summary>
        /// <param name="context"></param>
        /// <param name="current"></param>
        protected virtual void ResizeRequest(HttpContext context, yrl current) {
            Stopwatch s = new Stopwatch();
            s.Start();

            //Create our virtual file (if needed/wanted)
            VirtualFile vf = null;
            if (conf.VppUsage == VppUsageOption.Always || (conf.VppUsage == VppUsageOption.Fallback && !File.Exists(current.Local)))
                vf = HostingEnvironment.VirtualPathProvider.GetFile(getVPPSafePath(current));


            //Find out if we have a modified date that we can work with
            bool hasModifiedDate = (vf == null) || vf is IVirtualFileWithModifiedDate;
            DateTime modDate = DateTime.MinValue;
            if (hasModifiedDate && vf != null) {
                modDate = ((IVirtualFileWithModifiedDate)vf).ModifiedDateUTC;
                if (modDate == DateTime.MinValue || modDate == DateTime.MaxValue) {
                    hasModifiedDate = false; //Skip modified date checking if the file has no modified date
                }
            }

            ResizeSettings settings =new ResizeSettings(current.QueryString);
            IEncoder guessedEncoder = conf.GetImageBuilder().GetEncoder(null,settings);

            //Build CacheEventArgs
            ResponseArgs e = new ResponseArgs();
            e.RequestKey = current.ToString();
            e.RewrittenQuerystring = settings;
            e.ResponseHeaders.ContentType = guessedEncoder.MimeType;
            e.SuggestedExtension = guessedEncoder.Extension;
            e.HasModifiedDate = hasModifiedDate;
            //Add delegate for retrieving the modified date of the source file. s
            e.GetModifiedDateUTC = new ModifiedDateDelegate(delegate() {
                if (vf == null)
                    return System.IO.File.GetLastWriteTimeUtc(current.Local);
                else if (hasModifiedDate)
                    return modDate;
                else return DateTime.MinValue; //Won't be called, no modified date available.
            });
            //Add delegate for writing the data stream
            e.ResizeImageToStream = new ResizeImageDelegate(delegate(Stream stream) {
                //This runs on a cache miss or cache invalid. This delegate is preventing from running in more
                //than one thread at a time for the specified source file (current.Local)

                if (vf != null)
                    conf.GetImageBuilder().Build(vf, stream, settings);
                else
                    conf.GetImageBuilder().Build(current.Local, stream, settings);

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
