using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using ImageResizer.Configuration;
using Imazen.Common.Extensibility.StreamCache;
using Imazen.Common.Issues;
using Imazen.HybridCache;
using Imazen.HybridCache.MetaStore;
using Microsoft.Extensions.Logging;

namespace ImageResizer.Plugins.HybridCache
{
    public class HybridCachePlugin : IAsyncTyrantCache, IPlugin
    {
        public bool CanProcess(HttpContext current, IAsyncResponsePlan e)
        {
            throw new NotImplementedException();
        }

        public IPlugin Install(Config c)
        {
            throw new NotImplementedException();
        }

        public Task ProcessAsync(HttpContext current, IAsyncResponsePlan e)
        {
            throw new NotImplementedException();
        }

        public bool Uninstall(Config c)
        {
            throw new NotImplementedException();
        }


        private readonly Imazen.HybridCache.HybridCache cache;

        public HybridCachePlugin(HybridCacheOptions options, ILogger logger)
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
            cache = new Imazen.HybridCache.HybridCache(database, cacheOptions, logger);
        }

        public IEnumerable<IIssue> GetIssues()
        {
            return cache.GetIssues();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return cache.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return cache.StopAsync(cancellationToken);
        }

        public Task<IStreamCacheResult> GetOrCreateBytes(byte[] key, AsyncBytesResult dataProviderCallback,
            CancellationToken cancellationToken,
            bool retrieveContentType)
        {
            return cache.GetOrCreateBytes(key, dataProviderCallback, cancellationToken, retrieveContentType);
        }
    }
}