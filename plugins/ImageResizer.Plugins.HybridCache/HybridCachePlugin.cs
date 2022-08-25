using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using ImageResizer.Configuration;
using ImageResizer.Util;
using Imazen.Common.Extensibility.StreamCache;
using Imazen.Common.Instrumentation.Support.InfoAccumulators;
using Imazen.Common.Issues;
using Imazen.HybridCache;
using Imazen.HybridCache.MetaStore;
using Microsoft.Extensions.Logging;

namespace ImageResizer.Plugins.HybridCache
{
    public class HybridCachePlugin : IAsyncTyrantCache, IPlugin, IPluginInfo, IPluginRequiresShutdown
    {
        //TODO: Fix multiple instances issue (each HttpModule can have many instances)
        public HybridCachePlugin()
        {
        }

        public HybridCachePlugin(HybridCacheOptions options, ILogger logger)
        {
            _cacheOptions = options;
            this._logger = logger;
        }

        private Config _c;
        private readonly ILogger _logger;
        private Imazen.HybridCache.HybridCache _cache;
        private readonly HybridCacheOptions _cacheOptions = new HybridCacheOptions(null);


        private static string GetLegacyDiskCachePhysicalPath(Config config)
        {
            var defaultDir = (HostingEnvironment.ApplicationVirtualPath?.TrimEnd('/') ?? "") + "/imagecache";
            var dir = config.get("diskcache.dir", defaultDir);
            dir = string.IsNullOrEmpty(dir) ? null : PathUtils.ResolveAppRelativeAssumeAppRelative(dir);
            return !string.IsNullOrEmpty(dir) ? HostingEnvironment.MapPath(dir) : null;
        }
        
        private static readonly ConcurrentDictionary<string, HybridCachePlugin> firstInstancePerPath = new ConcurrentDictionary<string, HybridCachePlugin>();

        private static bool conflictsExist = false;

        private void LoadSettings(Config c)
        {
            _cacheOptions.DiskCacheDirectory = c.get("hybridCache.cacheLocation", _cacheOptions.DiskCacheDirectory);
            _cacheOptions.CacheSizeLimitInBytes = c.get("hybridCache.cacheMaxSizeBytes", _cacheOptions.CacheSizeLimitInBytes);
            _cacheOptions.DatabaseShards = c.get("hybridCache.shardCount", _cacheOptions.DatabaseShards);
            _cacheOptions.QueueSizeLimitInBytes = c.get("hybridCache.writeQueueLimitBytes", _cacheOptions.QueueSizeLimitInBytes);
            _cacheOptions.MinCleanupBytes = c.get("hybridCache.minCleanupBytes", _cacheOptions.MinCleanupBytes);

            if (firstInstancePerPath.AddOrUpdate(DiskCacheDirectory, this, (k, v) => v) != this)
            {
                conflictsExist = true;
            }

            var legacyCache = GetLegacyDiskCachePhysicalPath(c);
            if (Directory.Exists(legacyCache)) throw new ApplicationException("Legacy disk cache directory found at " + legacyCache + ". Please delete this directory (to prevent unauthorized access) and restart the application.");
            
        }

        private string GetDefaultCacheLocation() {
            var subfolder = $"imageresizer_cache_{Math.Abs(PathUtils.AppPhysicalPath.GetHashCode())}";
            return Path.Combine(Path.GetTempPath(), subfolder);
        }


        private string ResolveCacheLocation(string virtualOrRelativeOrPhysicalPath)
        {
            var s = virtualOrRelativeOrPhysicalPath;
            //If it starts with a tilde, we gotta resolve the app prefix
            if (string.IsNullOrEmpty(s)) return GetDefaultCacheLocation();

            if (s.StartsWith("~"))
            {
                //Clearly it's in the app format
                if (HostingEnvironment.ApplicationPhysicalPath != null)
                {
                    return HostingEnvironment.MapPath(s);
                }
                else
                {
                    throw new ApplicationException("Please specify a cache folder that is not within the web application; it should never be accessible.");
                }

            }
            else
            {
                if (!Path.IsPathRooted(s))
                {
                    throw new ApplicationException("Please specify a cache folder that is not within the web application; it should never be accessible.");
                }
            }
            return Path.GetFullPath(s);

        }

        /// <summary>
        /// Helper class to run async methods within a sync process.
        /// </summary>
        internal static class AsyncUtil
        {
            private static readonly TaskFactory _taskFactory = new
                TaskFactory(CancellationToken.None,
                            TaskCreationOptions.None,
                            TaskContinuationOptions.None,
                            TaskScheduler.Default);

