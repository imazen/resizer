/**
 * Written by Nathanael Jones 
 * http://nathanaeljones.com
 * nathanael.jones@gmail.com
 * 
 * This file is for user extension and modification (although all the source is!)
 * No restrictions on distribution of this file.
 * 
 **/
using System;
using System.Collections.Generic;
using System.Text;
using fbs;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using System.Web;
namespace fbs.ImageResizer
{
    /// <summary>
    /// Here is where you can set up custom image size defaults for folders (or any pattern you want). You can also perform URL rewriting on your images.
    /// </summary>
    public static class CustomFolders
    {
        //public delegate string ProcessPath(string virtualPath, NameValueCollection queryString);


        /*
         * Changes from version 1.2
         * 
         * The 1.2 version limited changes to the /resize(w,h,f)/ parameters.
         * This version allows any parameter to be set, and exposes much more flexibility.
         * 
         */
        /// <summary>
        /// Called for all image requests during PostAuthorizeRequest. 
        /// 
        /// Any custom URL syntaxes can be parsed here - just populate 'q' with the resulting data.
        /// 
        /// This method returns the 'real' path of the image, i.e. /app/img/file.jpg will be returned for /app/img/resize(50,50,jpg)/file.jpg
        /// 
        /// Should be very fast - don't make any I/O or database calls here.
        /// AllowURLRewriting must be enabled if you want to return something other than 'filePath'. You can still populate the querystring without this setting.
        /// AllowURLRewriting is required for the /resize(50,50,jpg)/ syntax. 
        /// 
        /// This is the only method the rest of the the image resizer touches. If you don't care for the applyDefaults(), applyOverrides(), and resize(w,h,f) syntax,
        /// feel free to delete everything else in this class and provide your own implementation of this method.
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        public static string processPath(string filePath, NameValueCollection q)
        {
            
            HttpContext context = HttpContext.Current;

            //At any time here you can  configure or disable client caching.
            //context.Items["ContentExpires"] = DateTime.Now.AddHours(5);
            //or context.Items["ContentExpires"] = null

            //q is just a copy of  context.Request.QueryString, but if you set  app.Context.Request.QueryString directly they won't get picked up.
            //filePath us just a copy of context.Request.Path. Feel free to read and any of the other Request properties if you need to.
            //Remember, though, you must return the *new* filePath, not set it in Request.

            NameValueCollection copy = new NameValueCollection(q); //Copy so we can later overwrite q with the original values.

            filePath = CustomFolders.applyDefaults(filePath, q); //Set defaults

            //Overwrite with querystring values again - this is what makes applyDefaults applyDefaults, vs. being applyOverrides.
            foreach (string k in copy)
                q[k] = copy[k];

            filePath = CustomFolders.applyOverrides(filePath, q); //Set overrides

            return filePath;

        }

        /// <summary>
        /// Settings inserted here are overridden by the querystring values. (q gets overwritten with a copy of the original querystring, so only untouched parameters can be changed)
        /// q is populated with the original querystring values to start.
        /// </summary>
        /// <param name="filePath">The virtual domain-relative path (/app/folder/file.jpg). Doesn't include the querystring.</param>
        private static string applyDefaults(string filePath, NameValueCollection q)
        {
            if (filePath.ToLowerInvariant().Contains(".psd.")) q["useresizingpipeline"] = "true";


            //Parse and remove the resize folder syntax from the URL. InterceptModule enforces AllowURLRewriting setting - we don't deal with it here.
            string path = parseResizeFolderSyntax(filePath, q);
            
            /* In version 1.2, we suggested this:
            //> You can make certain folders default to certain dimensions.
            //> return imagePath.Replace("/productThumbnails/","/resize(50,50)/productThumbnails/");
             * 
             * This no longer works.
             * 
             * The new way to accomplish this is:
             * if (path.IndexOf("/productThumbnails/",0, StringComparison.OrdinalIgnoreCase) > -1){
             *      q["maxwidth"] = "50";
             *      q["maxheight"] = "50";
             * }
             * 
             * For convenience, we've made it a method, so transitioning should be easy.
             * 
             * resizeMatch(path,"/productThumbnails/",q,50,50,null);
             * 
             */

            //You can also configure or disable client caching.
            //context.Items["ContentExpires"] = DateTime.Now.AddHours(5);
            //or context.Items["ContentExpires"] = null

            return path;
        }
        /// <summary>
        /// Settings inserted here override the querystring values.
        /// </summary>
        /// <param name="filePath">The virtual domain-relative path (/app/folder/file.jpg). Doesn't include the querystring.</param>
        private static string applyOverrides(string filePath, NameValueCollection q)
        {
            //Like applyDefaults, except q won't get overwriten with the original querystring again.

            

            return filePath;
        }

        /********************************
         * The remainder of this file contains sample code for implementing the /resize(w,h,f)/ syntax.
         * Feel free to modify and derive your own syntaxes.
         **/

        /// <summary>
        /// If substring is found within 'path', q["maxwidth"], q["maxheight"], and q["format"] are set to the specified values.
        /// Use -1 or null to omit a value.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="substring"></param>
        /// <param name="q"></param>
        /// <param name="maxwidth"></param>
        /// <param name="maxheight"></param>
        /// <param name="format"></param>
        private static void resizeMatch(string path, string substring, NameValueCollection q, int maxwidth, int maxheight, string format){
            if (path.IndexOf(substring,0, StringComparison.OrdinalIgnoreCase) > -1){
                if (maxheight > 0) q["maxheight"] = maxheight.ToString();
                if (maxwidth > 0) q["maxwidth"] = maxwidth.ToString();
                if (format != null) q["format"] = format;
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
        /// <param name="query">The collection to place parsed values into</param>
        /// <returns></returns>
        private static string parseResizeFolderSyntax(string path, NameValueCollection q)
        {
            Match m = resizeFolder.Match(path);
            if (m.Success)
            {
                //Parse capture groups
                int maxwidth = -1; int.TryParse(m.Groups["maxwidth"].Value, out maxwidth);
                int maxheight = -1; int.TryParse(m.Groups["maxheight"].Value, out maxheight);
                string format = (m.Groups["format"].Captures.Count > 0) ? format = m.Groups["format"].Captures[0].Value : null;
                
                //Remove first resize folder from URL
                path = resizeFolder.Replace(path, "",1);

                //Add values to querystring
                if (maxwidth > 0) q["maxwidth"] = maxwidth.ToString();
                if (maxheight > 0) q["maxheight"] = maxheight.ToString();
                if (format != null) q["format"] = format;

                //Call recursive - this handles multiple /resize(w,h)/resize(w,h)/ occurrences
                return parseResizeFolderSyntax(path, q);
            }

            return path;
        }

    }
}
