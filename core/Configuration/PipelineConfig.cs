// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Permissions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.Hosting;
using ImageResizer.Caching;
using ImageResizer.Collections;
using Imazen.Common.Issues;
using ImageResizer.ExtensionMethods;
using ImageResizer.Plugins;
using Imazen.Common.Storage;

namespace ImageResizer.Configuration
{
    public class PipelineConfig : IPipelineConfig, ISettingsModifier
    {
        protected Config c;

        public PipelineConfig(Config c)
        {
            ModuleInstalled = false;
            this.c = c;

            c.Plugins.QuerystringPlugins.Changed +=
                new SafeList<IQuerystringPlugin>.ChangedHandler(urlModifyingPlugins_Changed);

            new List<string>(c.get("pipeline.fakeExtensions", ".ashx")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));

            pipelineDefaults = new Instructions(c.get("pipeline.defaultCommands", ""));

            var pipelineNode = c.getNode("pipeline");

            if (HasPipelineDirective(pipelineDefaults))
            {
                var intersection = string.Join(", ", PipelineDirectivesPresent(pipelineDefaults));
                c.configurationSectionIssues.AcceptIssue(new Issue(
                    "You have specified default commands (" + intersection +
                    ") that will cause all image requests to be processed; even those that do not need ImageResizer.",
                    pipelineNode != null ? pipelineNode.ToString() : "", IssueSeverity.ConfigurationError));
            }

            if (pipelineDefaults.Count > 0) RewriteDefaults += PipelineConfig_RewriteDefaults;
        }

        private NameValueCollection pipelineDefaults;

        private void PipelineConfig_RewriteDefaults(IHttpModule sender, HttpContext context, IUrlEventArgs e)
        {
            e.QueryString = e.QueryString.MergeDefaults(pipelineDefaults);
        }

        protected void urlModifyingPlugins_Changed(SafeList<IQuerystringPlugin> sender)
        {
            lock (_cachedUrlDataSync)
            {
                _cachedDirectives = null;
                _cachedExtensions = null;
            }
        }


        protected object _cachedUrlDataSync = new object();

        [CLSCompliant(false)] protected volatile Dictionary<string, bool> _cachedDirectives = null;

        [CLSCompliant(false)] protected volatile Dictionary<string, bool> _cachedExtensions = null;

        /// <summary>
        ///     Populates the cache if it is empty. Not thread safe.
        /// </summary>
        protected void _cacheUrlData()
        {
            if (_cachedDirectives != null && _cachedExtensions != null) return;

            var directives = new Dictionary<string, bool>(48, StringComparer.OrdinalIgnoreCase);
            var exts = new Dictionary<string, bool>(24, StringComparer.OrdinalIgnoreCase);
            IEnumerable<string> vals = null;
            //Check the plugins
            foreach (var p in c.Plugins.QuerystringPlugins)
            {
                vals = p.GetSupportedQuerystringKeys();
                if (vals != null)
                    foreach (var s in vals)
                        directives[s] = true;
            }

            foreach (var p in c.Plugins.FileExtensionPlugins)
            {
                vals = p.GetSupportedFileExtensions();
                if (vals != null)
                    foreach (var e in vals)
                        exts[e.TrimStart('.')] = true;
            }


            //Now check the imagebuilder instance
            var b = c.CurrentImageBuilder;
            if (b != null)
            {
                vals = b.GetSupportedFileExtensions();
                if (vals != null)
                    foreach (var e in vals)
                        exts[e.TrimStart('.')] = true;

                vals = b.GetSupportedQuerystringKeys();
                if (vals != null)
                    foreach (var s in vals)
                        directives[s] = true;
            }

            _cachedDirectives = directives;
            _cachedExtensions = exts;
        }

        protected Dictionary<string, bool> getCachedDirectives()
        {
            lock (_cachedUrlDataSync)
            {
                _cacheUrlData();
                return _cachedDirectives;
            }
        }

        protected Dictionary<string, bool> getCachedExtensions()
        {
            lock (_cachedUrlDataSync)
            {
                _cacheUrlData();
                return _cachedExtensions;
            }
        }

