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
            context.PostAuthorizeRequest += new EventHandler(CheckRequest);
            //This is where we set content-type and caching headers. content-type headers don't match the 
            //file extension when format= or thumbnail= is used, so we have to override them
            context.PreSendRequestHeaders += new EventHandler(context_PreSendRequestHeaders);
        }
        void System.Web.IHttpModule.Dispose() { }


       

        /// <summary>
        /// This is where we filter requests and intercet those that want resizing performed.
        /// We first check for image extensions... 
        /// If it is one, then we run it through the CustomFolders methods to see if if there is custom resizing for it..
        /// If there aren't any querystring params or "resize(x,y,f)/" in the path after that, then we ignore the request.
        /// If the file doesn't exist, we ignore the request.
        /// 
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
                    string basePath = app.Context.Request.Path;
                    NameValueCollection q = new NameValueCollection();

                    //Parse and remove resize(x,y,f) already in URL
                    basePath = parseResizeFolderSyntax(basePath, q);

                    //Set folder resizing defaults (adds in resize(x,y) folder)
                    basePath = CustomFolders.folderDefaults(basePath);

                    //Parse and remove resize(x,y,f) added by folderDefaults
                    basePath =  parseResizeFolderSyntax(basePath, q);

                    //Overwrite with querystring values.
                    foreach (string k in app.Context.Request.QueryString)
                         q[k] = app.Context.Request.QueryString[k];

                    //Set folder resizing overrides (overrides the querystring)
                    basePath = CustomFolders.folderOverrides(basePath);

                    //Parse and remove resize(x,y,f) added by folderOverrides. Override existing values in q
                    basePath = parseResizeFolderSyntax(basePath, q);

                    //See if resizing is wanted
                    if (IsOneSpecified(q["thumbnail"], q["format"], q["width"], q["height"], q["maxwidth"], q["maxheight"], q["quality"]))
                    {
                        yrl current = new yrl(basePath);
                        current.QueryString = q;

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
        /// Matches /resize(x,y,f)/ syntax
        /// Fixed Bug - will replace both slashes.. make first a lookbehind
        /// </summary>
        private static Regex resizeFolder = new Regex(@"(?<=^|\/)resize\(\s*(?<maxwidth>\d+)\s*,\s*(?<maxheight>\d+)\s*(?:,\s*(?<format>jpg|png|gif)\s*)?\)\/", RegexOptions.Compiled
           | RegexOptions.IgnoreCase);

        /// <summary>
        /// Parses and removes the resize folder syntax "resize(x,y,f)/" from the specified file path. 
        /// Places settings into the referenced querystring
        /// </summary>
        /// <param name="path"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public string parseResizeFolderSyntax(string path, NameValueCollection q)
        {
            Match m = resizeFolder.Match(path);
            if (m.Success)
            {
                int maxwidth = -1;
                int.TryParse(m.Groups["maxwidth"].Value, out maxwidth);
                int maxheight = -1;
                int.TryParse(m.Groups["maxheight"].Value, out maxheight);
                string format = null;
                if (m.Groups["format"].Captures.Count > 0)
                {
                    format = m.Groups["format"].Captures[0].Value;
                }
                //Remove resize folder from URL
                path = resizeFolder.Replace(path, "");
                //Add values to querystring
                if (maxwidth > 0) q["maxwidth"] = maxwidth.ToString();
                if (maxheight > 0) q["maxheight"] = maxheight.ToString();
                if (format != null) q["format"] = format;

                //Call recursive - We want to pull all resize() folders out, otherwise they would allow overriding folderOverrides
                return parseResizeFolderSyntax(path, q);
            }

            return path;
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
