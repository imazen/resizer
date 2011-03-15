using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Configuration;

namespace fbs.ImageResizer {
    public class UrlEventArgs : EventArgs {
        protected string _virtualPath;
        protected NameValueCollection _queryString;

        public UrlEventArgs(string virtualPath, NameValueCollection queryString) {
            this._virtualPath = virtualPath;
            this._queryString = queryString;
        }

        public NameValueCollection QueryString {
            get { return _queryString; }
            set { _queryString = value; }
        }
        public string VirtualPath {
            get { return _virtualPath; }
            set { _virtualPath = value; }
        }
    }

    public delegate void UrlRewritingHook(InterceptModule sender, UrlEventArgs e);

    /// <summary>
    /// Allows run-time modification of image resizer settings, and modification of pipeline behavior through events
    /// </summary>
    public static class Configuration {

        private static ResizerConfigurationSection configuration;
        private static object configurationLock = new object();
        /// <summary>
        /// The ResizeConfigrationSection is not thread safe, and should not be modified
        /// Dynamically loads the ConfigurationSection from web.config when accessed for the first time. 
        /// </summary>
        protected static ResizerConfigurationSection cs {
            get {
                if (configuration == null) {
                    lock (configurationLock) {
                        if (configuration == null) {
                            ResizerConfigurationSection tmpConf = (ResizerConfigurationSection)ConfigurationManager.GetSection("imageresizer");
                            configuration = tmpConf;
                        }
                    }
                }
                return configuration;
            }
        }

        
        
        public static string get(string selector, string defaultValue){
            return cs.get(selector,defaultValue);   
        }
        public static int get(string selector, int defaultValue) {
            int i;
            string s = cs.get(selector, defaultValue.ToString());
            if (int.TryParse(s, out i)) return i;
            else throw new ConfigurationException("Error in imageresizer configuration section: Invalid integer at " + selector + ":" + s);
        }

        public static bool get(string selector, bool defaultValue) {
            string s = cs.get(selector, defaultValue.ToString());

            if ("true".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                "1".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                "yes".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                "on".Equals(s, StringComparison.OrdinalIgnoreCase)) return true;
            else if ("false".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                "0".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                "no".Equals(s, StringComparison.OrdinalIgnoreCase) ||
                "off".Equals(s, StringComparison.OrdinalIgnoreCase)) return false;
            else throw new ConfigurationException("Error in imageresizer configuration section: Invalid boolean at " + selector + ":" + s);
        }

        /// <summary>
        /// Fired during PostAuthorizeRequest, after ResizeExtension has been removed.
        /// Fired before CustomFolders and other URL rewriting events.
        /// <para>Only fired on accepted image types: ImageOutputSettings.IsAcceptedImageType(). 
        /// You can add acceptable image extentions using ImageOutputSettings, or you can add an 
        /// extra extension in the URL and remove it here. Example: .psd.jpg</para>
        /// </summary>
        public static event UrlRewritingHook Rewrite;
        /// <summary>
        /// Fired during PostAuthorizeRequest, after Rewrite.
        /// Any changes made here (which conflict) will be overwritten by the the current querystring values. I.e, this is a good place to specify default settings.
        /// <para>Only fired on accepted image types: ImageOutputSettings.IsAcceptedImageType()</para>
        /// </summary>
        public static event UrlRewritingHook RewriteDefaults;
        /// <summary>
        /// Fired after all other rewrite events and CustomFolders.cs 
        /// <para>Only fired on accepted image types: ImageOutputSettings.IsAcceptedImageType()</para>
        /// </summary>
        public static event UrlRewritingHook PostRewrite;

       

        /// <summary>
        /// Loads all settings from the Web.config file.
        /// </summary>
        public static void LoadSettingsFromWebConfig() {
            FakeExtension = cs["rewriting.fakeExtension"];

        }

        private static string FakeExtension = ".ashx";

        
        public static VppUsageOption VppUsage;
        public static float MaxWidth;
        public static float MaxHeight;


        /// <summary>
        /// Fires URL rewriting event in order, collecting the result in 'e'
        /// </summary>
        /// <param name="e"></param>
        public static void FireRewritingEvents(InterceptModule sender, UrlEventArgs e) {
            //Fire first event (results will stay in e)
            if (Rewrite != null) Rewrite(sender, e);

            //Copy querystring for use in 'defaults' even
            NameValueCollection copy = new NameValueCollection(e.QueryString); //Copy so we can later overwrite q with the original values.

            //Fire defaults event.
            if (RewriteDefaults != null) RewriteDefaults(sender, e);

            //Overwrite with querystring values again - this is what makes applyDefaults applyDefaults, vs. being applyOverrides.
            foreach (string k in copy)
                e.QueryString[k] = copy[k];

            //Fire final event
            if (PostRewrite != null) PostRewrite(sender, e);
        }






        internal static bool HasResizingDirective(NameValueCollection q) {
            throw new NotImplementedException();
        }

        internal static bool IsAcceptedImageType(string filePath) {
            throw new NotImplementedException();
        }

        internal static void FirePostAuthorize(InterceptModule interceptModule, UrlEventArgs urlEventArgs) {
            throw new NotImplementedException();
        }

        internal static void FirePreHeaders(System.Web.HttpContext context) {
            throw new NotImplementedException();
        }

        internal static void FirePostHeaders(System.Web.HttpContext context) {
            throw new NotImplementedException();
        }

        internal static ICache GetCachingModule(System.Web.HttpContext context, yrl current) {
            throw new NotImplementedException();
        }
    }
}