        /// <summary>
        ///     Returns a unique copy of the image extensions supported by the pipeline. Performs a cached query to all registered
        ///     IQuerystringPlugin instances.
        ///     Use IsAcceptedImageType for better performance.
        /// </summary>
        public ICollection<string> AcceptedImageExtensions => new List<string>(getCachedExtensions().Keys);

        /// <summary>
        ///     Returns a unique copy of all querystring keys supported by the pipeline. Performs a cached query to all registered
        ///     IQuerystringPlugin instances.
        ///     Use HasPipelineDirective for better performance. (binary search)
        /// </summary>
        public ICollection<string> SupportedQuerystringKeys => new List<string>(getCachedDirectives().Keys);

        protected string getExtension(string path)
        {
            //Trim off the extension
            var lastDot = path.LastIndexOfAny(new[] { '.', '/', ' ', '\\', '?', '&', ':' });
            if (lastDot > -1 && path[lastDot] == '.') return path.Substring(lastDot + 1);
            else return null;
        }

        /// <summary>
        ///     The specified path must not include a querystring. Slashes, spaces, question marks, ampersands, and colons are not
        ///     permitted in the extension.
        ///     If it contains a multipart extension like .txt.zip, only "zip" will be recognized. Must contain a dot - passing in
        ///     "jpg" will fail.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool IsAcceptedImageType(string path)
        {
            var ext = getExtension(path);
            if (string.IsNullOrEmpty(ext)) return false;
            return getCachedExtensions().ContainsKey(ext);
        }

        /// <summary>
        ///     Returns true if any of the querystring keys match any of the directives supported by the pipeline (such as width,
        ///     height, format, bgcolor, etc)
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public bool HasPipelineDirective(NameValueCollection q)
        {
            //Did you know that ASP.NET puts null keys into the QueryString?
            var dirs = getCachedDirectives();
            //The querystring always has fewer items than the cachedDirectives, so loop it instead.
            foreach (string key in q.Keys)
                if (key != null && dirs.ContainsKey(key))
                    return true; //Binary search, hashtable impl
            return false;
        }

        private IEnumerable<string> PipelineDirectivesPresent(NameValueCollection q)
        {
            //Did you know that ASP.NET puts null keys into the QueryString?
            var dirs = getCachedDirectives();
            var intersection = new List<string>();
            //The querystring always has fewer items than the cachedDirectives, so loop it instead.
            foreach (string key in q.Keys)
                if (key != null && dirs.ContainsKey(key))
                    intersection.Add(key); //Binary search, hashtable impl
            return intersection;
        }


        [CLSCompliant(false)] protected volatile IList<string> _fakeExtensions = null;

