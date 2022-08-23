using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Hosting;
using Imazen.Common.Issues;

namespace ImageResizer.Plugins
{
    /// <summary>
    ///     Provides a disk-persisted (hopefully, if it can successfully write/read) cache for a tiny number of keys. (one file
    ///     per key/value).
    ///     Provides no consistency or guarantees whatsoever. You hope something gets written to disk, and that it can be read
    ///     after app reboot. In the meantime you have a ConcurrentDictionary that doesn't sync to disk.
    ///     Errors reported via IIssueProvider, not exceptions.
    ///     Designed for license files
    /// </summary>
    internal class WriteThroughCache : IIssueProvider
    {
        private string prefix = "resizer_key_";
        private string sinkSource = "LicenseCache";
        private string dataKind = "license";


        private IIssueReceiver sink;
        private MultiFolderStorage store;
        private ConcurrentDictionary<string, string> cache = new ConcurrentDictionary<string, string>();

        internal WriteThroughCache() : this(null)
        {
        }

        internal WriteThroughCache(string keyPrefix)
        {
            prefix = keyPrefix ?? prefix;


            sink = new IssueSink(sinkSource);

            var appPath = HostingEnvironment.ApplicationPhysicalPath;
            var candidates = (appPath != null
                ? new[]
                {
                    Path.Combine(appPath, "imagecache"),
                    Path.Combine(appPath, "App_Data"), Path.GetTempPath()
                }
                : new[] { Path.GetTempPath() }).ToArray();

            store = new MultiFolderStorage(sinkSource, dataKind, sink, candidates, FolderOptions.Default);
        }

        private string hashToBase16(string data)
        {
            var bytes = SHA256.Create().ComputeHash(new UTF8Encoding().GetBytes(data));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x", NumberFormatInfo.InvariantInfo).PadLeft(2, '0'));
            return sb.ToString();
        }


        private string FilenameKeyFor(string key)
        {
            if (key.Any(c => !char.IsLetterOrDigit(c) && c != '_') || key.Length + prefix.Length > 200)
                return prefix + hashToBase16(key) + ".txt";
            else
                return prefix + key + ".txt";
        }


        /// <summary>
        ///     Write-through mem cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal StringCachePutResult TryPut(string key, string value)
        {
            string current;
            if (cache.TryGetValue(key, out current) && current == value) return StringCachePutResult.Duplicate;
            cache[key] = value;
            return store.TryDiskWrite(FilenameKeyFor(key), value)
                ? StringCachePutResult.WriteComplete
                : StringCachePutResult.WriteFailed;
        }

        /// <summary>
        ///     Read-through mem cache
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal string TryGet(string key)
        {
            string current;
            if (cache.TryGetValue(key, out current))
            {
                return current;
            }
            else
            {
                var disk = store.TryDiskRead(FilenameKeyFor(key));
                if (disk != null) cache[key] = disk;
                return disk;
            }
        }


        internal DateTime? GetWriteTimeUtc(string key)
        {
            return store.TryGetLastWriteTimeUtc(FilenameKeyFor(key));
        }

        public IEnumerable<IIssue> GetIssues()
        {
            return ((IIssueProvider)sink).GetIssues();
        }
    }

    /// <summary>
    ///     Not for you. Don't use this. It creates a separate file for every key. Wraps a singleton
    /// </summary>
    public class PersistentGlobalStringCache : IPersistentStringCache, IIssueProvider
    {
        private static WriteThroughCache processCache = new WriteThroughCache();


        private WriteThroughCache cache;

        public PersistentGlobalStringCache()
        {
            cache = processCache;
        }

        public string Get(string key)
        {
            return cache.TryGet(key);
        }

        public StringCachePutResult TryPut(string key, string value)
        {
            return cache.TryPut(key, value);
        }

        public IEnumerable<IIssue> GetIssues()
        {
            return cache.GetIssues();
        }

        public DateTime? GetWriteTimeUtc(string key)
        {
            return cache.GetWriteTimeUtc(key);
        }
    }
}