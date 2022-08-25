using System;
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
using Imazen.Common.Issues;
using Imazen.HybridCache;
using Imazen.HybridCache.MetaStore;
using Microsoft.Extensions.Logging;

namespace ImageResizer.Plugins.HybridCache
{
    public class HybridCachePlugin : IAsyncTyrantCache, IPlugin
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


        private string GetLegacyDiskCachePhysicalPath(Config config)
        {
            var defaultDir = (HostingEnvironment.ApplicationVirtualPath?.TrimEnd('/') ?? "") + "/imagecache";
            var dir = config.get("diskcache.dir", defaultDir);
            dir = string.IsNullOrEmpty(dir) ? null : PathUtils.ResolveAppRelativeAssumeAppRelative(dir);
            return !string.IsNullOrEmpty(dir) ? HostingEnvironment.MapPath(dir) : null;
        }
        
        
        private void LoadSettings(Config c)
        {
            _cacheOptions.DiskCacheDirectory = c.get("hybridCache.cacheLocation", _cacheOptions.DiskCacheDirectory);
            _cacheOptions.CacheSizeLimitInBytes = c.get("hybridCache.cacheMaxSizeBytes", _cacheOptions.CacheSizeLimitInBytes);
            _cacheOptions.DatabaseShards = c.get("hybridCache.shardCount", _cacheOptions.DatabaseShards);
            _cacheOptions.QueueSizeLimitInBytes = c.get("hybridCache.writeQueueLimitBytes", _cacheOptions.QueueSizeLimitInBytes);
            _cacheOptions.MinCleanupBytes = c.get("hybridCache.minCleanupBytes", _cacheOptions.MinCleanupBytes);

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

        public IEnumerable<IIssue> GetIssues()
        {
            return _cache.GetIssues();
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
    }
}