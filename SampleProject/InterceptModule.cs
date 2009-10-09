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

namespace fbs.ImageResizer
{
    class InterceptModule:System.Web.IHttpModule
    {
        void System.Web.IHttpModule.Dispose(){}

        /// <summary>
        /// Called when the app is initialized
        /// </summary>
        /// <param name="context"></param>
        void System.Web.IHttpModule.Init(System.Web.HttpApplication context)
        {
            context.PostAuthorizeRequest += new EventHandler(CheckRequest);
            context.PreSendRequestHeaders += new EventHandler(context_PreSendRequestHeaders);
        }



        /// <summary>
        /// This is where we filter requests and intercet those that want resizing performed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CheckRequest(object sender, EventArgs e)
        {
            //Get the http context
            HttpApplication app = sender as HttpApplication;
            if (app != null && app.Context != null && app.Context.Request != null)
            {
                //Is this an image request?
                string extension = System.IO.Path.GetExtension(app.Context.Request.FilePath).ToLowerInvariant().Trim('.');
                if (AcceptedImageExtensions.Contains(extension))
                {
                    yrl current = CustomURLs.customizeURL(yrl.Current);
                    //Here is the where 


                    //Is the querystring requesting a resize?
                    NameValueCollection q = current.QueryString;
                    if (IsOneSpecified(q["thumbnail"], q["format"], q["width"], q["height"], q["maxwidth"], q["maxheight"]))
                    {
                        //Does the physical file exist?
                        if (current.FileExists)
                        {
                            //It's for image resizing.
                            ResizeRequest(app.Context,current, extension);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if one or more of the arguments has a non-null or non-empty value
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private bool IsOneSpecified(params String[] args){
            foreach (String s in args) if (!string.IsNullOrEmpty(s)) return true;
            return false;
        }
        private static IList<String> _acceptedImageExtensions = new List<String>(new String[] { "jpg", "jpeg", "bmp", "gif", "png" });
        /// <summary>
        /// Returns a list of (lowercase invariant) image extensions that the module works with.
        /// Not thread safe for writes. A shared collection is used.
        /// </summary>
        protected static IList<String> AcceptedImageExtensions{
            get{
                return _acceptedImageExtensions;
            }
        }
        /// <summary>
        /// Builds the physical path for the cached version, using the hashcode of the normalized URL.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private string getCachedVersionFilename(yrl request)
        {
            string dir = DiskCache.GetCacheDir();
            if (dir == null) return null;
            //Build the physical path of the cached version, using the hashcode of the normalized URL.
            return dir.TrimEnd('/', '\\') + "\\" + request.ToString().ToLower().GetHashCode().ToString() + "." + GetOutputExtension(request);
        }

        /// <summary>
        /// Returns the appropriate file extension for the specified request. Looks at ?thumbnail=jpg, format=jpg etc. Looks at the original extension last. If the format is not an accepted output format, jpeg is used 
        /// </summary>
        /// <param name="requestUrl"></param>
        /// <returns></returns>
        public static string GetOutputExtension(yrl r)
        {
            string type = "";
            //Use the value from 'thumbnail' if available.
            if (!string.IsNullOrEmpty(r["thumbnail"])) type = r["thumbnail"].ToLowerInvariant().Trim();
            //If that didn't work, try using the value from 'format'
            if (string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(r["format"])) type = r["format"].ToLowerInvariant().Trim();
            
            List<String> formats = new List<string>(new string[]{"png","gif","jpg","jpeg"});
            
            if (!formats.Contains(type)){
                type = r.Extension.ToLowerInvariant().Trim('.').Trim();
                if (!formats.Contains(type)){
                    return "jpg"; //The default if we didn't recognize any of them as valid output formats. For example, if a .bmp file is requested, it will simply be converted to .jpg. We don't serve .bmp files.
                }
            }
            //Consolidate jpeg->jpg
            if (type.Equals("jpeg", StringComparison.OrdinalIgnoreCase)) type = "jpg";
            return type;
        }

        /// <summary>
        /// Generates the resized image to disk (if needed), then rewrites the request to that location.
        /// Perform 404 checking before calling this method. Assumes file exists.
        /// </summary>
        /// <param name="r"></param>
        /// <param name="extension"></param>
        private void ResizeRequest(HttpContext context, yrl current, string extension)
        {
          

            //This is where the cached version goes
            string cachedFile = getCachedVersionFilename(current);

            //Disk caching is good for images because they change much less often than the application restarts.

            //Make sure the resized image is in the disk cache.
            DiskCache.UpdateCachedVersionIfNeeded(current.Local, cachedFile, 
                delegate()
                {
                    ImageManager.ResizeImage(current.Local, cachedFile, current.QueryString);
                });

            //Get domain-relative path of cached file.
            string virtualPath = yrl.GetAppFolderName().TrimEnd(new char[] { '/' }) + "/" + yrl.FromPhysicalString(cachedFile).ToString();

            



            //Add content-type headers (they're not added correctly when the URL extension is wrong)
            //Determine content-type string;
            string contentType = "image/jpeg";

            switch (GetOutputExtension(current))
            {
                case "png":
                    contentType = "image/x-png";
                    break;
                case "gif":
                    contentType = "image/gif";
                    break;
            }
            context.Items["FinalContentType"] = contentType;
            context.Items["FinalCachedFile"] = cachedFile;



            //Rewrite to cached, resized image.
            context.RewritePath(virtualPath, false);
        }

        void context_PreSendRequestHeaders(object sender, EventArgs e)
        {
            HttpApplication app = sender as HttpApplication;
            HttpContext context = (app != null) ? app.Context : null;

            if (context != null && context.Items != null && context.Items["FinalContentType"] != null && context.Items["FinalCachedFile"] != null)
            {
                //Clear previous output
                //context.Response.Clear();
                context.Response.ContentType = context.Items["FinalContentType"].ToString();
                //Add caching headers
                context.Response.AddFileDependency(context.Items["FinalCachedFile"].ToString());

                context.Response.Cache.SetExpires(DateTime.Now.AddHours(24));
                //Enables in-memory caching
                context.Response.Cache.SetCacheability(HttpCacheability.Public);
                context.Response.Cache.SetLastModifiedFromFileDependencies();
                context.Response.Cache.SetValidUntilExpires(false);
            }

        }

        
    }
}
