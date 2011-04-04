using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration;
using ImageResizer.Plugins;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using ImageResizer.Caching;
using ImageResizer.Configuration.Issues;

namespace ImageResizer.Configuration {
    public class PipelineConfig : IPipelineConfig, ICacheProvider{
        protected Config c;
        public PipelineConfig(Config c) {
            this.c = c;

            c.Plugins.QuerystringPlugins.Changed += new SafeList<IQuerystringPlugin>.ChangedHandler(urlModifyingPlugins_Changed);
            
        }

        protected void urlModifyingPlugins_Changed(SafeList<IQuerystringPlugin> sender) {
            lock (_cachedUrlDataSync) {
                _cachedDirectives = null;
                _cachedExtensions = null;
            }
        }


        protected object _cachedUrlDataSync = new object();
        protected volatile Dictionary<string, bool> _cachedDirectives = null;
        protected volatile Dictionary<string, bool> _cachedExtensions = null;

        /// <summary>
        /// Populates the cache if it is empty. Not thread safe.
        /// </summary>
        protected void _cacheUrlData() {
            if (_cachedDirectives != null && _cachedExtensions != null) return;

            Dictionary<string, bool> directives = new Dictionary<string, bool>(48,StringComparer.OrdinalIgnoreCase);
            Dictionary<string, bool> exts = new Dictionary<string, bool>(24,StringComparer.OrdinalIgnoreCase);
            IEnumerable<string> vals = null;
            //Check the plugins
            foreach (IQuerystringPlugin p in c.Plugins.QuerystringPlugins) {

                vals = p.GetSupportedQuerystringKeys();
                if (vals != null)
                    foreach (string s in vals)
                        directives[s] = true;
            }
            foreach (IFileExtensionPlugin p in c.Plugins.FileExtensionPlugins) {
                vals = p.GetSupportedFileExtensions();
                if (vals != null)
                    foreach (string e in vals)
                        exts[e.TrimStart('.')] = true;
            }


            //Now check the imagebuider instance
            ImageBuilder b = c.CurrentImageBuilder;
            if (b != null) {
                vals = b.GetSupportedFileExtensions();
                if (vals != null)
                    foreach (string e in vals)
                        exts[e.TrimStart('.')] = true;

                vals = b.GetSupportedQuerystringKeys();
                if (vals != null)
                    foreach (string s in vals)
                        directives[s] = true;
            }

            _cachedDirectives = directives;
            _cachedExtensions = exts;
            
        }
        protected Dictionary<string, bool> getCachedDirectives(){
            lock (_cachedUrlDataSync) {
                _cacheUrlData();
                return _cachedDirectives;
            }
        }
        protected Dictionary<string, bool> getCachedExtensions(){
            lock (_cachedUrlDataSync) {
                _cacheUrlData();
                return _cachedExtensions;
            }
        }
        /// <summary>
        /// Returns a unqiue copy of the image extensions supported by the pipeline. Performs a cached query to all registered IQuerystringPlugin instances.
        /// </summary>
        public ICollection<string> AcceptedImageExtensions {
            get {
                return new List<string>(getCachedExtensions().Keys);
            }
        }
        /// <summary>
        /// Returns a unqiue copy of all querystring keys supported by the pipeline. Performs a cached query to all registered IQuerystringPlugin instances.
        /// </summary>
        public ICollection<string> SupportedQuerystringKeys {
            get {
                return new List<string>(getCachedDirectives().Keys);
            }
        }

        /// <summary>
        /// The specified path must not include a querystring. Slashes, spaces, question marks, ampersands, and colons are not permitted in the extension.
        /// If it contains a multipart extension like .txt.zip, only "zip" will be recognized.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool IsAcceptedImageType(string path) {
            //Trim off the extension
            int lastDot = path.LastIndexOfAny(new char[]{'.','/', ' ','\\','?','&',':'});
            if (lastDot > -1 && path[lastDot] == '.') path = path.Substring(lastDot + 1);

            return getCachedExtensions().ContainsKey(path);
        }
        /// <summary>
        /// Returns true if any of the querystring keys match any of the directives supported by the pipeline (such as width, height, format, bgcolor, etc)
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public bool HasPipelineDirective(System.Collections.Specialized.NameValueCollection q) {
            Dictionary<string, bool> dirs = getCachedDirectives();
            //The querystring always has fewer items than the cachedDirectives, so loop it instead.
            foreach (string key in q.Keys) {
                if (dirs.ContainsKey(key)) return true; //Binary search, hashtable impl
            }
            return false;
        }


        public string FakeExtension {
            get { return c.get("pipeline.fakeExtension",".ashx"); }
        }

        public string ModifiedQueryStringKey {
            get { return "resizer.modifiedQueryString"; }
        }

        public string ResponseArgsKey {
            get { return "resizer.cacheArgs"; }
        }