        /// <summary>
        ///     Cached access to pipeline.fakeExtensions
        /// </summary>
        public IList<string> FakeExtensions
        {
            get
            {
                var temp = _fakeExtensions;
                if (temp != null) return temp;
                else
                    temp = new List<string>(c.get("pipeline.fakeExtensions", ".ashx")
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                for (var i = 0; i < temp.Count; i++)
                    if (!temp[i].StartsWith(".", StringComparison.OrdinalIgnoreCase))
                        temp[i] = "." + temp[i];
                _fakeExtensions = temp;
                return temp;
            }
        }

        /// <summary>
        ///     Removes the first fake extension detected at the end of 'path'
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string TrimFakeExtensions(string path)
        {
            foreach (var s in FakeExtensions)
                if (path.EndsWith(s, StringComparison.OrdinalIgnoreCase))
                {
                    path = path.Substring(0, path.Length - s.Length).TrimEnd('.');
                    break;
                }

            return path;
        }

        public string ModifiedPathKey => "resizer.newPath";

        public string StopRoutingKey => "resizer.stopRouting";

        /// <summary>
        ///     Returns the value of Context.Items["resizer.newPath"] if present. If not, returns FilePath + PathInfo.
        ///     Sets Context.Items["resizer.newPath"].
        ///     Only useful during the Pipeline.PostAuthorizeRequestStart event.
        /// </summary>
        public string PreRewritePath
        {
            get
            {
                if (HttpContext.Current == null) return null;
                return HttpContext.Current.Items[ModifiedPathKey] != null
                    ? HttpContext.Current.Items[ModifiedPathKey] as string
                    : HttpContext.Current.Request.FilePath + HttpContext.Current.Request.PathInfo;
            }
            set => HttpContext.Current.Items[ModifiedPathKey] = value;
        }

        public string ModifiedQueryStringKey => "resizer.modifiedQueryString";


        /// <summary>
        ///     Returns the modified query string. If never set, returns a copy of Request.QueryString.
        ///     Returns the same instance if called multiple times. Copy it if you want to make changes without causing issues.
        /// </summary>
        public NameValueCollection ModifiedQueryString
        {
            get
            {
                if (HttpContext.Current == null) return null;
                if (HttpContext.Current.Items[ModifiedQueryStringKey] == null)
                {
                    var qs = new NameValueCollection(HttpContext.Current.Request.QueryString);

                    //see if we have query string parameters that we want to have ignored, e.g. cachebusters
                    var dropQuerystringKeys = DropQuerystringKeys;
                    if (!string.IsNullOrEmpty(dropQuerystringKeys))
                        foreach (var dropQuerystringKey in dropQuerystringKeys.Split(','))
                            qs.Remove(dropQuerystringKey);

                    HttpContext.Current.Items[ModifiedQueryStringKey] = qs;
                }

                return (NameValueCollection)HttpContext.Current.Items[ModifiedQueryStringKey];
            }
            set => HttpContext.Current.Items[ModifiedQueryStringKey] = value;
        }


        public string SkipFileTypeCheckKey => "resizer.skipFileTypeCheck";

        /// <summary>
        ///     Get or sets whether the file extension check should be applied to the current request. Defaults to true.
        ///     If set to true, will only affect the current request, and will only cause the Resizer to evaluate the rewriting
        ///     rules on the request.
        ///     Processing may still not occur if no querystring values are specified. Add 'cache=always' to force caching to
        ///     occur.
        /// </summary>
        public bool SkipFileTypeCheck
        {
            get => HttpContext.Current != null && HttpContext.Current.Items[SkipFileTypeCheckKey] != null &&
                   (bool)HttpContext.Current.Items[SkipFileTypeCheckKey];
            set => HttpContext.Current.Items[SkipFileTypeCheckKey] = value;
        }


        public string ResponseArgsKey => "resizer.cacheArgs";

        /// <summary>
        ///     Returns true if the current request is being processed and/or cached by the pipeline.
        ///     Will return false until *after* the FileExists method is called on the VirtualPathProviders, which is after the
        ///     AuthorizeImage event fires.
        ///     This will return a usable value if used from VirtualFile.Open(), or if used inside the PreHandleImage event or
        ///     later.
        /// </summary>
        public bool IsHandlingRequest =>
            HttpContext.Current != null && HttpContext.Current.Items[ResponseArgsKey] != null;


        public VppUsageOption VppUsage => c.get<VppUsageOption>("pipeline.vppUsage", VppUsageOption.Fallback);

        /// <summary>
        ///     The maximum number of concurrent jobs that can be executing at one time.
        /// </summary>
        public int? MaxConcurrentJobs
        {
            get
            {
                if ("auto".Equals(c.get("pipeline.maxConcurrentJobs", ""), StringComparison.OrdinalIgnoreCase))
                    return Environment.ProcessorCount + 1;
                var count = c.get<int>("pipeline.maxConcurrentJobs", 0);
                return count < 1 ? (int?)null : count;
            }
        }


        /// <summary>
        ///     Returns an IVirtualFileAsync instance if the specified file can be provided by an async provider
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public async Task<IVirtualFileAsync> GetFileAsync(string virtualPath, NameValueCollection queryString)
        {
            IVirtualFileAsync f = null;
            foreach (var p in c.Plugins.GetAll<IVirtualImageProviderAsync>())
                if (await p.FileExistsAsync(virtualPath, queryString))
                {
                    f = await p.GetFileAsync(virtualPath, queryString);
                    break;
                }

            if (f == null) return null;
            try
            {
                //Now we have a reference to the real virtual file, let's see if it is source-cached.
                IVirtualFileAsync cached = null;
                foreach (var p in c.Plugins.GetAll<IVirtualFileCacheAsync>())
                {
                    cached = await p.GetFileIfCachedAsync(virtualPath, queryString, f);
                    if (cached != null) return cached;
                }
            }
            catch (FileNotFoundException)
            {
                //IVirtualFileCache instances will .Open() and read the original IVirtualFile instance. 
                return null;
            }
            catch (BlobMissingException)
            {
                return null;
            }

            return f;
        }


        /// <summary>
        ///     Returns either an IVirtualFile instance or a VirtualFile instance.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public IVirtualFile GetFile(string virtualPath, NameValueCollection queryString)
        {
            IVirtualFile f = null;
            foreach (var p in c.Plugins.VirtualProviderPlugins)
                if (p.FileExists(virtualPath, queryString))
                {
                    f = p.GetFile(virtualPath, queryString);
                    break;
                }

            if (f == null && HostingEnvironment.VirtualPathProvider != null &&
                HostingEnvironment.VirtualPathProvider.FileExists(virtualPath))
            {
                var vf = HostingEnvironment.VirtualPathProvider.GetFile(virtualPath);
                if (vf is IVirtualFile) f = (IVirtualFile)vf;
                else f = new VirtualFileWrapper(vf);
            }

            if (f == null) return null;

            //Now we have a reference to the real virtual file, let's see if it is source-cached.
            try
            {
                IVirtualFile cached = null;
                foreach (var p in c.Plugins.GetAll<IVirtualFileCache>())
                {
                    cached = p.GetFileIfCached(virtualPath, queryString, f);
                    if (cached != null) return cached;
                }
            }
            catch (FileNotFoundException)
            {
                //IVirtualFileCache instances will .Open() and read the original IVirtualFile instance.
                //We must abstract the differences in thrown exception as much as possible.  Start with FileNotFound & DirectoryNotFound
                return null;
            }
            catch (BlobMissingException)
            {
                return null;
            }

            catch (DirectoryNotFoundException)
            {
                return null;
            }

            return f;
        }

        /// <summary>
        ///     Returns true if (a) A registered IVirtualImageProvider says it exists, or (b) if the VirtualPathProvider chain says
        ///     it exists.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public bool FileExists(string virtualPath, NameValueCollection queryString)
        {
            foreach (var p in c.Plugins.VirtualProviderPlugins)
                if (p.FileExists(virtualPath, queryString))
                    return true;
            return HostingEnvironment.VirtualPathProvider != null
                ? HostingEnvironment.VirtualPathProvider.FileExists(virtualPath)
                : false;
        }

        public async Task<bool> FileExistsAsync(string virtualPath, NameValueCollection queryString)
        {
            foreach (var p in c.Plugins.GetAll<IVirtualImageProviderAsync>())
                if (await p.FileExistsAsync(virtualPath, queryString))
                    return true;
            return false;
        }


        public ImageBuilder GetImageBuilder()
        {
            return c.CurrentImageBuilder;
        }

        public ImageFileType GuessOutputFileTypeIfSupported(Instructions commands, string virtualPath)
        {
            foreach (var p in c.Plugins.GetAll<IPluginSupportsOutputFileTypes>())
            {
                var guess = p.GuessOutputFileTypeIfSupported(commands, virtualPath);
                if (guess != null) return guess;
            }

            return null;
        }

        /// <summary>
        ///     Fired once, on the first PostAuthorizeRequest event.
        /// </summary>
        public event RequestEventHandler OnFirstRequest;

        /// <summary>
        ///     Fires during the PostAuthorizeRequest phase, prior to any module-specific logic.
        ///     Executes for every request to the website. Use only as a last resort. Other events occur only for image requests,
        ///     and thus have lower overhead.
        /// </summary>
        public event RequestEventHandler PostAuthorizeRequestStart;

        /// <summary>
        ///     Fired during PostAuthorizeRequest, after ResizeExtension has been removed.
        ///     On fired on requests with extensions that match supported image types.
        ///     <para>
        ///         You can add additional supported image extensions by registering a plugin that implements IQuerystringPlugin,
        ///         or you can add an
        ///         extra extension in the URL and remove it here. Example: .psd.jpg
        ///     </para>
        /// </summary>
        public event UrlRewritingEventHandler Rewrite;

        /// <summary>
        ///     Fired during PostAuthorizeRequest, after Rewrite.
        ///     Any changes made here (which conflict) will be overwritten by the current querystring values. I.e, this is a good
        ///     place to specify default settings.
        ///     <para>Only fired on accepted image types. (see Rewrite)</para>
        /// </summary>
        public event UrlRewritingEventHandler RewriteDefaults;

        /// <summary>
        ///     Fired after all other rewrite events.
        ///     <para>Only fired on accepted image types. (see Rewrite)</para>
        /// </summary>
        public event UrlRewritingEventHandler PostRewrite;

        /// <summary>
        ///     Fired after all rewriting is finished.
        ///     e.AllowAccess defaults to the result of the UrlAuthorization module's verdict. It can be changed.
        ///     Set e.AllowAccess to true to cause and 403 Access Denied result.
        /// </summary>
        public event UrlAuthorizationEventHandler AuthorizeImage;


        /// <summary>
        ///     Fired when the specified image doesn't exist. Only called for images that would normally be processed.
        ///     May be called during PostAuthorizeRequest or later - End the request completely with a redirect if you want
        ///     alternate behavior.
        /// </summary>
        public event UrlEventHandler ImageMissing;

        /// <summary>
        ///     (Sync Intercept Module only) Fired immediately before the image request is sent off to the caching system for
        ///     processing.
        ///     Allows modification of response headers, caching arguments, and callbacks.
        /// </summary>
        public event PreHandleImageEventHandler PreHandleImage;

        /// <summary>
        ///     (ASYNC Intercept Module only) Fired immediately before the image request is sent off to the caching system for
        ///     processing.
        ///     Allows modification of response headers, caching arguments, and callbacks.
        /// </summary>
        public event PreHandleImageAsyncEventHandler PreHandleImageAsync;


        /// <summary>
        ///     An event that fires for most image requests, but does not guarantee an httpcontext.
        /// </summary>
        public event HeartbeatHandler Heartbeat;

        [CLSCompliant(false)] protected volatile bool firedFirstRequest = false;

        protected volatile bool firstRequestFinished = false;

        protected object firedFirstRequestSync = new object();

        public void FirePostAuthorizeRequest(IHttpModule sender, HttpContext httpContext)
        {
            //The one-time event
            if (!firstRequestFinished)
                lock (firedFirstRequestSync)
                {
                    if (!firedFirstRequest)
                    {
                        firedFirstRequest = true;
                        if (OnFirstRequest != null) OnFirstRequest(sender, httpContext);
                        firstRequestFinished = true;
                    }
                }

            //And the main event
            if (PostAuthorizeRequestStart != null) PostAuthorizeRequestStart(sender, httpContext);
        }


        public void FireRewritingEvents(IHttpModule sender, HttpContext context, IUrlEventArgs e)
        {
            //TODO: this approach is non-intuitive....

            //Fire first event (results will stay in e)
            if (Rewrite != null) Rewrite(sender, context, e);

            //Copy querystring for use in 'defaults' even
            var copy = new NameValueCollection(e
                .QueryString); //Copy so we can later overwrite q with the rewrite values.

            //Fire defaults event.
            if (RewriteDefaults != null) RewriteDefaults(sender, context, e);

            //Overwrite with querystring values again - this is what makes applyDefaults applyDefaults, vs. being applyOverrides.
            foreach (string k in copy)
                if (copy[k] != null) //Don't allow null values to override defaults. Empty values can, however.
                    e.QueryString[k] = copy[k];

            //Fire final event
            if (PostRewrite != null) PostRewrite(sender, context, e);
        }

        public void FireHeartbeat()
        {
            Heartbeat?.Invoke(this, c);
        }

        public void FireHeartbeatViaAspNetCache(int intervalSeconds)
        {
            HttpRuntime.Cache.Insert("FireHeartbeat_for_PipelineConfig_" + GetHashCode(), intervalSeconds, null,
                DateTime.Now.AddSeconds(intervalSeconds), Cache.NoSlidingExpiration,
                CacheItemPriority.NotRemovable, (k, v, r) =>
                {
                    var seconds = Convert.ToInt32(v);
                    if (seconds <= 0) return;
                    FireHeartbeat();
                    FireHeartbeatViaAspNetCache(seconds);
                });
        }


        public void FireAuthorizeImage(IHttpModule sender, HttpContext context, IUrlAuthorizationEventArgs e)
        {
            if (AuthorizeImage != null) AuthorizeImage(sender, context, e);
        }

        public void FirePreHandleImageAsync(IHttpModule sender, HttpContext context, IAsyncResponsePlan e)
        {
            Interlocked.Increment(ref processedCount);
            if (PreHandleImageAsync != null) PreHandleImageAsync(sender, context, e);
        }

        public void FireImageMissing(IHttpModule sender, HttpContext context, IUrlEventArgs e)
        {
            if (ImageMissing != null) ImageMissing(sender, context, e);
        }

        protected long processedCount = 0;

        /// <summary>
        ///     The number of images processed by this pipeline.
        /// </summary>
        public long ProcessedCount => processedCount;

        public void FirePreHandleImage(IHttpModule sender, HttpContext context, IResponseArgs e)
        {
            Interlocked.Increment(ref processedCount);
            if (PreHandleImage != null) PreHandleImage(sender, context, e);
        }


        public ResizeSettings Modify(ResizeSettings settings)
        {
            foreach (var m in c.Plugins.SettingsModifierPlugins)
                settings = m.Modify(settings);
            return settings;
        }


        /// <summary>
        ///     True once the InterceptModule or InterceptModuleAsync has been installed and is intercepting requests.
        /// </summary>
        public bool ModuleInstalled { get; set; }


        private bool? _usingAsyncMode;

        /// <summary>
        ///     True if the InterceptModuleAsync has been installed. Null if we can't find out.
        /// </summary>
        public bool? UsingAsyncMode
        {
            get
            {
                if (_usingAsyncMode == null && HttpContext.Current != null &&
                    HttpContext.Current.ApplicationInstance != null)
                {
                    var modules = HttpContext.Current.ApplicationInstance.Modules;
                    var list = new IHttpModule[modules.Count];
                    modules.CopyTo(list, 0);

                    _usingAsyncMode = list.Any((s) => s is AsyncInterceptModule);
                }

                return _usingAsyncMode;
            }
            set => _usingAsyncMode = value;
        }

        /// <summary>
        ///     Returns true if the AppDomain has Unrestricted code access security
        /// </summary>
        /// <returns></returns>
        public bool IsAppDomainUnrestricted()
        {
            var permissionSet = new PermissionSet(PermissionState.Unrestricted);
            permissionSet.AddPermission(new SecurityPermission(PermissionState.Unrestricted));
            return permissionSet.IsSubsetOf(AppDomain.CurrentDomain.PermissionSet);
        }

        private bool? _authorizeAllImages = null;

        /// <summary>
        ///     If true, AuthorizeImage will be called for all image requests, not just those with command directives.
        /// </summary>
        public bool AuthorizeAllImages
        {
            get
            {
                if (_authorizeAllImages == null) _authorizeAllImages = c.get("pipeline.authorizeAllImages", true);
                return _authorizeAllImages.Value;
            }
            set => _authorizeAllImages = value;
        }


        public IAsyncTyrantCache GetAsyncCacheFor(HttpContext context, IAsyncResponsePlan plan)
        {
            IAsyncTyrantCache defaultCache = null;
            //Grab the last cache that claims it can process the request.
            foreach (var cache in c.Plugins.GetAll<IAsyncTyrantCache>())
                if (cache.CanProcess(context, plan))
                    defaultCache = cache;

            return defaultCache;
        }

        private string _dropQuerystringKeys = null;

        /// <summary>
        ///     If specified, DropQuerystringKeys will cause certain query string parameters to be excluded from processing.
        /// </summary>
        public string DropQuerystringKeys
        {
            get
            {
                if (_dropQuerystringKeys == null) _dropQuerystringKeys = c.get("pipeline.dropQuerystringKeys", "");
                return _dropQuerystringKeys;
            }
            set => _dropQuerystringKeys = value;
        }

        public string ModifyRequestCacheKey(string currentKey, string virtualPath, NameValueCollection queryString)
        {
            foreach(var m in c.Plugins.ModifiesRequestCacheKeyPlugins)
                currentKey = m.ModifyRequestCacheKey(currentKey, virtualPath, queryString);

            return currentKey;
        }
    }
}