            /// <summary>
            /// Executes an async Task method which has a void return value synchronously
            /// USAGE: AsyncUtil.RunSync(() => AsyncMethod());
            /// </summary>
            /// <param name="task">Task method to execute</param>
            public static void RunSync(Func<Task> task)
                => _taskFactory
                    .StartNew(task)
                    .Unwrap()
                    .GetAwaiter()
                    .GetResult();

            /// <summary>
            /// Executes an async Task<T> method which has a T return type synchronously
            /// USAGE: T result = AsyncUtil.RunSync(() => AsyncMethod<T>());
            /// </summary>
            /// <typeparam name="TResult">Return Type</typeparam>
            /// <param name="task">Task<T> method to execute</param>
            /// <returns></returns>
            public static TResult RunSync<TResult>(Func<Task<TResult>> task)
                => _taskFactory
                    .StartNew(task)
                    .Unwrap()
                    .GetAwaiter()
                    .GetResult();
        }
        public IPlugin Install(Config c)
        {
            _c = c;
            LoadSettings(c);
            _cacheOptions.DiskCacheDirectory = ResolveCacheLocation(_cacheOptions.DiskCacheDirectory);



            //OK, we have our cacheOptions;
            this._cache = CreateHybridCacheFromOptions(_cacheOptions, _logger);
            c.Plugins.add_plugin(this);
            AsyncUtil.RunSync(() => this._cache.StartAsync(CancellationToken.None));
            return this;
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }
        public bool CanProcess(HttpContext current, IAsyncResponsePlan e)
        {
            //TODO: use normal access instead of casting
            return ((ResizeSettings)e.RewrittenQuerystring).Cache != ServerCacheMode.No;
        }
        public async Task ProcessAsync(HttpContext current, IAsyncResponsePlan plan)
        {
            //TODO: Get rid of ImageResizer's encoding namespace
            //TODO:  check etags, send not-modified as needed

            //TODO: stream directly from virtual file if the virtual file claims to be low-latency/overhead

            //TODO: Otherwise use GetOrCreateBytes

            // And respond using the stream
            await new NewModuleHelpers().ProcessWithStreamCache(_logger, this._cache, current, plan);
          
        }


        private Imazen.HybridCache.HybridCache CreateHybridCacheFromOptions(HybridCacheOptions options, ILogger logger)
        {
            var cacheOptions = new Imazen.HybridCache.HybridCacheOptions(options.DiskCacheDirectory)
            {
                AsyncCacheOptions = new AsyncCacheOptions()
                {
                    MaxQueuedBytes = Math.Max(0, options.QueueSizeLimitInBytes),
                    WriteSynchronouslyWhenQueueFull = true,
                    MoveFileOverwriteFunc = (from, to) =>
                    {
                        File.Copy(from, to, true);
                        File.Delete(from);
                    }
                },
                CleanupManagerOptions = new CleanupManagerOptions()
                {
                    MaxCacheBytes = Math.Max(0, options.CacheSizeLimitInBytes),
                    MinCleanupBytes = Math.Max(0, options.MinCleanupBytes),
                    MinAgeToDelete = options.MinAgeToDelete.Ticks > 0 ? options.MinAgeToDelete : TimeSpan.Zero
                }
            };
            var database = new MetaStore(new MetaStoreOptions(options.DiskCacheDirectory)
            {
                Shards = Math.Max(1, options.DatabaseShards),
                MaxLogFilesPerShard = 3
            }, cacheOptions, null);
            return new Imazen.HybridCache.HybridCache(database, cacheOptions, logger);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return _cache.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _cache.StopAsync(cancellationToken);
        }

        public Task<IStreamCacheResult> GetOrCreateBytes(byte[] key, AsyncBytesResult dataProviderCallback,
            CancellationToken cancellationToken,
            bool retrieveContentType)
        {
            return _cache.GetOrCreateBytes(key, dataProviderCallback, cancellationToken, retrieveContentType);
        }

        private string DiskCacheDirectory => _cacheOptions?.DiskCacheDirectory;


