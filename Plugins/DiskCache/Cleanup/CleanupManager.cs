using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ImageResizer.Plugins.DiskCache.Cleanup;

namespace ImageResizer.Plugins.DiskCache {

   
    public class CleanupManager:IDisposable {
        protected CustomDiskCache cache = null;
        protected CleanupStrategy cs = null;
        protected CleanupQueue queue = null;
        protected CleanupWorker worker = null;
        

        public CleanupManager(CustomDiskCache cache, CleanupStrategy cs) {
            this.cache = cache;
            this.cs = cs;
            queue = new CleanupQueue();
            //Called each request
            cache.CacheResultReturned += delegate(CustomDiskCache sender, CacheResult r) {
                if (r.Result == CacheQueryResult.Miss)
                    this.AddedFile(r.RelativePath); //It was either updated or added.
                else
                    this.BeLazy();
            };
            //Called when the filesystem changes unexpectedly.
            cache.Index.FileDisappeared += delegate(string relativePath, string physicalPath) {
                //Stop everything ASAP and start a brand new cleaning run.
                queue.ReplaceWith(new CleanupWorkItem(CleanupWorkItem.Kind.CleanFolderRecursive, "", cache.PhysicalCachePath));
            };

            worker = new CleanupWorker(cs,queue,cache);
        }
        

        

        /// <summary>
        /// Notifies the CleanupManager that a request is in process. Helps CleanupManager optimize background work so it doesn't interfere with request processing.
        /// </summary>
        public void BeLazy() {
            worker.BeLazy();
        }
        /// <summary>
        /// Notifies the CleanupManager that a file was added under the specified relative path. Allows CleanupManager to detect when a folder needs cleanup work.
        /// </summary>
        /// <param name="relativePath"></param>
        public void AddedFile(string relativePath) {
            int slash = relativePath.LastIndexOf('/');
            string folder = slash > -1 ? relativePath.Substring(0, slash) : "";
            char c = System.IO.Path.DirectorySeparatorChar;
            queue.Queue(new CleanupWorkItem(CleanupWorkItem.Kind.CleanFolderRecursive, folder, cache.PhysicalCachePath.TrimEnd(c) + c + folder.Replace('/',c).Replace('\\',c).Trim(c)));
            worker.MayHaveWork();
        }

        public void CleanAll() {
            queue.Queue(new CleanupWorkItem(CleanupWorkItem.Kind.CleanFolderRecursive, "", cache.PhysicalCachePath));
            worker.MayHaveWork();
        }
        

        public void Dispose() {
            worker.Dispose();
        }
    }
   
    

        

        ///// <summary>
        ///// Deletes least-used files from the directory (if needed)
        ///// Throws an exception if cleanup fails.
        ///// Returns true if any files were deleted.
        ///// </summary>
        ///// <param name="dir">The directory to clean up</param>
        ///// <param name="maxCount">The maximum number of files to leave in the directory. Does nothing if this is less than 0</param>
        ///// <param name="deleteExtra">How many extra files to delete if deletions are required</param>
        ///// <returns></returns>
        //public bool TrimDirectoryFiles(string dir, int maxCount, int deleteExtra)
        //{
        //    if (maxCount < 0) return false;

        //    // if (deleteExtra > maxCount) throw warning
            
        //    string[] files = System.IO.Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly);
        //    if (files.Length <= maxCount)
        //    {
        //        hasCleanedUp = true;
        //        return false;
        //    }

           

        //    //Oops, look like we have to clean up a little.

        //    List<KeyValuePair<string, DateTime>> fileinfo = new List<KeyValuePair<string, DateTime>>(files.Length);

        //    //Sory by last access time
        //    foreach (string s in files)
        //    {
        //        fileinfo.Add(new KeyValuePair<string, DateTime>(s, System.IO.File.GetLastAccessTimeUtc(s)));
        //    }
        //    fileinfo.Sort(CompareFiles);


        //    int deleteCount = files.Length - maxCount + deleteExtra;

        //    bool deletedSome = false;
        //    //Delete files, Least recently used order
        //    for (int i = 0; i < deleteCount && i < fileinfo.Count; i++)
        //    {
        //        //Never delete .config files
        //        if (System.IO.Path.GetExtension(fileinfo[i].Key).Equals("config", StringComparison.OrdinalIgnoreCase)){
        //            deleteCount++; //Just delete an extra file instead...
        //            continue;
        //        }
        //        //All other files can be deleted.
        //        try
        //        {
        //            System.IO.File.Delete(fileinfo[i].Key);
        //            deletedSome = true;
        //        }
        //        catch (IOException ioe)
        //        {
        //            if (i >= fileinfo.Count - 1)
        //            {
        //                //Looks like we're at the end.
        //                throw new DiskCacheException("Couldn't delete enough files to make room for new images. Please increase MaxCachedFiles or solve I/O isses/", ioe);
                       
        //            }

        //            //Try an extra candidate to make up for this missing one
        //            deleteCount++;
        //            LogException(ioe);

        //        }
        //    }

        //    hasCleanedUp = true;
        //    filesUpdatedSinceCleanup = 0;
        //    return deletedSome;
        //}
        ///// <summary>
        ///// Compares the file dates on the arguments
        ///// </summary>
        ///// <param name="x"></param>
        ///// <param name="y"></param>
        ///// <returns></returns>
        //private static int CompareFiles(KeyValuePair<string, DateTime> x, KeyValuePair<string, DateTime> y)
        //{
        //    return x.Value.CompareTo(y.Value);
        //}






        ///// <summary>
        ///// Clears the cache directory. Returns true if successful. (In-use files make this rare).
        ///// </summary>
        ///// <returns></returns>
        //public bool ClearCacheDir()
        //{
        //    return TrimDirectoryFiles(GetCacheDir(), 0, 0);
        //}
        
      

}
