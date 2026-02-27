using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;

namespace ImageResizer.Plugins.Faces
{
    /// <summary>
    /// Locates cascade files on disk or extracts them from embedded resources.
    /// </summary>
    public class FileLocator
    {
        readonly List<string> searchFolders = new List<string>() { };
        int secondsToCache;

        /// <summary>
        /// Creates new instance of FileLocator.
        /// </summary>
        public FileLocator(int secondsToCache)
        {
            this.secondsToCache  = secondsToCache;
            var a = this.GetType().Assembly;
            //Use CodeBase if it is physical; this means we don't re-download each time we recycle.
            //If it's a URL, we fall back to Location, which is often the shadow-copied version.
            var searchFolder = a.CodeBase.StartsWith("file:///", StringComparison.OrdinalIgnoreCase)
                                ? a.CodeBase
                                : a.Location;
            //Convert UNC paths
            searchFolder = Path.GetDirectoryName(searchFolder.Replace("file:///", "").Replace("/", "\\"));

            searchFolders.Add(searchFolder);
        }


        readonly ConcurrentDictionary<string, string> cascadePaths = new ConcurrentDictionary<string, string>() { };
        // Reset CascadePaths every second
        long pathsLastCleared = 0;

        long pathsAdded = 0;

        /// <summary>
        /// Directory where extracted cascade resources are cached.
        /// </summary>
        static readonly string ResourceCacheDir = Path.Combine(Path.GetTempPath(), "ImageResizer_cascades");

        /// <summary>
        /// Assemblies to search for embedded cascade resources.
        /// Populated by CascadePool or calling code.
        /// </summary>
        public static readonly ConcurrentBag<Assembly> ResourceAssemblies = new ConcurrentBag<Assembly>();

        internal string LocateFileCached(string fileNameWithoutPath)
        {
            // It's a rough cache with 3-sec expiry, no need to be thread safe. (caches nulls, too)
            if (pathsAdded > 0 && pathsLastCleared < Stopwatch.GetTimestamp() - (Stopwatch.Frequency * secondsToCache))
            {
                cascadePaths.Clear();
                pathsLastCleared = Stopwatch.GetTimestamp();
            }

            var result = cascadePaths.GetOrAdd(fileNameWithoutPath, name =>
            {
                // First, try to find the file on disk in search folders
                var diskPath = (from basePath in searchFolders
                                select Path.Combine(basePath.TrimEnd('\\', '/'), name)
                                into full
                                where File.Exists(Path.GetFullPath(full))
                                select Path.GetFullPath(full))
                               .FirstOrDefault();
                if (diskPath != null) return diskPath;

                // Fall back to extracting from embedded resources
                return ExtractFromResources(name);
            });
            pathsAdded++;
            return result;
        }

        /// <summary>
        /// Extracts a cascade file from assembly embedded resources (gzip-compressed).
        /// Returns the extracted file path, or null if not found.
        /// </summary>
        static string ExtractFromResources(string fileNameWithoutPath)
        {
            var gzResourceSuffix = ".cascades." + fileNameWithoutPath + ".gz";

            foreach (var assembly in ResourceAssemblies)
            {
                var resourceName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith(gzResourceSuffix, StringComparison.OrdinalIgnoreCase));
                if (resourceName == null) continue;

                Directory.CreateDirectory(ResourceCacheDir);
                var targetPath = Path.Combine(ResourceCacheDir, fileNameWithoutPath);

                // Already extracted (from a previous call or app run)
                if (File.Exists(targetPath)) return targetPath;

                using (var resStream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (resStream == null) continue;
                    using (var gzStream = new GZipStream(resStream, CompressionMode.Decompress))
                    using (var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        gzStream.CopyTo(fileStream);
                    }
                }
                return targetPath;
            }
            return null;
        }
    }
    class Pool<T> where T:IDisposable
    {
        protected Func<string, T> Generator { get; set; }

        protected SemaphoreSlim Semaphore;

        readonly ConcurrentDictionary<string, ConcurrentBag<T>> pool =
            new ConcurrentDictionary<string, ConcurrentBag<T>>();

        static TR RunAndReturnToBag<TR>(ConcurrentBag<T> bag, T c, Func<T, TR> operation)
        {
            var inBag = false;
            try
            {
                var r = operation(c);
                bag.Add(c);
                inBag = true;
                return r;
            }
            finally
            {
                if (!inBag) c.Dispose();
            }
        }

        public TR Borrow<TR>(string key, Func<T,TR> operation, int timeoutMs)
        {
            if (Semaphore == null) {
                return BorrowItemInternal(key, operation);
            } else {
                if (Semaphore.Wait(timeoutMs)) {
                    try {
                        return BorrowItemInternal(key, operation);
                    } finally {
                        Semaphore.Release();
                    }
                } else {
                    throw new ImageProcessingException(
                        $"Timeout of {timeoutMs}ms exceeded to borrow item {key} from resource pool");
                }
            }
        }

        TR BorrowItemInternal<TR>(string key, Func<T, TR> operation)
        {
            var bag = pool.GetOrAdd(key, name => new ConcurrentBag<T>());
            T c;
            if (bag.TryTake(out c))
            {
                return RunAndReturnToBag(bag, c, operation);
            }
            else
            {
                return RunAndReturnToBag(bag, Generator(key), operation);
            }
        }

        /// <summary>
        /// Disposes all loaded cascades
        /// </summary>
        public void Dispose()
        {
            foreach (var bag in pool.Values)
            {
                foreach (var c in bag.TakeWhile(c => true))
                {
                    c.Dispose();
                }
            }
        }

        protected int SuggestedConcurrency => Environment.ProcessorCount * 2 + 1;
    }


    class CascadePool: Pool<CascadeClassifier>
    {
        FileLocator locator = new FileLocator(3);

        public CascadePool()
        {
            // Register known assemblies that contain cascade resources
            FileLocator.ResourceAssemblies.Add(typeof(CascadePool).Assembly);

            Generator = key =>
            {
                var path = locator.LocateFileCached(key);
                if (path == null) {
                    throw new ImageProcessingException(
                        "Failed to find " + key +
                        " in any of the search directories or embedded resources. Verify the cascade XML files are available.");
                }
                return new CascadeClassifier(path);
            };
            Semaphore = new SemaphoreSlim(SuggestedConcurrency, SuggestedConcurrency);
        }

        public static CascadePool Shared { get; } = new CascadePool();

    }
}
