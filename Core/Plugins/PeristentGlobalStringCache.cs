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


        internal WriteThroughCache() : this(null) { }

        internal WriteThroughCache(string keyPrefix)
        {
            this.prefix = prefix ?? "resizer_key_";
            sink = new IssueSink(sinkSource);
        }

        // Potentially good folders to cache keys in
        string[] potentialLocations;
        // Folders that writes or reads have failed. 
        ConcurrentBag<string> badLocations;
        IIssueReceiver sink;
        ReaderWriterLockSlim filesystem = new ReaderWriterLockSlim();

        ConcurrentDictionary<string, string> cache = new ConcurrentDictionary<string, string>();


        void AddBadLocation(string path, IIssue i)
        {
            if (badLocations == null) badLocations = new ConcurrentBag<string>();
            badLocations.Add(path);
            sink.AcceptIssue(i);
        }
        string[] Locations()
        {
            if (potentialLocations == null)
            {

                var appPath = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath;

                potentialLocations = ((appPath != null) ? new string[] { Path.Combine(appPath, "imagecache"), Path.Combine(appPath, "App_Data"), Path.GetTempPath() } : new string[] { Path.GetTempPath() })
                        .Where(p =>
                        {
                            try { return Directory.Exists(p); } catch { return false; }
                        }).ToArray();

            }
            if (badLocations == null)
            {
                return potentialLocations;
            }
            else
            {
                return potentialLocations.Except(badLocations).ToArray();
            }
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
            if (key.Any(c => !Char.IsLetterOrDigit(c)))
            {
                return this.prefix + hashToBase16(key) + ".txt";
            }
            else
            {
                return this.prefix + key + ".txt";
            }
        }

        bool TryDelete(string key)
        {
            bool failedAtSomething = false;
            try
            {
                filesystem.EnterWriteLock();
                foreach (var dest in Locations())
                {
                    var path = Path.Combine(dest, FilenameKeyFor(key));
                    try
                    {
                        if (File.Exists(path)) File.Delete(path);
                    }
                    catch (Exception e)
                    {
                        AddBadLocation(dest, new Issue("Failed to delete " + dataKind + " at location " + path, e.ToString(), IssueSeverity.Warning));
                        failedAtSomething = true;
                    }
                }
                return !failedAtSomething;
            }
            finally
            {
                filesystem.ExitWriteLock();
            }
        }

        bool TryDiskWrite(string key, string value)
        {
            if (value == null)
            {
                return TryDelete(key);
            }
            else
            {
                try
                {
                    filesystem.EnterWriteLock();

                    foreach (var dest in Locations())
                    {
                        var path = Path.Combine(dest, FilenameKeyFor(key));
                        try
                        {

                            File.WriteAllText(path, value, UTF8Encoding.UTF8);
                            return true;
                        }
                        catch (Exception e)
                        {
                            AddBadLocation(dest, new Issue("Failed to write " + dataKind + " to location " + path, e.ToString(), IssueSeverity.Warning));
                        }
                    }

                }
                finally
                {
                    filesystem.ExitWriteLock();
                }
                sink.AcceptIssue(new Issue("Unable to cache " + dataKind + " to disk in any location.", IssueSeverity.Error));
                return false;
            }
        }

        string TryDiskRead(string key)
        {
            bool readFailed = false; //To tell non-existent files apart from I/O errors
            try
            {
                filesystem.EnterReadLock();
                foreach (var dest in Locations())
                {
                    var path = Path.Combine(dest, FilenameKeyFor(key));
                    if (File.Exists(path))
                    {
                        try
                        {
                            return File.ReadAllText(path, UTF8Encoding.UTF8);
                        }
                        catch (Exception e)
                        {
                            readFailed = true;
                            AddBadLocation(dest, new Issue("Failed to read " + dataKind + " from location " + path, e.ToString(), IssueSeverity.Warning));
                        }
                    }
                }
            }
            finally
            {
                filesystem.ExitReadLock();
            }
            if (readFailed)
            {
                sink.AcceptIssue(new Issue("Unable to read " + dataKind + " from disk despite its existence.", IssueSeverity.Error));
            }
            return null;
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
            return TryDiskWrite(key, value) ? StringCachePutResult.WriteComplete : StringCachePutResult.WriteFailed;
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
                var disk = TryDiskRead(key);
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
