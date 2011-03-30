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
using fbs;
using System.IO;
using fbs.ImageResizer.Caching;
using fbs.ImageResizer.Configuration;

namespace fbs.ImageResizer.Plugins.DiskCache
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
    /// Extending this to work for database-sourced (or anything) vs disk-source data 
    /// could be done by making alternative UpdateCachedVersionIfNeeded and IsCachedVersionValid
    /// methods that accept DateTime instances instead of filenames. 
    /// 
    /// </summary>
    public class DiskCache: ICache
    {
        protected string dir;
        protected int subfolders;
        bool autoClean;
        int maxImagesPerFolder;
        bool debugMode;
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
        /// Uses the defaults from the imageresizer.diskcache configuration section
        /// </summary>
        public DiskCache(Config c){
            debugMode = c.get("diskcache.debugMode",false);
            autoClean = c.get("diskcache.autoClean",false);
            dir = c.get("diskcache.dir","~/imagecache");
            maxImagesPerFolder =  c.get("diskcache.maxImagesPerFolder", 8000);
            subfolders = c.get("diskcache.subfolders",0);
            fileLockTimeout = c.get("diskcache.fileLockTimeout", 10000); //10s
        }
       
        /// <summary>
        /// Returns the physical path of the image cache dir. Calcualted from imageresizer.diskcache.dir (yrl form). throws an exception if missing
        /// </summary>
        /// <returns></returns>
        public string GetCacheDir()
        {
            yrl conv = (!string.IsNullOrEmpty(dir)) ? conv = yrl.FromString(dir) : null;

            if (yrl.IsNullOrEmpty(conv))  throw new DiskCacheException("The 'diskcache.dir' setting has an invalid value. A directory name is required for image caching to work.");
            
            return conv.Local;
        }
        
        /// <summary>
        /// Returns the maxium number of images that can be cached per folder
        /// </summary>
        /// <returns></returns>
        public  int GetMaxCachedFilesPerFolder()
        {
            return maxImagesPerFolder;
        }


        private long filesUpdatedSinceCleanup = 0;
        private bool hasCleanedUp = false;

        public void Process(HttpContext context, IResponseArgs e) {
            string cachedFilename = null; //Build using hash utils
        
        /// <summary>
        /// Checks if an update is needed on the specified file... calls the method if needed.
        /// Fixed: Implement locking to prevent I/O conflicts on concurrent inital request
        /// Returns false if a lock on the source file could not be acquired within fileLockTimeout ms. -1 for indefinite wait - not reccomended!
        /// Only one resize operation can be performed on a source file at a time. This method enforces that, and should eliminte costly I/O 'access denied' messages.
        /// Of course, locking based on source filename also eliminates writing contention on cached files..
        /// </summary>
             PrepareCacheDir();
            //Fixed - implement locking so concurrent requests for the same file don't cause an I/O issue.
            if ((!e.HasModifiedDate && IsCachedVersionValid(cachedFilename)) || !IsCachedVersionValid(e.GetModifiedDateUTC(), cachedFilename))
            {
                //Create or obtain a blank object for locking purposes. Store or retrieve using the filename as a key.
                string key = cachedFilename.ToLower();

                object fileLock = null;
                lock (fileLocks) //We have to lock the dictionary, since otherwise two locks for the same file could be created and assigned at the same time. (i.e, between ContainsKey and the assignment)
                {
                    if (fileLocks.ContainsKey(key))
                        fileLock = fileLocks[key];
                    else
                        fileLocks[key] = fileLock = new Object();//make new lock
                }
                //We should now have an exclusive lock for this filename.  We're only going to hold this thread open for fileLockTimeout ms - too many threads blocked kills performance.
                //We don't use a standard lock{}, since that could block as long as the underlying I/O calls.
                if (System.Threading.Monitor.TryEnter(fileLock,fileLockTimeout))
                {
                    try
                    {
                        DateTime modDate = e.HasModifiedDate ? e.GetModifiedDateUTC() : DateTime.MinValue;
                        if ((!e.HasModifiedDate && IsCachedVersionValid(cachedFilename)) || !IsCachedVersionValid(modDate, cachedFilename))
                        {
                            //Create subdirectory if needed.
                            if (!Directory.Exists(Path.GetDirectoryName(cachedFilename))) Directory.CreateDirectory(Path.GetDirectoryName(cachedFilename));
                            
                            //Open stream 
                            System.IO.FileStream fs = new FileStream(cachedFilename, FileMode.Create, FileAccess.Write);
                            using (fs) {
                                //Run callback to process and encode image
                                e.ResizeImageToStream(fs);
                            }
                            filesUpdatedSinceCleanup++;
                            //Update the write time to match - this is how we know whether they are in sync.
                            if (e.HasModifiedDate) System.IO.File.SetLastWriteTimeUtc(cachedFilename,modDate);
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
                    throw new ApplicationException("Failed to acquire a lock on file \"" + cachedFilename + "\" within " + fileLockTimeout + "ms. Image resizing failed.");
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

            context.Items["FinalCachedFile"] = cachedFilename;

            //Now it is time to rewrite the request to serve from the cache
            //Get domain-relative path of cached file.
            string virtualPath = yrl.GetAppFolderName().TrimEnd(new char[] { '/' }) + "/" + yrl.FromPhysicalString(cachedFilename).ToString();

            //Rewrite to cached, resized image.
            context.RewritePath(virtualPath, false);

        }
        /// <summary>
        /// The only objects in this collection should be for open files. 
        /// </summary>
        private static Dictionary<String, Object> fileLocks = new Dictionary<string, object>();

        private static void LogWarning(String message)
        {
            HttpContext.Current.Trace.Warn("ImageResizer", message);
        }
        private static void LogException(Exception e)
        {
            HttpContext.Current.Trace.Warn("ImageResizer", e.Message, e);// Event.CreateExceptionEvent(e).SaveAsync();
        }

        /// <summary>
        /// Assumes localSourceFile exists. Returns true if localCachedFile exists and matches the last write time of localSourceFile.
        /// </summary>
        /// <param name="localSourceFile">full physical path of original file</param>
        /// <param name="cachedFilename">full physical path of cached file.</param>
        /// <returns></returns>
        public bool IsCachedVersionValid(string localSourceFile, string localCachedFile)
        {
            if (debugMode) return false;
            if (localCachedFile == null) return false;
            if (!System.IO.File.Exists(localCachedFile)) return false;

            

            //When we save thumbnail files to disk, we set the write time to that of the source file.
            //This allows us to track if the source file has changed.

            DateTime cached = System.IO.File.GetLastWriteTimeUtc(localCachedFile);
            DateTime source = System.IO.File.GetLastWriteTimeUtc(localSourceFile);
            return RoughCompare(cached, source);
        }

        
        /// <summary>
        /// Assumes localSourceFile exists. Returns true if localCachedFile exists and DiskCacheAlwaysInvalid is false.
        /// </summary>
        /// <param name="localSourceFile">full physical path of original file</param>
        /// <param name="cachedFilename">full physical path of cached file.</param>
        /// <returns></returns>
        public bool IsCachedVersionValid(string localCachedFile){
            if (debugMode) return false;

            if (localCachedFile == null) return false;
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
            if (debugMode) return false;
            if (localCachedFile == null) return false;
            if (!System.IO.File.Exists(localCachedFile)) return false;


            //When we save thumbnail files to disk, we set the write time to that of the source file.
            //This allows us to track if the source file has changed.

            DateTime cached = System.IO.File.GetLastWriteTimeUtc(localCachedFile);
            return RoughCompare(cached, sourceDataModifiedUTC);
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

            //Create the cache directory if it doesn't exist.
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            //Add URL authorization protection using a web.config file.
            yrl wc = yrl.Combine(new yrl(dir), new yrl("Web.config"));
            if (!wc.FileExists) {
                lock (webConfigSyncObj) {
                    if (!wc.FileExists){
                        System.IO.File.WriteAllText(wc.Local, webConfigFile);
                    }
                }
            }



            //TODO: fix cleanup system
            //Perform cleanup if needed. Clear 1/10 of the files if we are running low.
            if (maxImagesPerFolder > 0  && this.autoClean)
            {  
                //Only test for cleanup if we've added 1/15 of the quota since last check. This may make things a little less precise, but provides a 
                //huge perfomance boost - GetFiles() can be very slow on some machines
                if (filesUpdatedSinceCleanup > maxImagesPerFolder / 15 || !hasCleanedUp)
                {
                    TrimDirectoryFiles(dir, maxImagesPerFolder - 1, (maxImagesPerFolder / 10));
                }
            }
        }

        

        /// <summary>
        /// Deletes least-used files from the directory (if needed)
        /// Throws an exception if cleanup fails.
        /// Returns true if any files were deleted.
        /// </summary>
        /// <param name="dir">The directory to clean up</param>
        /// <param name="maxCount">The maximum number of files to leave in the directory. Does nothing if this is less than 0</param>
        /// <param name="deleteExtra">How many extra files to delete if deletions are required</param>
        /// <returns></returns>
        public bool TrimDirectoryFiles(string dir, int maxCount, int deleteExtra)
        {
            if (maxCount < 0) return false;

            // if (deleteExtra > maxCount) throw warning
            
            string[] files = System.IO.Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly);
            if (files.Length <= maxCount)
            {
                hasCleanedUp = true;
                return false;
            }

           

            //Oops, look like we have to clean up a little.

            List<KeyValuePair<string, DateTime>> fileinfo = new List<KeyValuePair<string, DateTime>>(files.Length);

            //Sory by last access time
            foreach (string s in files)
            {
                fileinfo.Add(new KeyValuePair<string, DateTime>(s, System.IO.File.GetLastAccessTimeUtc(s)));
            }
            fileinfo.Sort(CompareFiles);


            int deleteCount = files.Length - maxCount + deleteExtra;

            bool deletedSome = false;
            //Delete files, Least recently used order
            for (int i = 0; i < deleteCount && i < fileinfo.Count; i++)
            {
                //Never delete .config files
                if (System.IO.Path.GetExtension(fileinfo[i].Key).Equals("config", StringComparison.OrdinalIgnoreCase)){
                    deleteCount++; //Just delete an extra file instead...
                    continue;
                }
                //All other files can be deleted.
                try
                {
                    System.IO.File.Delete(fileinfo[i].Key);
                    deletedSome = true;
                }
                catch (IOException ioe)
                {
                    if (i >= fileinfo.Count - 1)
                    {
                        //Looks like we're at the end.
                        throw new DiskCacheException("Couldn't delete enough files to make room for new images. Please increase MaxCachedFiles or solve I/O isses/", ioe);
                       
                    }

                    //Try an extra candidate to make up for this missing one
                    deleteCount++;
                    LogException(ioe);

                }
            }

            hasCleanedUp = true;
            filesUpdatedSinceCleanup = 0;
            return deletedSome;
        }
        /// <summary>
        /// Compares the file dates on the arguments
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private static int CompareFiles(KeyValuePair<string, DateTime> x, KeyValuePair<string, DateTime> y)
        {
            return x.Value.CompareTo(y.Value);
        }


        /// <summary>
        /// Returns true if both dates are equal (to the nearest 200th of a second)
        /// </summary>
        /// <param name="modifiedOn"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        private static bool RoughCompare(DateTime d1, DateTime d2)
        {
            return (new TimeSpan((long)Math.Abs(d1.Ticks - d2.Ticks)).TotalMilliseconds <= 5);
        }



        /// <summary>
        /// Returns true if successful, false if not.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
       /* public static bool SendDiskCache(HttpContext context, string identifier)
        {
            string filename = GetFilenameFromId(identifier);
            if (filename == null) return false;
            if (!System.IO.File.Exists(filename)) return false;

            context.Response.TransmitFile(filename);
            return true;
        }*/

        /// <summary>
        /// Clears the cache directory. Returns true if successful. (In-use files make this rare).
        /// </summary>
        /// <returns></returns>
        public bool ClearCacheDir()
        {
            return TrimDirectoryFiles(GetCacheDir(), 0, 0);
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

        

        /// <summary>
        /// Returns the number of files inside the image cache directory (recursive traversal)
        /// </summary>
        /// <returns></returns>
        public int GetCacheDirFilesCount()
        {
            if (CacheDirExists())
            {
                string dir = GetCacheDir();
                string[] files = System.IO.Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
                return files.Length;
            }
            return 0;
        }
        /// <summary>
        /// Returns the summation of the size of the indiviual files in the image cache directory (recursive traversal)
        /// </summary>
        /// <returns></returns>
        public long GetCacheDirTotalSize()
        {
            if (CacheDirExists())
            {
                string dir = GetCacheDir();
                long totalSize = 0;
                string[] files = System.IO.Directory.GetFiles(dir, "*", SearchOption.AllDirectories);
                foreach (string s in files)
                {

                    FileInfo fi = new FileInfo(s);
                    totalSize += fi.Length;
                }
                return totalSize;
            }
            return 0;

        }
        /// <summary>
        /// Returns the average size of a file in the image cache directory. Expensive, calls GetCacheDirFilesCount() and GetCacheDirTotalSize()
        /// </summary>
        /// <returns></returns>
        public int GetAverageCachedFileSize()
        {
            double files = GetCacheDirFilesCount();
            if (files < 1) return 0;

            return (int)Math.Round((double)GetCacheDirTotalSize() / files);
        }


    }
}
