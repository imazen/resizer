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


namespace fbs.ImageResizer
{
    /// <summary>
    /// Monitors incoming image requests. Image requests that request resizing are processed. The resized images are immediately written to disk, and 
    /// the request is rewritten to the disk-cached resized version. This way IIS can handle the actual serving of the file.
    /// The disk-cache directory is protected through URL authorization.
    /// See CustomFolders.cs for any type of URL rewriting 
    /// </summary>
    public class InterceptModule:System.Web.IHttpModule
    {
        
        /// <summary>
        /// Called when the app is initialized
        /// </summary>
        /// <param name="context"></param>
        void System.Web.IHttpModule.Init(System.Web.HttpApplication context)
        {
            //We wait until after URL auth happens for security.
            context.PostAuthorizeRequest += new EventHandler(CheckRequest_PostAuthorizeRequest);
            //This is where we set content-type and caching headers. content-type headers don't match the 
            //file extension when format= or thumbnail= is used, so we have to override them
            context.PreSendRequestHeaders += new EventHandler(context_PreSendRequestHeaders);
        }
        void System.Web.IHttpModule.Dispose() { }


       

        /// <summary>
        /// This is where we filter requests and intercet those that want resizing performed.
        /// We first check for image extensions... 
        /// If it is one, then we run it through the CustomFolders methods to see if if there is custom resizing for it..
        /// If there still aren't any querystring params after that, then we ignore the request.
        /// If the file doesn't exist, we also ignore the request. They're going to cause a 404 anyway.
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void CheckRequest_PostAuthorizeRequest(object sender, EventArgs e)
        {
            //Get the http context, and only intercept requests where the Request object is actually populated
            HttpApplication app = sender as HttpApplication;
            if (app != null && app.Context != null && app.Context.Request != null)
            {
                
                //Is this an image request? Checks the file extension for .jpg, .png, .tiff, etc.
                if (ImageOutputSettings.IsAcceptedImageType(app.Context.Request.FilePath))
                {
                    //Init the caching settings. These only take effect if the image is actually resized
                    //CustomFolders.cs can override these during processPath
                    app.Context.Items["ContentExpires"] = DateTime.Now.AddHours(24); //Default to 24 hours
                    string cacheSetting = ConfigurationManager.AppSettings["ImageResizerClientCacheMinutes"];
                    if (!string.IsNullOrEmpty(cacheSetting)){
                        double f;
                        if (double.TryParse(cacheSetting,out f)){
                            if (f >= 0)
                                app.Context.Items["ContentExpires"] = DateTime.Now.AddMinutes(f);
                            else
                                app.Context.Items["ContentExpires"] = null;
                        }
                    }
                    
                    //Copy the querystring
                    NameValueCollection q = new NameValueCollection(app.Context.Request.QueryString);

                    //Call CustomFolders.cs to do resize(w,h,f)/ parsing and any other custom syntax.
                    //The real virtual path should be returned (with the resize() stuff removed)
                    //And q should be populated with the querystring values
                    string basePath = CustomFolders.processPath(app.Context.Request.Path, q);

                    
                    //If the path has changed, this will circumvent the URL auth system.
                    //Make sure the user has explicity allowed it through web.config
                    if (!basePath.Equals(app.Context.Request.Path))
                    {
                        //Make sure the resize() notation is allowed.
                        string allow = ConfigurationManager.AppSettings["AllowURLRewriting"];
                        if (string.IsNullOrEmpty(allow)) allow = ConfigurationManager.AppSettings["AllowFolderResizeNotation"];
                        if (string.IsNullOrEmpty(allow) || allow.Equals("false", StringComparison.OrdinalIgnoreCase)){
                            return; //Skip the request
                        }
                        //Prevent access to the /imagecache/ directory (URL auth won't be protecting it now)
                        if (new yrl(basePath).Local.StartsWith(DiskCache.GetCacheDir(), StringComparison.OrdinalIgnoreCase))
                        {
                            throw new HttpException(403, "Access denied to image cache folder.");
                        }
                    }
                    //See if resizing is wanted (i.e. one of the querystring commands is present).
                    //Called after processPath so processPath can add them if needed.
                    //Checks for thumnail, format, width, height, maxwidth, maxheight and a lot more
                    if (ImageManager.HasResizingDirective(q))
                    {
                        //Build a URL using the new basePath and the new Querystring q
                        yrl current = new yrl(basePath);
                        current.QueryString = q;

                        //If the file exists, resize it
                        if (current.FileExists)  
                            ResizeRequest(app.Context,current);
                        
                    }
                }
            }
        }



