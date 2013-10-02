﻿/* Copyright (c) 2011 Nathanael Jones. See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration;
using ImageResizer.Plugins;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using ImageResizer.Caching;
using ImageResizer.Configuration.Issues;
using ImageResizer.Encoding;
using System.Web.Hosting;
using ImageResizer.Collections;
using System.Web;
using System.Security.Permissions;
using System.Runtime.InteropServices;

namespace ImageResizer.Configuration {
    public class PipelineConfig : IPipelineConfig, ICacheProvider, ISettingsModifier{
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

		[CLSCompliant(false)]
        protected volatile Dictionary<string, bool> _cachedDirectives = null;

		[CLSCompliant(false)]
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
        /// Use IsAcceptedImageType for better performance.
        /// </summary>
        public ICollection<string> AcceptedImageExtensions {
            get {
                return new List<string>(getCachedExtensions().Keys);
            }
        }
        /// <summary>
        /// Returns a unqiue copy of all querystring keys supported by the pipeline. Performs a cached query to all registered IQuerystringPlugin instances.
        /// Use HasPipelineDirective for better performance. (binary search)
        /// </summary>
        public ICollection<string> SupportedQuerystringKeys {
            get {
                return new List<string>(getCachedDirectives().Keys);
            }
        }

        protected string getExtension(string path) {
            //Trim off the extension
            int lastDot = path.LastIndexOfAny(new char[] { '.', '/', ' ', '\\', '?', '&', ':' });
            if (lastDot > -1 && path[lastDot] == '.') return path.Substring(lastDot + 1);
            else return null;
        }

        /// <summary>
        /// The specified path must not include a querystring. Slashes, spaces, question marks, ampersands, and colons are not permitted in the extension.
        /// If it contains a multipart extension like .txt.zip, only "zip" will be recognized. Must contain a dot - passing in "jpg" will fail.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool IsAcceptedImageType(string path) {
            string ext = getExtension(path);
            if (string.IsNullOrEmpty(ext)) return false;
            return getCachedExtensions().ContainsKey(ext);
        }
        /// <summary>
        /// Returns true if any of the querystring keys match any of the directives supported by the pipeline (such as width, height, format, bgcolor, etc)
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public bool HasPipelineDirective(System.Collections.Specialized.NameValueCollection q) {
            //Did you know that ASP.NET puts null keys into the QueryString?
            Dictionary<string, bool> dirs = getCachedDirectives();
            //The querystring always has fewer items than the cachedDirectives, so loop it instead.
            foreach (string key in q.Keys) {
                if (key != null && dirs.ContainsKey(key)) return true; //Binary search, hashtable impl
            }
            return false;
        }

		[CLSCompliant(false)]
        protected volatile IList<string> _fakeExtensions = null;

        /// <summary>
        /// Cached access to pipeline.fakeExtensions
        /// </summary>
        public IList<string> FakeExtensions {
            get {
                IList<string> temp = _fakeExtensions;
                if (temp != null) return temp;
                else temp = new List<string>(c.get("pipeline.fakeExtensions",".ashx").Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries));
                for (int i = 0; i < temp.Count; i++) {
                    if (!temp[i].StartsWith(".", StringComparison.OrdinalIgnoreCase)) temp[i] = "." + temp[i];
                }
                _fakeExtensions = temp;
                return temp;
            }
        }
        /// <summary>
        /// Removes the first fake extensionm detected at the end of 'path'
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string TrimFakeExtensions(string path) {
            foreach (string s in FakeExtensions) {
                if (path.EndsWith(s, StringComparison.OrdinalIgnoreCase)) {
                    path = path.Substring(0, path.Length - s.Length).TrimEnd('.');
                    break;
                }
            }
            return path;
        }

        public string ModifiedPathKey {
            get { return "resizer.newPath"; }
        }

        public string StopRoutingKey {
            get { return "resizer.stopRouting"; }
        }
        /// <summary>
        /// Returns the value of Context.Items["resizer.newPath"] if present. If not, returns FilePath + PathInfo.
        /// Sets Context.Items["resizer.newPath"]. 
        /// Only useful during the Pipeline.PostAuthorizeRequestStart event.
        /// </summary>
        public string PreRewritePath {
            get {
                if (HttpContext.Current == null) return null;
                return HttpContext.Current.Items[ModifiedPathKey] != null ? HttpContext.Current.Items[ModifiedPathKey] as string :
                    (HttpContext.Current.Request.FilePath + HttpContext.Current.Request.PathInfo);

            }
            set {
                HttpContext.Current.Items[ModifiedPathKey] = value;
            }
        }

        public string ModifiedQueryStringKey {
            get { return "resizer.modifiedQueryString"; }
        }

        
        /// <summary>
        /// Returns the modified query string. If never set, returns a copy of Request.QueryString.
        /// Returns the same instance if called multiple times. Copy it if you want to make changes without causing issues.
        /// </summary>
        public NameValueCollection ModifiedQueryString {
            get {
                if (HttpContext.Current == null) return null;
                if (HttpContext.Current.Items[ModifiedQueryStringKey] == null)
                    HttpContext.Current.Items[ModifiedQueryStringKey] = new NameValueCollection(HttpContext.Current.Request.QueryString);

                return (NameValueCollection)HttpContext.Current.Items[ModifiedQueryStringKey];
            }
            set {
                HttpContext.Current.Items[ModifiedQueryStringKey] = value;
            }
        }


        public string SkipFileTypeCheckKey { get { return "resizer.skipFileTypeCheck"; } }
        /// <summary>
        /// Get or sets whether the file extension check should be applied to the current request. Defaults to true.
        /// If set to true, will only affect the current request, and will only cause the Resizer to evaluate the rewriting rules on the request.
        /// Processing may still not occur if no querystring values are specified. Add 'cache=always' to force caching to occur.
        /// </summary>
        public bool SkipFileTypeCheck {
            get { return HttpContext.Current != null && HttpContext.Current.Items[SkipFileTypeCheckKey] != null && (bool)HttpContext.Current.Items[SkipFileTypeCheckKey]; }
            set { HttpContext.Current.Items[SkipFileTypeCheckKey] = value; }
        }



        public string ResponseArgsKey {
            get { return "resizer.cacheArgs"; }
        }
        /// <summary>
        /// Returns true if the current request is being processed and/or cached by the pipeline.
        /// Will return false until *after* the FileExists method is called on the VirtualPathProviders, which is after the 
        /// AuthorizeImage event fires. 
        /// This will return a usable value if used from VirtualFile.Open(), or if used inside the PreHandleImage event or later.
        /// </summary>
        public bool IsHandlingRequest {
            get { return (System.Web.HttpContext.Current != null && System.Web.HttpContext.Current.Items[ResponseArgsKey] != null); }
        }
        

        public VppUsageOption VppUsage {
            get {
                return c.get<VppUsageOption>("pipeline.vppUsage", VppUsageOption.Fallback);
            }
        }

        /// <summary>
        /// Returns either an IVirtualFile instance or a VirtualFile instance.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public IVirtualFile GetFile(string virtualPath, NameValueCollection queryString) {
            IVirtualFile f = null;
            foreach (IVirtualImageProvider p in c.Plugins.VirtualProviderPlugins) {
                if (p.FileExists(virtualPath, queryString)) { f = p.GetFile(virtualPath, queryString); break; }
            }
            if (f == null &&  HostingEnvironment.VirtualPathProvider != null && HostingEnvironment.VirtualPathProvider.FileExists(virtualPath))
            {
                var vf = HostingEnvironment.VirtualPathProvider.GetFile(virtualPath);
                if (vf is IVirtualFile) f = (IVirtualFile)vf;
                else f = new VirtualFileWrapper(vf);
            }
            if (f == null) return null;

            //Now we have a reference to the real virtual file, let's see if it is source-cached.
            IVirtualFile cached = null;
            foreach (IVirtualFileCache p in c.Plugins.GetAll<IVirtualFileCache>()) {
                cached = p.GetFileIfCached(virtualPath,queryString,f);
                if (cached != null) return cached;
            }
            return f;
        }

        /// <summary>
        /// Returns true if (a) A registered IVirtualImageProvider says it exists, or (b) if the VirtualPathProvider chain says it exists.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public bool FileExists(string virtualPath, NameValueCollection queryString) {
            foreach (IVirtualImageProvider p in c.Plugins.VirtualProviderPlugins) {
                if (p.FileExists(virtualPath, queryString)) return true;
            }
            return HostingEnvironment.VirtualPathProvider != null ? HostingEnvironment.VirtualPathProvider.FileExists(virtualPath) : false;
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
        public event RequestEventHandler OnFirstRequest;
        /// <summary>
        /// Fires during the PostAuthorizeRequest phase, prior to any module-specific logic.
        /// Executes for every request to the website. Use only as a last resort. Other events occur only for image requests, and thus have lower overhead.
        /// </summary>
        public event RequestEventHandler PostAuthorizeRequestStart;

        /// <summary>
        /// Fired during PostAuthorizeRequest, after ResizeExtension has been removed.
        /// On fired on requests with extensions that match supported image types.
        /// <para> 
        /// You can add additonal supported image extentions by registering a plugin that implementes IQuerystringPlugin, or you can add an 
        /// extra extension in the URL and remove it here. Example: .psd.jpg</para>
        /// </summary>
        public event UrlRewritingEventHandler Rewrite;
        /// <summary>
        /// Fired during PostAuthorizeRequest, after Rewrite.
        /// Any changes made here (which conflict) will be overwritten by the the current querystring values. I.e, this is a good place to specify default settings.
        /// <para>Only fired on accepted image types. (see Rewrite)</para>
        /// </summary>
        public event UrlRewritingEventHandler RewriteDefaults;
        /// <summary>
        /// Fired after all other rewrite events.
        /// <para>Only fired on accepted image types. (see Rewrite)</para>
        /// </summary>
        public event UrlRewritingEventHandler PostRewrite;
        /// <summary>
        /// Fired after all rewriting is finished.
        /// e.AllowAccess defaults to the result of the UrlAuthorization module's verdict. It can be changed. 
        /// Set e.AllowAccess to true to cause and 403 Access Dened result.
        /// </summary>
        public event UrlAuthorizationEventHandler AuthorizeImage;


        /// <summary>
        /// Fired when the specified image doesn't exist. Only called for images that would normally be processed.
        /// May be called during PostAuthorizeRequest or later - End the request completely with a redirect if you want alternate behavior.
        /// </summary>
        public event UrlEventHandler ImageMissing;

        /// <summary>
        /// Fired immediately before the image request is sent off to the caching system for proccessing.
        /// Allows modification of response headers, caching arguments, and callbacks.
        /// </summary>
        public event PreHandleImageEventHandler PreHandleImage;

        public event CacheSelectionHandler SelectCachingSystem;

		[CLSCompliant(false)]
        protected volatile bool firedFirstRequest = false;

        protected volatile bool firstRequestFinished = false;

        protected object firedFirstRequestSync = new object();

        public void FirePostAuthorizeRequest(System.Web.IHttpModule sender, System.Web.HttpContext httpContext) {
            //The one-time event
            if (!firstRequestFinished) {
                lock (firedFirstRequestSync) {
                    if (!firedFirstRequest) {
                        firedFirstRequest = true;
                        if (OnFirstRequest != null) OnFirstRequest(sender, httpContext);
                        firstRequestFinished = true;
                    }
                }
            }
            //And the main event
            if (PostAuthorizeRequestStart != null) PostAuthorizeRequestStart(sender, httpContext);
                   
        }


        public void FireRewritingEvents(System.Web.IHttpModule sender, System.Web.HttpContext context, IUrlEventArgs e) {

            //TODO: this approach is non-intuitive....

            //Fire first event (results will stay in e)
            if (Rewrite != null) Rewrite(sender,context, e);

            //Copy querystring for use in 'defaults' even
            NameValueCollection copy = new NameValueCollection(e.QueryString); //Copy so we can later overwrite q with the rewrite values.

            //Fire defaults event.
            if (RewriteDefaults != null) RewriteDefaults(sender, context, e);

            //Overwrite with querystring values again - this is what makes applyDefaults applyDefaults, vs. being applyOverrides.
            foreach (string k in copy) {
                if (copy[k] != null) { //Don't allow null values to override defaults. Empty values can, however.
                    e.QueryString[k] = copy[k];
                }
            }
            
            //Fire final event
            if (PostRewrite != null) PostRewrite(sender,context, e);
        }

        public void FireAuthorizeImage(System.Web.IHttpModule sender, System.Web.HttpContext context, IUrlAuthorizationEventArgs e) {
            if (AuthorizeImage != null) AuthorizeImage(sender, context, e);
        }

        public void FireImageMissing(System.Web.IHttpModule sender, System.Web.HttpContext context, IUrlEventArgs e) {
            if (ImageMissing != null) ImageMissing(sender, context, e);
        }

        protected long processedCount = 0;
        /// <summary>
        /// The number of images processed by this pipeline.
        /// </summary>
        public long ProcessedCount {
            get {
                return processedCount;
            }
        }

        public void FirePreHandleImage(System.Web.IHttpModule sender, System.Web.HttpContext context, IResponseArgs e) {
            System.Threading.Interlocked.Increment(ref processedCount);
            if (PreHandleImage != null) PreHandleImage(sender, context, e);
        }

        /// <summary>
        /// Cache selection occurs as follows: (1) The first registered CachingSystem that returns  true from .CanProcess() is the default
        /// (2) The SelectCachingSystem event is fired, allowing handlers to modify the selected cache. 
        /// This method may return null. 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="args"></param>
        /// <returns></returns>
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





        public ResizeSettings Modify(ResizeSettings settings) {
            foreach (ISettingsModifier m in c.Plugins.SettingsModifierPlugins)
                settings = m.Modify(settings);
            return settings;
        }


        private bool _moduleInstalled = false;
        /// <summary>
        /// True once the InterceptModule has been installed and is intercepting requests.
        /// </summary>
        public bool ModuleInstalled {
            get {
                return _moduleInstalled;
            }
            set {
                _moduleInstalled = value;
            }
        }
    }
}
