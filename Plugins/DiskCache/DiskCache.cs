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

        public DiskCache(string dir, int subfolders, bool autoClean, int maxImagesPerFolder, bool debugMode, int fileLockTimeout){
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

        /// <summary>
        /// Returns the physical path of the image cache dir. Calculated from imageresizer.diskcache.dir (yrl form). throws an exception if missing
        /// </summary>
        /// <returns></returns>
        public string GetCacheDir()
        {
            yrl conv = (!string.IsNullOrEmpty(dir)) ? conv = yrl.FromString(dir) : null;

            if (yrl.IsNullOrEmpty(conv))  throw new DiskCacheException("The 'diskcache.dir' setting has an invalid value. A directory name is required for image caching to work.");
            
            return conv.Local;
        }
        
        

        private long filesUpdatedSinceCleanup = 0;
        private bool hasCleanedUp = false;

        public bool CanProcess(HttpContext current, IResponseArgs e) {
            return IsConfigurationValid();//Add support for nocache
        }

        public string getRelativeCachedFilename(IResponseArgs e) {
            return new UrlHasher().hash(e.RequestKey,subfolders,"/") + '.' + e.SuggestedExtension;
        }
        /// <summary>
        /// The only objects in this collection should be for open files. 
        /// </summary>
        private static Dictionary<String, Object> fileLocks = new Dictionary<string, object>(StringComparer.Ordinal);


        public void Process(HttpContext context, IResponseArgs e) {
            string relativePath = getRelativeCachedFilename(e); //Relative to the cache directory. Not relative to the app or domain root
            string virtualPath = VirtualCacheDir.TrimEnd('/') + '/' + relativePath; //Relative to the domain
            string physicalPath = HostingEnvironment.MapPath(virtualPath); //Physical path
               
        
             PrepareCacheDir();
            //Fixed - implement locking so concurrent requests for the same file don't cause an I/O issue.
            if ((!e.HasModifiedDate && Exists(relativePath,physicalPath)) || !IsCachedVersionValid(e.GetModifiedDateUTC(), relativePath,physicalPath))
            {
                //Create or obtain a blank object for locking purposes. Store or retrieve using the filename as a key.
                string key = relativePath.ToUpperInvariant();

                object fileLock = null;
                lock (fileLocks) //We have to lock the dictionary, since otherwise two locks for the same file could be created and assigned at the same time. (i.e, between TryGetValue and the assignment)
                {
                    //If it doesn't exist
                    if (!fileLocks.TryGetValue(key, out fileLock))
                        fileLocks[key] = fileLock = new Object(); //make a new lock!
                }
                //We should now have an exclusive lock for this filename.  We're only going to hold this thread open for fileLockTimeout ms - too many threads blocked kills performance.
                //We don't use a standard lock{}, since that could block as long as the underlying I/O calls.
                if (System.Threading.Monitor.TryEnter(fileLock,fileLockTimeout))
                {
                    try
                    {
                        DateTime modDate = e.HasModifiedDate ? e.GetModifiedDateUTC() : DateTime.MinValue;
                        if ((!e.HasModifiedDate && Exists(physicalPath)) || !IsCachedVersionValid(modDate, physicalPath))
                        {
                            //Create subdirectory if needed.
                            if (!Directory.Exists(Path.GetDirectoryName(physicalPath))) Directory.CreateDirectory(Path.GetDirectoryName(physicalPath));
                            
                            //Open stream 
                            System.IO.FileStream fs = new FileStream(physicalPath, FileMode.Create, FileAccess.Write);
                            using (fs) {
                                //Run callback to process and encode image
                                e.ResizeImageToStream(fs);
                            }
                            filesUpdatedSinceCleanup++;
                            //Update the write time to match - this is how we know whether they are in sync.
                            if (e.HasModifiedDate) System.IO.File.SetLastWriteTimeUtc(physicalPath,modDate);
                            System.IO.File.SetCreationTimeUtc(physicalPath, DateTime.UtcNow);
                            //TODO: tell the cache index what we have done
                        }
                    }
                    finally
                    {
                        //release lock
                        System.Threading.Monitor.Exit(fileLock);
                    }
                }
                else
                {
                    //Only one resize operation on a source file occurs at a time.
                    //fileLockTimeout was not enough to acquire a lock on the file
                    throw new ApplicationException("Failed to acquire a lock on file \"" + physicalPath + "\" within " + fileLockTimeout + "ms. Image resizing failed.");
                }
                //Attempt cleanup of lock objects. TryEnter() failes if there is anybody else holding the lock at that moment
                if (System.Threading.Monitor.TryEnter(fileLocks))
                {
                    try
                    {
                        if (System.Threading.Monitor.TryEnter(fileLock)) //Try entering on the file-specific lock. 
                        {
                            try{ fileLocks.Remove(key);  } //It succeeds, so no-one else is locking on it - clean it up.
                            finally { System.Threading.Monitor.Exit(fileLock); }
                        }
                    }
                    finally { System.Threading.Monitor.Exit(fileLocks); }
                }
                //Ideally the only objects in fileLocks will be open operations now.
            }

            context.Items["FinalCachedFile"] = physicalPath;

            
            //Rewrite to cached, resized image.
            context.RewritePath(virtualPath, false);

        }


        
        /// <summary>
        /// Returns true if the specified file exists. 
        /// </summary>
        /// <returns></returns>
        public bool Exists(string localCachedFile){
            if (string.IsNullOrEmpty(localCachedFile)) return false;
            if (!System.IO.File.Exists(localCachedFile)) return false;
            return true;
        }
        /// <summary>
        ///  Returns true if localCachedFile exists and matches sourceDataModifiedUTC.
        /// </summary>
        /// <param name="sourceDataModifiedUTC"></param>
        /// <param name="localCachedFile"></param>
        /// <returns></returns>
        public bool IsCachedVersionValid(DateTime sourceDataModifiedUTC, string localCachedFile)
        {
            if (string.IsNullOrEmpty(localCachedFile)) return false;
            if (!Exists(localCachedFile)) return false;


            //When we save cached files to disk, we set the write time to that of the source file.
            //This allows us to track if the source file has changed.

            DateTime cached = System.IO.File.GetLastWriteTimeUtc(localCachedFile);
            return RoughCompare(cached, sourceDataModifiedUTC);
        }
        /// <summary>
        /// Returns true if both dates are equal (to the nearest 200th of a second)
        /// </summary>
        /// <param name="modifiedOn"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        private static bool RoughCompare(DateTime d1, DateTime d2) {
            return Math.Abs(d1.Ticks - d2.Ticks) < TimeSpan.TicksPerMillisecond * 5;
        }


    
        /// <summary>
        /// This string contains the contents of a web.conig file that sets URL authorization to "deny all" inside the current directory.
        /// </summary>
        private const string webConfigFile =
            "<?xml version=\"1.0\"?>" +
            "<configuration xmlns=\"http://schemas.microsoft.com/.NetConfiguration/v2.0\">" +
            "<system.web><authorization>" +
            "<deny users=\"*\" />" +
            "</authorization></system.web></configuration>";


        private static readonly object webConfigSyncObj = new object();

        /// <summary>
        /// Creates the directory for caching if needed, and performs 'garbage collection'
        /// Throws a DiskCacheException if the cache direcotry isn't specified in web.config
        /// Creates a web.config file in the caching directory to prevent direct access.
        /// </summary>
        /// <returns></returns>
        public void PrepareCacheDir()
        {
            string dir = GetCacheDir();



            //Add URL authorization protection using a web.config file.
            yrl wc = yrl.Combine(new yrl(dir), new yrl("Web.config"));
            if (!wc.FileExists) {
                lock (webConfigSyncObj) {
                    //Create the cache directory if it doesn't exist.
                    if (!System.IO.Directory.Exists(dir))
                        System.IO.Directory.CreateDirectory(dir);
                    if (!wc.FileExists){
                        System.IO.File.WriteAllText(wc.Local, webConfigFile);
                    }
                }
            }



        /// <summary>
        /// Returns true if the image caching directory (GetCacheDir()) exists.
        /// </summary>
        /// <returns></returns>
        public bool CacheDirExists()
        {
            string dir = GetCacheDir();
            if (!string.IsNullOrEmpty(dir))
            {
                return System.IO.Directory.Exists(dir);
            }
            return false;
        }









        public IPlugin Install(Config c) {
            throw new NotImplementedException();
        }

        public bool Uninstall(Config c) {
            throw new NotImplementedException();
        }
    }
}
