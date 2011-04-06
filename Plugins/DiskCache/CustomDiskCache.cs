using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Caching;
using ImageResizer.Util;
using System.IO;

namespace ImageResizer.Plugins.DiskCache {
    public class CustomDiskCache {
        protected string physicalCachePath;
        protected int subfolders;
        public CustomDiskCache(string physicalCachePath, int subfolders) {
            this.physicalCachePath = physicalCachePath;
            this.subfolders = subfolders;
        }


        /// <summary>
        /// Provides string-based locking
        /// </summary>
        protected LockProvider locks = new LockProvider();

        /// <summary>
        /// Provides 
        /// </summary>
        protected CacheIndex index = new CacheIndex();


        public string GetCachedPath(string keyBasis, string extension, ResizeImageDelegate writeCallback, DateTime sourceModifiedUtc, int timeoutMs) {
            //Relative to the cache directory. Not relative to the app or domain root
            string relativePath = new UrlHasher().hash(keyBasis, subfolders, "/") + '.' + extension;
            //Physical path
            string physicalPath = physicalCachePath.TrimEnd('\\', '/') + System.IO.Path.DirectorySeparatorChar +
                    relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar);


            bool hasModifiedDate = !sourceModifiedUtc.Equals(DateTime.MinValue);
            
            //On the first check, verify the file exists using System.IO directly (the last 'true' parameter)
            if ((!hasModifiedDate && index.Exists(relativePath, physicalPath, true)) || !index.ModifiedMatches(sourceModifiedUtc, relativePath, physicalPath, true)) {
                
                //Lock execution using relativePath as the sync basis. Ignore casing differences.
                locks.TryLock(relativePath.ToUpperInvariant(), delegate() {
                    
                    //On the second check, use cached data for speed. The cached data should be updated if another thread updated a file.
                    if ((!hasModifiedDate && index.Exists(relativePath, physicalPath, false)) || !index.ModifiedMatches(sourceModifiedUtc, relativePath, physicalPath, false)) {
                        
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
                        index.UpdateTimes(relativePath, sourceModifiedUtc, createdUtc);
                    }
                },
                delegate() {
                    //On failure
                    throw new ApplicationException("Failed to acquire a lock on file \"" + physicalPath + "\" within " + timeoutMs + "ms. Caching failed.");
                }, timeoutMs);
            }
            return physicalPath;
        }
    }
}
