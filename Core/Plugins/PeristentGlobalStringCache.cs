using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageResizer.Configuration;
using System.IO;
using System.Collections.Concurrent;
using ImageResizer.Configuration.Issues;
using System.Security.Cryptography;
using System.Globalization;
using System.Threading;

namespace ImageResizer.Plugins
{
    /// <summary>
    /// Provides a disk-persisted (hopefully, if it can successfully write/read) cache for a tiny number of keys. (one file per key/value). 
    /// Provides no consistency or guarantees whatsoever. You hope something gets written to disk, and that it can be read after app reboot. In the meantime you have a ConcurrentDictionary that doesn't sync to disk. 
    /// Errors reported via IIssueProvider, not exceptions.
    /// Designed for license files
    /// </summary>
    class WriteThroughCache : IIssueProvider
    {

        string prefix = "resizer_key_";
        string sinkSource = "LicenseCache";
        string dataKind = "license";

        
        IIssueReceiver sink;
        MultiFolderStorage store;
        ConcurrentDictionary<string, string> cache = new ConcurrentDictionary<string, string>();

        internal WriteThroughCache() : this(null) { }

        internal WriteThroughCache(string keyPrefix)
        {
            this.prefix = keyPrefix ?? prefix;


            sink = new IssueSink(sinkSource);

            var appPath = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;
            var candidates = ((appPath != null) ? 
                new string[] { Path.Combine(appPath, "imagecache"),
                    Path.Combine(appPath, "App_Data"), Path.GetTempPath() } 
                : new string[] { Path.GetTempPath() }).ToArray();

            store = new MultiFolderStorage(sinkSource, dataKind, sink, candidates, FolderOptions.Default);
        }
        
        string hashToBase16(string data)
        {
            byte[] bytes = SHA256.Create().ComputeHash(new UTF8Encoding().GetBytes(data));
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                sb.Append(b.ToString("x", NumberFormatInfo.InvariantInfo).PadLeft(2, '0'));
            return sb.ToString();
        }


        string FilenameKeyFor(string key)
        {
            if (key.Any(c => !Char.IsLetterOrDigit(c) && c != '_') || key.Length + prefix.Length > 200)
            {
                return this.prefix + hashToBase16(key) + ".txt";
            }
            else
            {
                return this.prefix + key + ".txt";
            }
        }

        
        /// <summary>
        /// Write-through mem cache
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal StringCachePutResult TryPut(string key, string value)
        {
            string current;
            if (cache.TryGetValue(key, out current) && current == value)
            {
                return StringCachePutResult.Duplicate;
            }
            cache[key] = value;
            return store.TryDiskWrite(FilenameKeyFor(key), value) ? StringCachePutResult.WriteComplete : StringCachePutResult.WriteFailed;
        }

        /// <summary>
        /// Read-through mem cache
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal string TryGet(string key)
        {
            string current;
            if (cache.TryGetValue(key, out current))
            {
                return current;
            }else
            {
                var disk = store.TryDiskRead(FilenameKeyFor(key));
                if (disk != null)
                {
                    cache[key] = disk;
                }
                return disk;
            }
        }

        public IEnumerable<IIssue> GetIssues()
        {
            return ((IIssueProvider)sink).GetIssues();
        }
    }

    /// <summary>
    /// Not for you. Don't use this. It creates a separate file for every key. Wraps a singleton
    /// </summary>
    public class PeristentGlobalStringCache : IPersistentStringCache, IIssueProvider
    {
        static WriteThroughCache processCache = new WriteThroughCache();


        WriteThroughCache cache;
        public PeristentGlobalStringCache()
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
    }
}
