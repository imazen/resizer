using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins.DiskCache {
    public enum WorkItemType {
        UpdateCacheRecord,
        PopulateFolderCache,

    }
    /// <summary>
    /// Cleanup strategy
    /// 
    /// After a file is added to the cache, add a work item to the queue to verify that folder is within the limits.
    /// On startup, populate the cache index with data.
    /// 
    /// //OnBeforeWrite
    /// //AfterWriteFile - 
    /// 
    /// </summary>
    public class CleanupWorker {
        public CleanupWorker(CacheIndex index, LockProvider locks) {
        }
        private int minimumAgeForCleanup;
        /// <summary>
        /// The minimum age a file must have to be cleaned up. (in minutes) (age = createdUtc vs NowUtc difference)
        /// </summary>
        public int MinimumAgeForCleanup {
            get { return minimumAgeForCleanup; }
            set { minimumAgeForCleanup = value; }
        }



        public void UpdatedFile(string relativePath) {
        }
        public void UsedFile(string relativePath) {
        }

        public void DoWorkSegment() {
            //Should work on only one subfolder at a time.

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