        private bool HasNtfsPermission()
        {
            try
            {
                if (!Directory.Exists(DiskCacheDirectory)) Directory.CreateDirectory(DiskCacheDirectory);
                string testFile = Path.Combine(DiskCacheDirectory, "TestFile.txt");
                File.WriteAllText(testFile,
                    "You may delete this file - it is written and deleted just to verify permissions are configured correctly");
                File.Delete(testFile);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string GetExecutingUser() {
            try {
                return Thread.CurrentPrincipal.Identity.Name;
            } catch {
                return "[Unknown - please check App Pool configuration]";
            }
        }

        private bool CacheDriveOnNetwork()
        {
            var physicalCache = DiskCacheDirectory;
            if (!string.IsNullOrEmpty(physicalCache))
            {
                return physicalCache.StartsWith("\\\\") || GetCacheDrive()?.DriveType == DriveType.Network;
            }
            return false;
        }

        private DriveInfo GetCacheDrive()
        {
            try
            {
                var drive = string.IsNullOrEmpty(DiskCacheDirectory) ? null : new DriveInfo(Path.GetPathRoot(DiskCacheDirectory));
                return (drive?.IsReady == true) ? drive : null;
            }
            catch { return null; }
        }

        public IEnumerable<IIssue> GetIssues() {
            var issues = new List<IIssue>();
            if (_cache != null) issues.AddRange(_cache.GetIssues());
            
            if (this._c.get("diskcache.dir", null) != null)
                issues.Add(new Issue("HybridCache", "Remove the <diskcache> element from web.config, as well as the plugin package", IssueSeverity.ConfigurationError));
            
            if (!HasNtfsPermission()) 
                issues.Add(new Issue("HybridCache", "Not working: Your NTFS Security permissions are preventing the application from writing to the disk cache",
    "Please give user " + GetExecutingUser() + " read and write access to directory \"" + DiskCacheDirectory + "\" to correct the problem. You can access NTFS security settings by right-clicking the aforementioned folder and choosing Properties, then Security.", IssueSeverity.ConfigurationError));
            
            //Warn user about setting hashModifiedDate=false in a web garden.
            if (_cacheOptions.MinCleanupBytes < 1000 * 1000)
                issues.Add(new Issue("HybridCache", "minCleanupBytes should not be set below 1 megabyte (1,000,000). Found in the <hybridCache /> element in Web.config.",
                    "Setting a value too low will waste energy and reduce performance", IssueSeverity.ConfigurationError));

            if (_cacheOptions.CacheSizeLimitInBytes < 1000 * 1000 * 100)
                issues.Add(new Issue("HybridCache", "cacheMaxSizeBytes should not be set below 100 MiB, 1GiB is the suggested minimum . Found in the <hybridCache /> element in Web.config.",
                    "Setting a value too low will increase latency, increase cache misses, waste energy and reduce server performance.", IssueSeverity.ConfigurationError));

            if (conflictsExist)
                issues.Add(new Issue("HybridCache", "More than one instance of HybridCache has been created for the same directory, these instances will fight.", IssueSeverity.ConfigurationError));


            if (CacheDriveOnNetwork())
                issues.Add(new Issue("HybridCache", "It appears that the cache directory is located on a network drive.",
                    "Network drives often have unacceptable latency for response caching; please test yours.", IssueSeverity.Warning));
                    
            return issues;
        }

        public IEnumerable<KeyValuePair<string, string>> GetInfoPairs()
        {
            var list = new List<KeyValuePair<string, string>>();
            list.Add(new KeyValuePair<string, string>("hybridCache_" + "cacheMaxSizeBytes", _cacheOptions.CacheSizeLimitInBytes.ToString()));
            list.Add(new KeyValuePair<string, string>("hybridCache_" + "writeQueueLimitBytes", _cacheOptions.QueueSizeLimitInBytes.ToString()));
            list.Add(new KeyValuePair<string, string>("hybridCache_" + "minCleanupBytes", _cacheOptions.MinCleanupBytes.ToString()));
            list.Add(new KeyValuePair<string, string>("hybridCache_" + "shardCount", _cacheOptions.DatabaseShards.ToString()));
            list.Add(new KeyValuePair<string, string>("hybridCache_network_drive", CacheDriveOnNetwork() ? "1" : "0"));
            list.Add(new KeyValuePair<string, string>("hybridCache_filesystem", GetCacheDrive()?.DriveFormat ?? ""));
            list.Add(new KeyValuePair<string, string>("hybridCache_drive_avail", GetCacheDrive()?.AvailableFreeSpace.ToString() ?? ""));
            list.Add(new KeyValuePair<string, string>("hybridCache_drive_total", GetCacheDrive()?.TotalSize.ToString() ?? ""));
            return list;
        }


    }
}