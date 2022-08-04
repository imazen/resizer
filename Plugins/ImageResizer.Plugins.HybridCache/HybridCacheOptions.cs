using System;

namespace ImageResizer.Plugins.HybridCache
{
    public class HybridCacheOptions
    {
        /// <summary>
        /// Where to store the cached files and the database
        /// </summary>
        public string DiskCacheDirectory { get; set; }

        /// <summary>
        /// How many RAM bytes to use when writing asynchronously to disk before we switch to writing synchronously.
        /// Defaults to 100MiB. 
        /// </summary>
        public long QueueSizeLimitInBytes { get; set; } = 100 * 1024 * 1024;

        /// <summary>
        /// Defaults to 1 GiB. Don't set below 9MB or no files will be cached, since 9MB is reserved just for empty directory entries.
        /// </summary>
        public long CacheSizeLimitInBytes { get; set; } = 1 * 1024 * 1024 * 1024;
        
        /// <summary>
        /// The minimum number of bytes to free when running a cleanup task. Defaults to 1MiB;
        /// </summary>
        public long MinCleanupBytes { get; set; } = 1 * 1024 * 1024;
        
        /// <summary>
        /// The minimum age of files to delete. Defaults to 10 seconds.
        /// </summary>
        public TimeSpan MinAgeToDelete { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// The number of shards to split the metabase into. More shards means more open log files, slower shutdown.
        /// But more shards also mean less lock contention and faster start time for individual cached requests.
        /// Defaults to 8. You have to delete the database directory each time you change this number.
        /// </summary>
        public int DatabaseShards { get; set; } = 8;

        public HybridCacheOptions(string cacheDir)
        {
            DiskCacheDirectory = cacheDir;
        }
    }
}