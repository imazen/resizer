/* Copyright (c) 2014 Imazen See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Configuration;
using ImageResizer.Util;
using System.IO;
using ImageResizer.Caching;
using ImageResizer.Configuration;
using System.Web.Hosting;
using ImageResizer.Configuration.Issues;
using ImageResizer.Configuration.Logging;
using ImageResizer.Plugins.Basic;
using System.Security.Permissions;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

namespace ImageResizer.Plugins.DiskCache
{
    /// <summary>
    /// Indicates a problem with disk caching. Causes include a missing (or too small) ImageDiskCacheDir setting, and severe I/O locking preventing 
    /// the cache dir from being cleaned at all.
    /// </summary>
    public class DiskCacheException : Exception
    {
        public DiskCacheException(string message):base(message){}
        public DiskCacheException(string message, Exception innerException) : base(message, innerException) { }
    }



    /// <summary>
    /// Provides methods for creating, maintaining, and securing the disk cache. 
    /// </summary>
    public class DiskCache: IAsyncTyrantCache, ICache, IPlugin, IIssueProvider, ILoggerProvider
    {
        private int subfolders = 8192;
        /// <summary>
        /// Controls how many subfolders to use for disk caching. Rounded to the next power of to. (1->2, 3->4, 5->8, 9->16, 17->32, 33->64, 65->128,129->256,etc.)
        /// NTFS does not handle more than 8,000 files per folder well. Larger folders also make cleanup more resource-intensive.
        /// Defaults to 8192, which combined with the default setting of 400 images per folder, allows for scalability to ~1.5 million actively used image versions. 
        /// For example, given a desired cache size of 100,000 items, this should be set to 256.
        /// </summary>
        public int Subfolders {
            get { return subfolders; }
            set { BeforeSettingChanged(); subfolders = value; }
        }

        private bool enabled = true;
        /// <summary>
        /// Allows disk caching to be disabled for debuginng purposes. Defaults to true.
        /// </summary>
        public bool Enabled {
            get { return enabled; }
            set { BeforeSettingChanged(); enabled = value; }
        }


        private bool autoClean = false;
        /// <summary>
        /// If true, items from the cache folder will be automatically 'garbage collected' if the cache size limits are exceeded.
        /// Defaults to false.
        /// </summary>
        public bool AutoClean {
            get { return autoClean; }
            set { BeforeSettingChanged();  autoClean = value; }
        }
        private CleanupStrategy cleanupStrategy = new CleanupStrategy();
        /// <summary>
        /// Only relevant when AutoClean=true. Settings about how background cache cleanup are performed.
        /// It is best not to modify these settings. There are very complicated and non-obvious factors involved in their choice.
        /// </summary>
        public CleanupStrategy CleanupStrategy {
            get { return cleanupStrategy; }
            set { BeforeSettingChanged(); cleanupStrategy = value; }
        }

        /// <summary>
        /// Sets the timeout time to 15 seconds as default.
        /// </summary>
        protected int cacheAccessTimeout = 15000;
        /// <summary>
        /// How many milliseconds to wait for a cached item to be available. Values below 0 are set to 0. Defaults to 15 seconds.
        /// Actual time spent waiting may be 2 or 3x this value, if multiple layers of synchronization require a wait.
        /// </summary>
        public int CacheAccessTimeout {
            get { return cacheAccessTimeout; }
            set { BeforeSettingChanged(); cacheAccessTimeout = Math.Max(value,0); }
        }


        private bool _asyncWrites = false;
        /// <summary>
        /// If true, writes to the disk cache will be performed outside the request thread, allowing responses to return to the client quicker. 
        /// </summary>
        public bool AsyncWrites {
            get { return _asyncWrites; }
            set { BeforeSettingChanged(); _asyncWrites = value; }
        }


        private int _asyncBufferSize = 1024 * 1024 * 10;
        /// <summary>
        /// If more than this amount of memory (in bytes) is currently allocated by queued writes, the request will be processed synchronously instead of asynchronously.
        /// </summary>
        public int AsyncBufferSize {
            get { return _asyncBufferSize; }
            set { BeforeSettingChanged(); _asyncBufferSize = value; }
        }


        protected string virtualDir =  HostingEnvironment.ApplicationVirtualPath.TrimEnd('/') + "/imagecache";
        /// <summary>
        /// Sets the location of the cache directory. 
        /// Can be a virtual path (like /App/imagecache) or an application-relative path (like ~/imagecache, the default).
        /// Relative paths are assummed to be relative to the application root.
        /// All values are converted to virtual path format upon assignment (/App/imagecache)
        /// Will throw an InvalidOperationException if changed after the plugin is installed.
        /// </summary>
        public string VirtualCacheDir { 
            get { 
                return virtualDir; 
            }
            set {
                BeforeSettingChanged();
                //Default to application-relative path if no leading slash is present. 
                //Resolve the tilde if present.
                virtualDir =  string.IsNullOrEmpty(value) ? null : PathUtils.ResolveAppRelativeAssumeAppRelative(value);
            }
        }
        /// <summary>
        /// Returns the physical path of the cache directory specified in VirtualCacheDir.
        /// </summary>
        public string PhysicalCacheDir {
            get {
                if (!string.IsNullOrEmpty(VirtualCacheDir)) return HostingEnvironment.MapPath(VirtualCacheDir);
                return null;
            }
        }

        /// <summary>
        /// Throws an exception if the class is already modified
        /// </summary>
        protected void BeforeSettingChanged() {
            if (_started) throw new InvalidOperationException("DiskCache settings may not be adjusted after it is started.");
        }

        /// <summary>
        /// Creates a disk cache in the /imagecache folder
        /// </summary>
        public DiskCache(){}
        
        /// <summary>
        /// Creates a DiskCache instance at the specified location. Must be installed as a plugin to be operational.
        /// </summary>
        /// <param name="virtualDir"></param>
        public DiskCache(string virtualDir){
            VirtualCacheDir = virtualDir;
        }
        /// <summary>
        /// Uses the defaults from the resizing.diskcache section in the specified configuration.
        /// Throws an invalid operation exception if the DiskCache is already started.
        /// </summary>
        public void LoadSettings(Config c){
            Subfolders = c.get("diskcache.subfolders", Subfolders);
            Enabled = c.get("diskcache.enabled", Enabled);
            AutoClean = c.get("diskcache.autoClean", AutoClean);
            VirtualCacheDir = c.get("diskcache.dir", VirtualCacheDir);
            CacheAccessTimeout = c.get("diskcache.cacheAccessTimeout", CacheAccessTimeout);
            AsyncBufferSize = c.get("diskcache.asyncBufferSize", AsyncBufferSize);
            AsyncWrites = c.get("diskcache.asyncWrites", AsyncWrites);
            CleanupStrategy.LoadFrom(c.getNode("cleanupStrategy"));
            AsyncModuleMode = c.get("diskcache.asyncModuleMode", AsyncModuleMode);
        }
       
        public bool AsyncModuleMode {get; private set; }

        protected ILogger log = null;
        public ILogger Logger { get { return log; } }

        private Config c;
        /// <summary>
        /// Loads the settings from 'c', starts the cache, and registers the plugin.
        /// Will throw an invalidoperationexception if already started.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public IPlugin Install(Config c) {
            this.c = c;
            if (c.get("diskcache.logging", false)) {
                if (c.Plugins.LogManager != null) 
                    log = c.Plugins.LogManager.GetLogger("ImageResizer.Plugins.DiskCache");
                else 
                    c.Plugins.LoggingAvailable += delegate(ILogManager mgr) {
                        if (log != null) log = c.Plugins.LogManager.GetLogger("ImageResizer.Plugins.DiskCache");
                    };
            }

            bool? inAsyncMode = c.Pipeline.UsingAsyncMode;
            if (inAsyncMode == null) throw new InvalidOperationException("You must set Config.Current.Pipeline.UsingAsyncMode before installing DiskCache");
            this.AsyncModuleMode = inAsyncMode.Value;

            LoadSettings(c);
            Start();
            c.Pipeline.AuthorizeImage += Pipeline_AuthorizeImage;
            c.Plugins.add_plugin(this);
            return this;
        }

        void Pipeline_AuthorizeImage(IHttpModule sender, HttpContext context, IUrlAuthorizationEventArgs e) {
            //Don't allow direct access to the cache.
            if (e.VirtualPath.StartsWith(this.VirtualCacheDir, StringComparison.OrdinalIgnoreCase)) e.AllowAccess = false;
        }

        /// <summary>
        /// Removes the plugin from the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Uninstall(Config c) {
            c.Plugins.remove_plugin(this);
            c.Pipeline.AuthorizeImage -= Pipeline_AuthorizeImage;
            return this.Stop();
        }


        /// <summary>
        /// Returns true if the configured settings are valid and .NET (not NTFS) permissions will work.
        /// </summary>
        /// <returns></returns>
        public bool IsConfigurationValid(){
            return !string.IsNullOrEmpty(VirtualCacheDir) && this.Enabled && HasFileIOPermission();
        }
        /// <summary>
        /// Returns true if .NET permissions allow writing to the cache directory. Does not check NTFS permissions. 
        /// </summary>
        /// <returns></returns>
        protected bool HasFileIOPermission() {
            return PathUtils.HasIOPermission(new string[] { PhysicalCacheDir, Path.Combine(PhysicalCacheDir, "web.config") });
        }
        
        protected CustomDiskCache cache = null;
        protected AsyncCustomDiskCache asyncCache = null;
        protected CleanupManager cleaner = null;
        protected WebConfigWriter writer = null;

        protected readonly object _startSync = new object();
        protected volatile bool _started = false;
        /// <summary>
        /// Returns true if the DiskCache instance is operational.
        /// </summary>
        public bool Started { get { return _started; } }
        /// <summary>
        /// Attempts to start the DiskCache using the current settings. Returns true if succesful or if already started. Returns false on a configuration error.
        /// Called by Install()
        /// </summary>
        public bool Start() {
            if (!IsConfigurationValid()) return false;
            lock (_startSync) {
                if (_started) return true;
                if (!IsConfigurationValid()) return false;

                //Init the writer.
                writer = new WebConfigWriter(this,PhysicalCacheDir);
                //Init the inner cache
                if (!AsyncModuleMode) cache = new CustomDiskCache(this, PhysicalCacheDir, Subfolders,AsyncBufferSize);
                if (AsyncModuleMode) asyncCache = new AsyncCustomDiskCache(this, PhysicalCacheDir, Subfolders, AsyncBufferSize);

                //Init the cleanup strategy
                if (AutoClean && cleanupStrategy == null) cleanupStrategy = new CleanupStrategy(); //Default settings if null
                //Init the cleanup worker
                if (AutoClean) cleaner = new CleanupManager(this, AsyncModuleMode ? (ICleanableCache)asyncCache : (ICleanableCache)cache, cleanupStrategy);
                //If we're running with subfolders, enqueue the cache root for cleanup (after the 5 minute delay)
                //so we don't eternally 'skip' files in the root or in other unused subfolders (since only 'accessed' subfolders are ever cleaned ). 
                if (cleaner != null) cleaner.CleanAll();

                if (log != null) log.Info("DiskCache started successfully.");
                //Started successfully
                _started = true;
                return true;
            }
        }
        /// <summary>
        /// Returns true if stopped succesfully. Cannot be restarted
        /// </summary>
        /// <returns></returns>
        public bool Stop() {
            if (cleaner != null) cleaner.Dispose();
            cleaner = null;
            return true;
        }

        public bool CanProcess(HttpContext current, IResponseArgs e) {
            //Disk caching will 'pass on' caching requests if 'cache=no'.
            if (((ResizeSettings)e.RewrittenQuerystring).Cache == ServerCacheMode.No) return false;
            return Started;//Add support for nocache
        }
        public bool CanProcess(HttpContext current, IAsyncResponsePlan e)
        {
            //Disk caching will 'pass on' caching requests if 'cache=no'.
            if (new Instructions(e.RewrittenQuerystring).Cache == ServerCacheMode.No) return false;
            return Started;//Add support for nocache
        }


        public void Process(HttpContext context, IResponseArgs e) {
            if (this.AsyncModuleMode) throw new InvalidOperationException("DiskCache cannot be used in synchronous mode if AsyncModuleMode=true");
            CacheResult r = Process(e);
            context.Items["FinalCachedFile"] = r.PhysicalPath;

            if (r.Data == null) {

                //Calculate the virtual path
                string virtualPath = VirtualCacheDir.TrimEnd('/') + '/' + r.RelativePath.Replace('\\', '/').TrimStart('/');

                //Rewrite to cached, resized image.
                context.RewritePath(virtualPath, false);
            } else {
                //Remap the response args writer to use the existing stream.
                ((ResponseArgs)e).ResizeImageToStream = delegate(Stream s) {
                    ((MemoryStream)r.Data).WriteTo(s);
                };
                context.RemapHandler(new NoCacheHandler(e));
            }
        }
        public async Task ProcessAsync(HttpContext context, IAsyncResponsePlan e)
        {
            if (!this.AsyncModuleMode) throw new InvalidOperationException("DiskCache cannot be used in asynchronous mode if AsyncModuleMode=false");
            CacheResult r = await ProcessAsync(e);
            context.Items["FinalCachedFile"] = r.PhysicalPath;

            if (r.Data == null)
            {

                //Calculate the virtual path
                string virtualPath = VirtualCacheDir.TrimEnd('/') + '/' + r.RelativePath.Replace('\\', '/').TrimStart('/');

                //Rewrite to cached, resized image.
                context.RewritePath(virtualPath, false);
            }
            else
            {
                //Remap the response args writer to use the existing stream.
                e.CreateAndWriteResultAsync = delegate(Stream s, IAsyncResponsePlan plan)
                {
                    return ((MemoryStream)r.Data).CopyToAsync(s);
                };
                context.RemapHandler(new NoCacheAsyncHandler(e));
            }
        }


        private async Task<CacheResult> ProcessAsync(IAsyncResponsePlan e)
        {

            //Verify web.config exists in the cache folder.
            writer.CheckWebConfigEvery5();

            //Cache the data to disk and return a path.
            CacheResult r = await asyncCache.GetCachedFile(e.RequestCachingKey, e.EstimatedFileExtension, async delegate(Stream outStream){
                await e.CreateAndWriteResultAsync(outStream, e);
            }, CacheAccessTimeout, AsyncWrites);

            //Fail
            if (r.Result == CacheQueryResult.Failed)
                throw new ImageResizer.ImageProcessingException("Failed to acquire a lock on file \"" + r.PhysicalPath + "\" within " + CacheAccessTimeout + "ms. Caching failed.");

            if (r.Result == CacheQueryResult.Hit && cleaner != null)
                cleaner.UsedFile(r.RelativePath, r.PhysicalPath);

            return r;
        }

        public CacheResult Process(IResponseArgs e) {

            //Verify web.config exists in the cache folder.
            writer.CheckWebConfigEvery5();

            //Cache the data to disk and return a path.
            CacheResult r = cache.GetCachedFile(e.RequestKey, e.SuggestedExtension, e.ResizeImageToStream, CacheAccessTimeout,AsyncWrites);

            //Fail
            if (r.Result == CacheQueryResult.Failed)
                throw new ImageResizer.ImageProcessingException("Failed to acquire a lock on file \"" + r.PhysicalPath + "\" within " + CacheAccessTimeout + "ms. Caching failed.");

            if (r.Result == CacheQueryResult.Hit && cleaner != null)
                cleaner.UsedFile(r.RelativePath, r.PhysicalPath);

            return r;
        }

        protected bool HasNTFSPermission(){
            try {
                if (!Directory.Exists(PhysicalCacheDir)) Directory.CreateDirectory(PhysicalCacheDir);
                string testFile = Path.Combine(this.PhysicalCacheDir, "TestFile.txt");
                File.WriteAllText(testFile, "You may delete this file - it is written and deleted just to verify permissions are configured correctly");
                File.Delete(testFile);
                return true;
            } catch (Exception){
                return false;
            }
        }

        protected string GetExecutingUser() {
            try {
                return Thread.CurrentPrincipal.Identity.Name;
            } catch {
                return "[Unknown - please check App Pool configuration]";
            }
        }

        public IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>();
            if (cleaner != null) issues.AddRange(cleaner.GetIssues());

            if (!c.get("diskcache.hashModifiedDate", true)) issues.Add(new Issue("DiskCache", "V4.0 no longer supports hashModifiedDate=false. Please remove this attribute.", IssueSeverity.ConfigurationError));

            if (!HasFileIOPermission()) 
                issues.Add(new Issue("DiskCache", "Failed to start: Write access to the cache directory is prohibited by your .NET trust level configuration.", 
                "Please configure your .NET trust level to permit writing to the cache directory. Most medium trust configurations allow this, but yours does not.", IssueSeverity.ConfigurationError));
            
            if (!HasNTFSPermission()) 
                issues.Add(new Issue("DiskCache", "Not working: Your NTFS Security permissions are preventing the application from writing to the disk cache",
    "Please give user " + GetExecutingUser() + " read and write access to directory \"" + PhysicalCacheDir + "\" to correct the problem. You can access NTFS security settings by right-clicking the aformentioned folder and choosing Properties, then Security.", IssueSeverity.ConfigurationError));

            if (!Started && !Enabled) issues.Add(new Issue("DiskCache", "DiskCache is disabled in Web.config. Set enabled=true on the <diskcache /> element to fix.", null, IssueSeverity.ConfigurationError));

            //Warn user about setting hashModifiedDate=false in a web garden.
            if (this.AsyncBufferSize < 1024 * 1024 * 2)
                issues.Add(new Issue("DiskCache", "The asyncBufferSize should not be set below 2 megabytes (2097152). Found in the <diskcache /> element in Web.config.",
                    "A buffer that is too small will cause requests to be processed synchronously. Remember to set the value to at least 4x the maximum size of an output image.", IssueSeverity.ConfigurationError));

            string physicalCache = PhysicalCacheDir;
            if (!string.IsNullOrEmpty(physicalCache)) {
                bool isNetwork = false;
                if (physicalCache.StartsWith("\\\\")) 
                    isNetwork = true;
                else{
                    try {
                        DriveInfo dri = new DriveInfo(Path.GetPathRoot(physicalCache));
                        if (dri.DriveType == DriveType.Network) isNetwork = true;
                    } catch { }
                }
                if (isNetwork)
                    issues.Add(new Issue("DiskCache", "It appears that the cache directory is located on a network drive.",
                        "Both IIS and ASP.NET have trouble hosting websites with large numbers of folders over a network drive, such as a SAN. The cache will create " +
                        Subfolders.ToString() + " subfolders. If the total number of network-hosted folders exceeds 100, you should contact support@imageresizing.net and consult the documentation for details on configuring IIS and ASP.NET for this situation.", IssueSeverity.Warning));
                    
            }

            return issues;
        }
    

    }
}
