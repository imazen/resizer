/**
 * Written by Nathanael Jones 
 * http://nathanaeljones.com
 * nathanael.jones@gmail.com
 * 
 * Although I typically release my components for free, I decided to charge a 
 * 'download fee' for this one to help support my other open-source projects. 
 * Don't worry, this component is still open-source, and the license permits 
 * source redistribution as part of a larger system. However, I'm asking that 
 * people who want to integrate this component purchase the download instead 
 * of ripping it out of another open-source project. My free to non-free LOC 
 * (lines of code) ratio is still over 40 to 1, and I plan on keeping it that 
 * way. I trust this will keep everybody happy.
 * 
 * By purchasing the download, you are permitted to 
 * 
 * 1) Modify and use the component in all of your projects. 
 * 
 * 2) Redistribute the source code as part of another project, provided 
 * the component is less than 5% of the project (in lines of code), 
 * and you keep this information attached.
 * 
 * 3) If you received the source code as part of another open source project, 
 * you cannot extract it (by itself) for use in another project without purchasing a download 
 * from http://nathanaeljones.com/. If nathanaeljones.com is no longer running, and a download
 * cannot be purchased, then you may extract the code.
 * 
 * Disclaimer of warranty and limitation of liability continued at http://nathanaeljones.com/11151_Image_Resizer_License
 * 
 **/


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
    public class DiskCache: ICache, IPlugin
    {
        protected string dir;
        protected int subfolders;
        bool autoClean;
        int maxImagesPerFolder;
        bool disabled;
        int fileLockTimeout;

        public DiskCache(string virtualDir, int subfolders, bool autoClean, int maxImagesPerFolder, bool debugMode, int fileLockTimeout){
            this.dir = dir;
            this.subfolders = subfolders;
            this.autoClean = autoClean;
            this.maxImagesPerFolder = maxImagesPerFolder;
            this.debugMode = debugMode;
            this.fileLockTimeout = fileLockTimeout;
        }
        /// <summary>
        /// Uses the defaults from the resizing.diskcache configuration section
        /// </summary>
        public DiskCache(Config c){
            disabled = c.get("diskcache.disable",false);
            autoClean = c.get("diskcache.autoClean",false);
            dir = c.get("diskcache.dir","~/imagecache");
            maxImagesPerFolder =  c.get("diskcache.maxImagesPerFolder", 8000);
            subfolders = c.get("diskcache.subfolders",0);
            fileLockTimeout = c.get("diskcache.fileLockTimeout", 10000); //10s
        }



        public IPlugin Install(Config c) {
            throw new NotImplementedException();
        }

        public bool Uninstall(Config c) {
            throw new NotImplementedException();
        }

        public string CacheDir{
            get{
                return dir;
            }
            set{
                dir = value;
                virtualDir = string.IsNullOrEmpty(dir) ? null : dir.Replace("~",HostingEnvironment.ApplicationVirtualPath);
            }
        }
        protected string virtualDir  = null;
        public string VirtualCacheDir{get{return virtualDir;}}

        public bool IsConfigurationValid(){
            return !string.IsNullOrEmpty(CacheDir);
        }

  

        private long filesUpdatedSinceCleanup = 0;
        private bool hasCleanedUp = false;

        public bool CanProcess(HttpContext current, IResponseArgs e) {
            return IsConfigurationValid();//Add support for nocache
        }


        public void Process(HttpContext context, IResponseArgs e) {

            //Query for the modified date of the source file. If the source file changes on us, we (may) end up saving the newer version in the cache properly, but with an older modified date.
            //This will not cause any problems from a behavioral standpoint - the next request for the image will cause the file to be overwritten and the modified date updated.
            DateTime modDate = e.HasModifiedDate ? e.GetModifiedDateUTC() : DateTime.MinValue;

            CheckWebConfigEvery5(physicalPath);

            string physicalPath = new CustomDiskCache().GetCachedPath(e.RequestKey, e.SuggestedExtension, e.ResizeImageToStream, modDate, fileLockTimeout);


            context.Items["FinalCachedFile"] = physicalPath;

            
            //Rewrite to cached, resized image.
            context.RewritePath(virtualPath, false);

        }


        /// <summary>
        /// This string contains the contents of a web.conig file that sets URL authorization to "deny all" inside the current directory.
        /// </summary>
        protected const string defaultWebConfigContents =
            "<?xml version=\"1.0\"?>" +
            "<configuration xmlns=\"http://schemas.microsoft.com/.NetConfiguration/v2.0\">" +
            "<system.web><authorization>" +
            "<deny users=\"*\" />" +
            "</authorization></system.web></configuration>";

        protected virtual string getNewWebConfigContents() {
            return defaultWebConfigContents;
        }


        private readonly object _webConfigSyncObj = new object();

        private volatile DateTime _lastCheckedWebConfig = DateTime.MinValue;
        private volatile bool _checkedWebConfigOnce = false;
        /// <summary>
        /// Verifies a Web.config file is present in the specified directory every 5 minutes that the function is called
        /// </summary>
        /// <returns></returns>
        public void CheckWebConfigEvery5(string physicalPath) {
            if (_lastCheckedWebConfig < DateTime.UtcNow.Subtract(new TimeSpan(0, 5, 0))) {
                lock (_webConfigSyncObj) {
                    if (_lastCheckedWebConfig < DateTime.UtcNow.Subtract(new TimeSpan(0, 5, 0)))
                        _checkWebConfig(physicalPath);
                }
            }
        }


        /// <summary>
        /// If CheckWebConfig has never executed, it is executed immediately, but only once. 
        /// Verifies a Web.config file is present in the specified directory, and creates it if needed.
        /// </summary>
        /// <param name="physicalPath"></param>
        protected void CheckWebConfigOnce(string physicalPath) {
            if (_checkedWebConfigOnce) return;
            lock (_webConfigSyncObj) {
                if (_checkedWebConfigOnce) return;
                _checkWebConfig(physicalPath);
            }
        }
        public void CheckWebConfig(string physicalPath) {
            lock (_webConfigSyncObj) {
                _checkWebConfig(physicalPath);
            }
        }
        /// <summary>
        /// Should only be called inside a lock. Creates the cache dir and the web.config file if they are missing. Updates
        /// _lastCheckedWebConfig and _checkedWebConfigOnce
        /// </summary>
        /// <param name="physicalPath"></param>
        protected void _checkWebConfig(string physicalPath){
            try {
                string webConfigPath = physicalPath.TrimEnd('/', '\\') + System.IO.Path.DirectorySeparatorChar + "Web.config";
                if (System.IO.File.Exists(webConfigPath)) return; //Already exists, quit


                //Web.config doesn't exist? make sure the directory exists!
                if (!System.IO.Directory.Exists(physicalPath))
                    System.IO.Directory.CreateDirectory(physicalPath);

                //Create the Web.config file
                System.IO.File.WriteAllText(webConfigPath, getNewWebConfigContents());
                
            } finally {
                _lastCheckedWebConfig = DateTime.UtcNow;
                _checkedWebConfigOnce = true;
            }
        }


    }
}