        public VppUsageOption VppUsage {
            get {
                string value = c.get("pipeline.vppUsage", null);
                if (value == null) return VppUsageOption.Fallback;
                try {
                    return (VppUsageOption)Enum.Parse(typeof(VppUsageOption), value, true);
                } catch (ArgumentException ae) {
                    c.configurationSectionIssues.AcceptIssue(new Issue("Failed to parse pipleine.vppUsage. Invalid value \"" + value + "\".", IssueSeverity.ConfigurationError));
                    return VppUsageOption.Fallback;
                }
            }
        }


        public ImageBuilder GetImageBuilder() {
            return c.CurrentImageBuilder;
        }

        public ICacheProvider GetCacheProvider() {
            return this;
        }

        /// <summary>
        /// Fired once, on the first PostAuthorizeRequest event.
        /// </summary>
        public event RequestHook OnFirstRequest;
        /// <summary>
        /// Fires during the PostAuthorizeRequest phase, prior to any module-specific logic.
        /// Executes for every request to the website. Use only as a last resort. Other events occur only for image requests, and thus have lower overhead.
        /// </summary>
        public event RequestHook PostAuthorizeRequestStart;

        /// <summary>
        /// Fired during PostAuthorizeRequest, after ResizeExtension has been removed.
        /// On fired on requests with extensions that match supported image types.
        /// <para> 
        /// You can add additonal supported image extentions by registering a plugin that implementes IQuerystringPlugin, or you can add an 
        /// extra extension in the URL and remove it here. Example: .psd.jpg</para>
        /// </summary>
        public event UrlRewritingHook Rewrite;
        /// <summary>
        /// Fired during PostAuthorizeRequest, after Rewrite.
        /// Any changes made here (which conflict) will be overwritten by the the current querystring values. I.e, this is a good place to specify default settings.
        /// <para>Only fired on accepted image types. (see Rewrite)</para>
        /// </summary>
        public event UrlRewritingHook RewriteDefaults;
        /// <summary>
        /// Fired after all other rewrite events.
        /// <para>Only fired on accepted image types. (see Rewrite)</para>
        /// </summary>
        public event UrlRewritingHook PostRewrite;
        /// <summary>
        /// Fired after all rewriting should be finished, and the secondary UrlAuthorization has been completed. Plugins wanting to add additional authorization rules can implement them in a handler,
        /// and modify the response accordingly.
        /// </summary>
        public event UrlRewritingHook PostAuthorizeImage;

        /// <summary>
        /// Fired immediately before the image request is sent off to the caching system for proccessing.
        /// Allows modification of response headers, caching arguments, and callbacks.
        /// </summary>
        public event PreHandleImageHook PreHandleImage;

        public event CacheSelectionDelegate SelectCachingSystem;

        protected volatile bool firedFirstRequest = false;
        protected object firedFirstRequestSync = new object();

        public void FirePostAuthorizeRequest(System.Web.IHttpModule sender, System.Web.HttpContext httpContext) {
            //The one-time event
            if (!firedFirstRequest) {
                lock (firedFirstRequestSync) {
                    if (!firedFirstRequest) {
                        firedFirstRequest = true;
                        if (OnFirstRequest != null) OnFirstRequest(sender, httpContext);
                    }
                }
            }
            //And the main event
            if (PostAuthorizeRequestStart != null) PostAuthorizeRequestStart(sender, httpContext);
                   
        }

        public void FireRewritingEvents(System.Web.IHttpModule sender, System.Web.HttpContext context, IUrlEventArgs e) {
            //Fire first event (results will stay in e)
            if (Rewrite != null) Rewrite(sender,context, e);

            //Copy querystring for use in 'defaults' even
            NameValueCollection copy = new NameValueCollection(e.QueryString); //Copy so we can later overwrite q with the original values.

            //Fire defaults event.
            if (RewriteDefaults != null) RewriteDefaults(sender, context, e);

            //Overwrite with querystring values again - this is what makes applyDefaults applyDefaults, vs. being applyOverrides.
            foreach (string k in copy)
                e.QueryString[k] = copy[k];
            
            //Fire final event
            if (PostRewrite != null) PostRewrite(sender,context, e);
        }

        public void FirePostAuthorizeImage(System.Web.IHttpModule sender, System.Web.HttpContext context, IUrlEventArgs e) {
            if (PostAuthorizeImage != null) PostAuthorizeImage(sender, context, e);
        }

        public void FirePreHandleImage(System.Web.IHttpModule sender, System.Web.HttpContext context, IResponseArgs e) {
            if (PreHandleImage != null) PreHandleImage(sender, context, e);
        }


        public ICache GetCachingSystem(System.Web.HttpContext context, IResponseArgs args) {
            ICache defaultCache = null;
            //Grab the first cache that claims it can process the request.
            foreach (ICache cache in c.Plugins.CachingSystems) {
                if (cache.CanProcess(context, args)) {
                    defaultCache = cache;
                    break;
                }
            }

            CacheSelectionEventArgs e = new CacheSelectionEventArgs(context, args, defaultCache);
            if (SelectCachingSystem != null) SelectCachingSystem(this, e);
            return e.SelectedCache;
        }
    }
}