        /// <summary>
        /// Builds the physical path for the cached version, using the hashcode of the normalized URL.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        protected string getCachedVersionFilename(yrl request)
        {
            string dir = DiskCache.GetCacheDir();
            if (dir == null) return null;
            //Build the physical path of the cached version, using the hashcode of the normalized URL.
            return dir.TrimEnd('/', '\\') + "\\" + request.ToString().ToLower().GetHashCode().ToString() + "." + new ImageOutputSettings(request).GetFinalExtension();
        }

        
        /// <summary>
        /// Generates the resized image to disk (if needed), then rewrites the request to that location.
        /// Perform 404 checking before calling this method. Assumes file exists.
        /// Called during PostAuthorizeRequest
        /// </summary>
        /// <param name="r"></param>
        /// <param name="extension"></param>
        protected virtual void ResizeRequest(HttpContext context, yrl current)
        {
            //This is where the cached version goes
            string cachedFile = getCachedVersionFilename(current);

            //Disk caching is good for images because they change much less often than the application restarts.

            //Make sure the resized image is in the disk cache.
            bool succeeded = DiskCache.UpdateCachedVersionIfNeeded(current.Local, cachedFile,
                delegate(){
                    //This runs if the update is needed. This delegate is preventing from running in more
                    //than one thread at a time for the specified source file (current.Local)
                    AnimatedImageManager.BuildImage(current.Local, cachedFile, current.QueryString);
                },30000);

            //If a co-occurring resize has the file locked for more than 30 seconds, quit with an error.
            if (!succeeded)
                throw new ApplicationException("Failed to acquire a lock on file \"" + current.Virtual + "\" within 30 seconds. Image resizing failed.");
            

            //Get domain-relative path of cached file.
            string virtualPath = yrl.GetAppFolderName().TrimEnd(new char[] { '/' }) + "/" + yrl.FromPhysicalString(cachedFile).ToString();

            //Add content-type headers (they're not added correctly when the source URL extension is wrong)
            //Determine content-type string;
            string contentType = new ImageOutputSettings(current).GetContentType();
            
            context.Items["FinalContentType"] = contentType;
            context.Items["FinalCachedFile"] = cachedFile;


            //Rewrite to cached, resized image.
            context.RewritePath(virtualPath, false);
        }
        /// <summary>
        /// We don't actually send the data - but we still want to control the headers on the data.
        /// PreSendRequestHeaders allows us to change the content-type and cache headers at excatly the last
        /// second. We populate the headers from context.Items["FinalContentType"],
        /// context.Items["ContentExpires"], and context.Items["FinalCachedFile"].
        /// This also indirectly enables server-side mem caching. (HttpCacheability.Public does it)
        /// We set the file dependency to FinalCachedFile so changes are update quickly server-side
        /// - however, clients will not check for updates until ContentExpires occurs.
        ///  
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void context_PreSendRequestHeaders(object sender, EventArgs e)
        {
            HttpApplication app = sender as HttpApplication;
            HttpContext context = (app != null) ? app.Context : null;
            //Check to ensure the context and Response is in good shape (it's needed)
            if (context != null && context.Items != null && context.Items["FinalContentType"] != null && context.Items["FinalCachedFile"] != null)
            {
                //Clear previous output
                //context.Response.Clear();
                context.Response.ContentType = context.Items["FinalContentType"].ToString();
                //Add caching headers
                context.Response.AddFileDependency(context.Items["FinalCachedFile"].ToString());

                //It's not UTC - server time zone.
                if (context.Items["ContentExpires"] != null)
                    context.Response.Cache.SetExpires((DateTime)context.Items["ContentExpires"]);

                //Enables in-memory caching
                context.Response.Cache.SetCacheability(HttpCacheability.Public);
                context.Response.Cache.SetLastModifiedFromFileDependencies();
                context.Response.Cache.SetValidUntilExpires(false);
            }

        }

        
    }
}
