/* Copyright (c) 2011 Nathanael Jones. See license.txt for your rights. */
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
    public class DiskCache: ICache, IPlugin, IIssueProvider, ILoggerProvider
    {
        private int subfolders = 32;
        /// <summary>
        /// Controls how many subfolders to use for disk caching. Rounded to the next power of to. (1->2, 3->4, 5->8, 9->16, 17->32, 33->64, 65->128,129->256,etc.)
        /// NTFS does not handle more than 8,000 files per folder well. Larger folders also make cleanup more resource-intensive.
        /// Defaults to 32, which combined with the default setting of 400 images per folder, allows for scalability to 12,800 actively used image versions. 
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


        protected int cacheAccessTimeout = 15000;
        /// <summary>
        /// How many milliseconds to wait for a cached item to be available. Values below 0 are set to 0. Defaults to 15 seconds.
        /// Actual time spent waiting may be 2 or 3x this value, if multiple layers of synchronization require a wait.
        /// </summary>
        public int CacheAccessTimeout {
            get { return cacheAccessTimeout; }
            set { BeforeSettingChanged(); cacheAccessTimeout = Math.Max(value,0); }
        }

        private bool hashModifiedDate = true;
        /// <summary>
        /// If true, when a source file is changed, a new file will be created instead of overwriting the old cached file.
        /// This helps prevent file lock contention on high-traffic servers. Defaults to true.  
        /// Do NOT set this to false in a Web Garden or if you have overlapped recycle enabled, as you may risk having occasional failed requests due
        /// to write contention by separate proccesses.
        /// Changes the hash function, so you should delete the cache folder whenever this setting is modified.
        /// </summary>
        public bool HashModifiedDate {
            get { return hashModifiedDate; }
            set { BeforeSettingChanged(); hashModifiedDate = value; }
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
                virtualDir =  string.IsNullOrEmpty(value) ? null : value;
                if (virtualDir != null){
                    //Default to application-relative path if no leading slash is present. 
                    //Resolve the tilde if present.
                    if (virtualDir.StartsWith("~")) virtualDir = HostingEnvironment.ApplicationVirtualPath.TrimEnd('/') + virtualDir.Substring(1);
                    else if (!virtualDir.StartsWith("/")) virtualDir =  HostingEnvironment.ApplicationVirtualPath.TrimEnd('/')  + "/" + virtualDir;
                }
                
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
            HashModifiedDate = c.get("diskcache.hashModifiedDate", HashModifiedDate);
            CacheAccessTimeout = c.get("diskcache.cacheAccessTimeout", CacheAccessTimeout);
            CleanupStrategy.LoadFrom(c.getNode("cleanupStrategy"));
        }

        protected ILogger log = null;
        public ILogger Logger { get { return log; } }
        /// <summary>
        /// Loads the settings from 'c', starts the cache, and registers the plugin.
        /// Will throw an invalidoperationexception if already started.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public IPlugin Install(Config c) {
            if (c.get("diskcache.logging", false)) {
                if (c.Plugins.LogManager != null) 
                    log = c.Plugins.LogManager.GetLogger("ImageResizer.Plugins.DiskCache");
                else 
                    c.Plugins.LoggingAvailable += delegate(ILogManager mgr) {
                        if (log != null) log = c.Plugins.LogManager.GetLogger("ImageResizer.Plugins.DiskCache");
                    };
            }
            LoadSettings(c);
            Start();
            c.Pipeline.AuthorizeImage += Pipeline_AuthorizeImage;
            c.Plugins.add_plugin(this);
            return this;
        }

        void Pipeline_AuthorizeImage(IHttpModule sender, HttpContext context, IUrlAuthorizationEventArgs e) {
            //Don't allow direct access to the cache.
            if (e.VirtualPath.IndexOf(this.VirtualCacheDir, StringComparison.OrdinalIgnoreCase) >= 0) e.AllowAccess = false;
        }

        public bool Uninstall(Config c) {
            c.Plugins.remove_plugin(this);
            c.Pipeline.AuthorizeImage -= Pipeline_AuthorizeImage;
            return this.Stop();
        }


        /// <summary>
        /// Returns true if the configured settings are valid.
        /// </summary>
        /// <returns></returns>
        public bool IsConfigurationValid(){
            return !string.IsNullOrEmpty(VirtualCacheDir) && this.Enabled;
        }



        
        protected CustomDiskCache cache = null;
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
                cache = new CustomDiskCache(this, PhysicalCacheDir, Subfolders, HashModifiedDate);
                //Init the cleanup strategy
                if (AutoClean && cleanupStrategy == null) cleanupStrategy = new CleanupStrategy(); //Default settings if null
                //Init the cleanup worker
                if (AutoClean) cleaner = new CleanupManager(this, cache, cleanupStrategy);
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


        public void Process(HttpContext context, IResponseArgs e) {

            //Query for the modified date of the source file. If the source file changes on us during the write,
            //we (may) end up saving the newer version in the cache properly, but with an older modified date.
            //This will not cause any actual problems from a behavioral standpoint - the next request for the 
            //image will cause the file to be overwritten and the modified date updated.
            DateTime modDate = e.HasModifiedDate ? e.GetModifiedDateUTC() : DateTime.MinValue;

            //Verify web.config exists in the cache folder.
            writer.CheckWebConfigEvery5();

            //Cache the data to disk and return a path.
            CacheResult r = cache.GetCachedFile(e.RequestKey, e.SuggestedExtension, e.ResizeImageToStream, modDate, CacheAccessTimeout);

            //Fail
            if (r.Result == CacheQueryResult.Failed) 
                throw new ImageResizer.ImageProcessingException("Failed to acquire a lock on file \"" + r.PhysicalPath + "\" within " + CacheAccessTimeout + "ms. Caching failed.");

            if (r.Result == CacheQueryResult.Hit && cleaner != null)
                cleaner.UsedFile(r.RelativePath, r.PhysicalPath);

            context.Items["FinalCachedFile"] = r.PhysicalPath;


            //Calculate the virtual path
            string virtualPath = VirtualCacheDir.TrimEnd('/') + '/' + r.RelativePath.Replace('\\', '/').TrimStart('/');

            //Rewrite to cached, resized image.
            context.RewritePath(virtualPath, false);
        }




        public IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>();
            if (cleaner != null) issues.AddRange(cleaner.GetIssues());
            if (string.IsNullOrEmpty(VirtualCacheDir)) issues.Add(new Issue("DiskCache", "cacheDir is empty. Cannot operate", null, IssueSeverity.ConfigurationError));
            if (!Started) issues.Add(new Issue("DiskCache", "DiskCache is not running. Verify cacheDir is a valid path and enabled=true.", null, IssueSeverity.ConfigurationError));

            //Warn user about setting hashModifiedDate=false in a web garden.
            if (this.cleaner != null && cleaner.ExteralProcessCleaning && !this.HashModifiedDate)
                issues.Add(new Issue("DiskCache", "You should set hashModifiedDate=\"true\" on the <diskcache /> element in Web.config.",
                    "Setting false for this value in a Web Garden scenario can cause failed requests. (DiskCache detects one or more other process on this machine working on the same cache directory).", IssueSeverity.Critical));


            return issues;
        }
    }
}
