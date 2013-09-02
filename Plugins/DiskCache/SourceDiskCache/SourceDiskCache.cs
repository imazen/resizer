using ImageResizer.Configuration;
using ImageResizer.Configuration.Logging;
using ImageResizer.ExtensionMethods;
using ImageResizer.Plugins.DiskCache;
using ImageResizer.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;
using System.Security.Permissions;
using System.Text;
using System.Web;
using System.Web.Hosting;

namespace ImageResizer.Plugins.SourceDiskCache
{
    public class SourceDiskCachePlugin : IVirtualFileCache, IPlugin, ILoggerProvider
    {
        public SourceDiskCachePlugin() { }

        protected string virtualDir = HostingEnvironment.ApplicationVirtualPath.TrimEnd('/') + "/cache/sourceimages";
        /// <summary>
        /// Sets the location of the cache directory. 
        /// Can be a virtual path (like /App/imagecache) or an application-relative path (like ~/imagecache, the default).
        /// Relative paths are assummed to be relative to the application root.
        /// All values are converted to virtual path format upon assignment (/App/imagecache)
        /// Will throw an InvalidOperationException if changed after the plugin is installed.
        /// </summary>
        public string VirtualCacheDir
        {
            get
            {
                return virtualDir;
            }
            set
            {
                BeforeSettingChanged();
                //Default to application-relative path if no leading slash is present. 
                //Resolve the tilde if present.
                virtualDir = string.IsNullOrEmpty(value) ? null : PathUtils.ResolveAppRelativeAssumeAppRelative(value);
            }
        }
        /// <summary>
        /// Returns the physical path of the cache directory specified in VirtualCacheDir.
        /// </summary>
        public string PhysicalCacheDir
        {
            get
            {
                if (!string.IsNullOrEmpty(VirtualCacheDir)) return HostingEnvironment.MapPath(VirtualCacheDir);
                return null;
            }
        }
        /// <summary>
        /// Throws an exception if the class is already modified
        /// </summary>
        protected void BeforeSettingChanged()
        {
            if (_started) throw new InvalidOperationException("SourceDiskCache settings may not be adjusted after it is started.");
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

        public IVirtualFile GetFileIfCached(string virtualPath, System.Collections.Specialized.NameValueCollection queryString, IVirtualFile original)
        {
            if (!"disk".Equals(queryString["scache"], StringComparison.OrdinalIgnoreCase)) return null;


            //Verify web.config exists in the cache folder.
            writer.CheckWebConfigEvery5();

            //Use alternate cache key if provided
            string key = original is IVirtualFileSourceCacheKey ? ((IVirtualFileSourceCacheKey)original).GetCacheKey(false) : original.VirtualPath;
            //If cached, serve it. 
            var r = cache.GetCachedFile(key, ".cache", delegate(Stream target)
            {
                using (Stream data = original.Open())
                {//Very long-running call
                    StreamExtensions.CopyToStream(data, target);
                }
            }, DateTime.MinValue, 15 * 1000,true);

            if (r.Result == CacheQueryResult.Failed)
                throw new ImageResizer.ImageProcessingException("Failed to acquire a lock on file \"" + r.PhysicalPath + "\" within 15 seconds. Source caching failed.");

            if (r.Result == CacheQueryResult.Hit && cleaner != null)
                cleaner.UsedFile(r.RelativePath, r.PhysicalPath);


            return new SourceVirtualFile(original.VirtualPath,delegate(){
                return r.Data ?? File.Open(r.PhysicalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            });
        }

        public class SourceVirtualFile : IVirtualFile
        {
            public delegate Stream OpenDelegate();
            public SourceVirtualFile(string virtualPath, OpenDelegate streamDelegate){
                this.path = virtualPath;
                this.stream = streamDelegate;
            }
            private string path;
            public string VirtualPath
            {
                get { return path; }
            }

            private OpenDelegate stream;
            public Stream Open()
            {
                return stream();
            }
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

            Start();
            c.Pipeline.AuthorizeImage += Pipeline_AuthorizeImage;
            c.Plugins.add_plugin(this);
            return this;
        }

        void Pipeline_AuthorizeImage(IHttpModule sender, HttpContext context, IUrlAuthorizationEventArgs e)
        {
            //Don't allow direct access to the cache.
            if (e.VirtualPath.StartsWith(this.VirtualCacheDir, StringComparison.OrdinalIgnoreCase)) e.AllowAccess = false;
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            c.Pipeline.AuthorizeImage -= Pipeline_AuthorizeImage;
            return this.Stop();
        }

        /// <summary>
        /// Returns true if the configured settings are valid and .NET (not NTFS) permissions will work.
        /// </summary>
        /// <returns></returns>
        public bool IsConfigurationValid()
        {
            return !string.IsNullOrEmpty(VirtualCacheDir) && HasFileIOPermission();
        }

        /// <summary>
        /// Returns true if .NET permissions allow writing to the cache directory. Does not check NTFS permissions. 
        /// </summary>
        /// <returns></returns>
        protected bool HasFileIOPermission() {
            FileIOPermission writePermission = new FileIOPermission(FileIOPermissionAccess.AllAccess,new string[]{PhysicalCacheDir, Path.Combine(PhysicalCacheDir,"web.config")});
            return SecurityManager.IsGranted(writePermission);
        }


        /// <summary>
        /// Attempts to start the DiskCache using the current settings. Returns true if succesful or if already started. Returns false on a configuration error.
        /// Called by Install()
        /// </summary>
        public bool Start()
        {
            if (!IsConfigurationValid()) return false;
            lock (_startSync)
            {
                if (_started) return true;
                if (!IsConfigurationValid()) return false;

                //Init the writer.
                writer = new WebConfigWriter(this, PhysicalCacheDir);
                //Init the inner cache
                cache = new CustomDiskCache(this, PhysicalCacheDir, 512, true, 1024 * 1024 * 30); //30MB
                //Init the cleanup strategy
                var cleanupStrategy = new CleanupStrategy(); //Default settings if null
                cleanupStrategy.TargetItemsPerFolder = 50;
                //Init the cleanup worker
                cleaner = new CleanupManager(this, cache, cleanupStrategy);
                //If we're running with subfolders, enqueue the cache root for cleanup (after the 5 minute delay)
                //so we don't eternally 'skip' files in the root or in other unused subfolders (since only 'accessed' subfolders are ever cleaned ). 
                if (cleaner != null) cleaner.CleanAll();

                //Started successfully
                _started = true;
                return true;
            }
        }
        /// <summary>
        /// Returns true if stopped succesfully. Cannot be restarted
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            if (cleaner != null) cleaner.Dispose();
            cleaner = null;
            return true;
        }



    }
}
