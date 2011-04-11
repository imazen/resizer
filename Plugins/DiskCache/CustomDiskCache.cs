using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Caching;
using ImageResizer.Util;
using System.IO;

namespace ImageResizer.Plugins.DiskCache {
    public delegate void CacheResultHandler(CustomDiskCache sender, CacheResult r);

    /// <summary>
    /// Handles access to a disk-based file cache. Handles locking and versioning. 
    /// Supports subfolders for scalability.
    /// </summary>
    public class CustomDiskCache {
        protected string physicalCachePath;

        public string PhysicalCachePath {
            get { return physicalCachePath; }
        }
        protected int subfolders;
        protected bool hashModifiedDate;
        public CustomDiskCache(string physicalCachePath, int subfolders, bool hashModifiedDate) {
            this.physicalCachePath = physicalCachePath;
            this.subfolders = subfolders;
            this.hashModifiedDate = hashModifiedDate;
        }
        /// <summary>
        /// Fired immediately before GetCachedFile return the result value. 
        /// </summary>
        public event CacheResultHandler CacheResultReturned; 


        protected LockProvider locks = new LockProvider();
        /// <summary>
        /// Provides string-based locking for file write access.
        /// </summary>
        public LockProvider Locks {
            get { return locks; }
        }


        private CacheIndex index = new CacheIndex();
        /// <summary>
        /// Provides an in-memory index of the cache.
        /// </summary>
        public CacheIndex Index {
            get { return index; }
        }
        
        


        public CacheResult GetCachedFile(string keyBasis, string extension, ResizeImageDelegate writeCallback, DateTime sourceModifiedUtc, int timeoutMs) {
            bool hasModifiedDate = !sourceModifiedUtc.Equals(DateTime.MinValue);


            //Hash the modified date if needed.
            if (hashModifiedDate && hasModifiedDate)
                keyBasis += "|" + sourceModifiedUtc.Ticks.ToString();

            //Relative to the cache directory. Not relative to the app or domain root
            string relativePath = new UrlHasher().hash(keyBasis, subfolders, "/") + '.' + extension;

            //Physical path
            string physicalPath = PhysicalCachePath.TrimEnd('\\', '/') + System.IO.Path.DirectorySeparatorChar +
                    relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar);


            CacheResult result = new CacheResult(CacheQueryResult.Hit, physicalPath,relativePath);
            
            //On the first check, verify the file exists using System.IO directly (the last 'true' parameter)
            if (((!hasModifiedDate || hashModifiedDate) && Index.existsCertain(relativePath, physicalPath)) || !Index.modifiedDateMatchesCertainExists(sourceModifiedUtc, relativePath, physicalPath)) {
                
                //Lock execution using relativePath as the sync basis. Ignore casing differences.
                if (!Locks.TryExecute(relativePath.ToUpperInvariant(), timeoutMs,
                    delegate() {

                        //On the second check, use cached data for speed. The cached data should be updated if another thread updated a file.
                        if (((!hasModifiedDate || hashModifiedDate) && Index.exists(relativePath, physicalPath)) || !Index.modifiedDateMatches(sourceModifiedUtc, relativePath, physicalPath)) {

                            //Create subdirectory if needed.
                            if (!Directory.Exists(Path.GetDirectoryName(physicalPath))) Directory.CreateDirectory(Path.GetDirectoryName(physicalPath));

                            //Open stream 
                            System.IO.FileStream fs = new FileStream(physicalPath, FileMode.Create, FileAccess.Write);
                            using (fs) {
                                //Run callback to write the cached data
                                writeCallback(fs);
                            }
                            DateTime createdUtc = DateTime.UtcNow;
                            //Update the write time to match - this is how we know whether they are in sync.
                            if (hasModifiedDate) System.IO.File.SetLastWriteTimeUtc(physicalPath, sourceModifiedUtc);
                            //Set the created date, so we know the last time we updated the cache.s
                            System.IO.File.SetCreationTimeUtc(physicalPath, createdUtc);
                            //Update index
                            Index.setCachedFileInfo(relativePath, new CachedFileInfo(sourceModifiedUtc, createdUtc, createdUtc));
                            //This was a cache miss
                            result.Result = CacheQueryResult.Miss;
                        }
                    })) {
                    //On failure
                    result.Result = CacheQueryResult.Failed;
                }
                
            }
            //Fire event
            if (CacheResultReturned != null) CacheResultReturned(this, result);
            return result;
        }


    }
}
