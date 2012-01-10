using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Text.RegularExpressions;
using System.Collections.Specialized;
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
            CatchAll = Util.Utils.getBool(settings, "catchAll", CatchAll);
        }


        private bool _catchAll = false;
        /// <summary>
        /// If true, 'iefix=true' will be the default for all PNG images, instead of 'iefix=false'.
        /// </summary>
        public bool CatchAll {
            get { return _catchAll; }
            set { _catchAll = value; }
        }

        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            c.Pipeline.PreHandleImage += Pipeline_PreHandleImage;
            c.Pipeline.RewriteDefaults += Pipeline_RewriteDefaults;
            return this;
        }

        void Pipeline_RewriteDefaults(IHttpModule sender, HttpContext context, Configuration.IUrlEventArgs e) {
            if (!NeedsPngFix(context.Request)) return; //Only do stuff with IE 1-6
            if (CatchAll && ".png".Equals(PathUtils.GetExtension(e.VirtualPath))) {
                e.QueryString["iefix"] = "true"; //If CatchAll is enabled, we set the default value here.
            }
        }

        void Pipeline_PreHandleImage(IHttpModule sender, HttpContext context, Caching.IResponseArgs e) {
            if (!NeedsPngFix(context.Request)) return; //Only do stuff with IE 1-6

            //If iefix is enabled and the destination format is 'png', let's change that.
            if (Utils.getBool(e.RewrittenQuerystring,"iefix",false) && "png".Equals(e.SuggestedExtension, StringComparison.OrdinalIgnoreCase)) {
                //Get the original request URL, and change the 'format' setting to 'gif'. 
                NameValueCollection newValues = new NameValueCollection();
                newValues["format"] = "gif";
                context.Response.Redirect(PathUtils.MergeOverwriteQueryString(context.Request.RawUrl,newValues),true);
            }
        }

        public bool Uninstall(Configuration.Config c) {
            c.Pipeline.RewriteDefaults -= Pipeline_RewriteDefaults;
            c.Pipeline.PreHandleImage -= Pipeline_PreHandleImage;
            c.Plugins.remove_plugin(this);
            return true;
        }

        private static Regex ie456 = new Regex("\\sMSIE\\s[123456]\\.[0-9a-zA-Z]+;\\sWindows", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        /// <summary>
        /// Returns true if the user agent string specifies MSIE versions 1 through 6 for Windows.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static bool NeedsPngFix(HttpRequest request) {
            if (request == null || string.IsNullOrEmpty(request.UserAgent)) return false;

            return ie456.IsMatch(request.UserAgent);
        }

        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "iefix" };
        }
    }
}
