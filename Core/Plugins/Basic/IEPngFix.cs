using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
using ImageResizer.ExtensionMethods;
using ImageResizer.Configuration;
using ImageResizer.Encoding;
using ImageResizer.Util;

namespace ImageResizer.Plugins.Basic {
    /// <summary>
    /// Causes IE6 and earlier to use GIF versions of PNG files. 
    /// By default, only fixes requests with ?iefix=true in the querystring. 
    /// When catchall is enabled, it will filter all png images, unless iefix=false on those requests.
    /// Not compatible with CDNs or proxy servers, as they do not allow varying by user agent reliably.
    /// </summary>
    public class IEPngFix :IPlugin, IQuerystringPlugin {
        public IEPngFix() {
        }
        public IEPngFix(NameValueCollection settings) {
            CatchAll = settings.Get("catchAll", CatchAll);
            Redirect = settings.Get("redirect", Redirect);
        }


        private bool _catchAll = false;
        /// <summary>
        /// If true, 'iefix=true' will be the default for all PNG images, instead of 'iefix=false'.
        /// </summary>
        public bool CatchAll {
            get { return _catchAll; }
            set { _catchAll = value; }
        }


        private bool _redirect = true;
        /// <summary>
        /// If true, the requests from IE will be HTTP redirected to new URLs. If false, the GIF will be silently served instead of the PNG, without any redirection.
        /// A CDN or caching proxy will mess things up regardless, but using redirection ensures that the CDN/proxy never caches the GIF version instead of the PNG.
        /// </summary>
        public bool Redirect {
            get { return _redirect; }
            set { _redirect = value; }
        }

        Config c;
        public IPlugin Install(Configuration.Config c) {
            this.c = c;
            c.Plugins.add_plugin(this);
            c.Pipeline.RewriteDefaults += Pipeline_RewriteDefaults;
            c.Pipeline.Rewrite += Pipeline_Rewrite;
            c.Pipeline.PostRewrite +=Pipeline_PostRewrite;
            return this;
        }



        void Pipeline_Rewrite(IHttpModule sender, HttpContext context, Configuration.IUrlEventArgs e) {
            //Remove 'iefix' immediately if it's not relevant so we don't slow down static requests or do needless re-encoding.
            if (!NeedsPngFix(context)) e.QueryString.Remove("iefix");
        }

        void Pipeline_RewriteDefaults(IHttpModule sender, HttpContext context, Configuration.IUrlEventArgs e) {
            //If this is IE 6 or earlier, and catchall is enabled, add iefix=true to all requests, regardless of (current) file type. 
            //Rewriting rules may change the format or add it later, we'll filter to just PNG requests in PostRewrite.
            if (NeedsPngFix(context) && CatchAll) {
                e.QueryString["iefix"] = "true"; //If CatchAll is enabled, it's a png, and we set the default value here.
            }
        }

        void Pipeline_PostRewrite(IHttpModule sender, HttpContext context, Configuration.IUrlEventArgs e) {
            if (e.QueryString.Get("iefix",false) && NeedsPngFix(context) && DestFormatPng(e)){
                if (Redirect) {
                    //Get the original request URL, and change the 'format' setting to 'gif'. 
                    NameValueCollection newValues = new NameValueCollection();
                    newValues["format"] = "gif";
                    context.Response.Redirect(PathUtils.MergeOverwriteQueryString(context.Request.RawUrl, newValues), true);
                } else {
                    e.QueryString["format"] = "gif";
                }
            }
            e.QueryString.Remove("iefix");
        }

        /// <summary>
        /// Returns true if the specified querystring and file will cause a PNG file to be returned.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool DestFormatPng(IUrlEventArgs e) {
            IEncoder guessedEncoder = c.Plugins.GetEncoder(new ResizeSettings(e.QueryString), e.VirtualPath);
            return  (guessedEncoder != null && "image/png".Equals(guessedEncoder.MimeType, StringComparison.OrdinalIgnoreCase));
        }


        public bool Uninstall(Configuration.Config c) {
            c.Pipeline.RewriteDefaults -= Pipeline_RewriteDefaults;
            c.Pipeline.Rewrite -= Pipeline_Rewrite;
            c.Pipeline.PostRewrite -= Pipeline_PostRewrite;
            c.Plugins.remove_plugin(this);
            return true;
        }

        private static Regex ie456 = new Regex("\\sMSIE\\s[123456]\\.[0-9a-zA-Z]+;\\sWindows", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        /// <summary>
        /// Returns true if the user agent string specifies MSIE versions 1 through 6 for Windows. Cached in context.Items after first call.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static bool NeedsPngFix(HttpContext context) {

            if (context == null || context.Request == null || string.IsNullOrEmpty(context.Request.UserAgent)) return false;
            if (context.Items["isIE6orlower"] == null) {
                bool needsFix = ie456.IsMatch(context.Request.UserAgent);
                context.Items["isIE6orlower"] = needsFix;
                return needsFix;
            }
            return (bool)context.Items["isIE6orlower"];
        }

        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "iefix" };
        }
    }
}
