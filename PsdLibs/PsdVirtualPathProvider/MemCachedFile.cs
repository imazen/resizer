using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Web.Caching;
using System.Diagnostics;

namespace PsdRenderer
{
    /// <summary>
    /// Caches a file in memory using the asp.net Cache object, while exposing methods for adding subkeys that invalidate along with the file if the source file is changed.
    /// </summary>
    public class MemCachedFile
    {
        /// <summary>
        /// For saving data parsed from the file
        /// </summary>
        private Dictionary<string, Object> subkeys = new Dictionary<string, object>();

        private object subkey_syncobj = new object();
        /// <summary>
        /// Remember, the object returned here may be accessed by multiple threads at the same time! No modifications!
        /// </summary>
        /// <param name="subkey"></param>
        /// <returns></returns>
        public object getSubkey(string subkey)
        {
            lock (subkey_syncobj)
            {
                if (!subkeys.ContainsKey(subkey)) return null;
                return subkeys[subkey];
            }
        }
        public void setSubkey(string subkey, object item)
        {
            lock (subkey_syncobj)
            {
                subkeys[subkey] = item;
            }
        }
       

        /// <summary>
        /// Wraps the byte array in a read-only stream
        /// </summary>
        /// <returns></returns>
        public Stream GetStream()
        {
            return new MemoryStream(data,false);
        }
        /// <summary>
        /// Reads the file into memory 
        /// </summary>
        /// <param name="physicalPath"></param>
        MemCachedFile(string physicalPath)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            this.path = physicalPath;
            using (System.IO.FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                // Read the source file into a byte array.
                byte[] bytes = new byte[fs.Length];
                int numBytesToRead = (int)fs.Length;
                int numBytesRead = 0;
                while (numBytesToRead > 0)
                {
                    // Read may return anything from 0 to numBytesToRead.
                    int n = fs.Read(bytes, numBytesRead, numBytesToRead);

                    // Break when the end of the file is reached.
                    if (n == 0)
                        break;

                    numBytesRead += n;
                    numBytesToRead -= n;
                }
                Debug.Assert(numBytesRead == bytes.Length);
                this.data = bytes;
            }

            sw.Stop();
            if (HttpContext.Current != null)
                HttpContext.Current.Trace.Write("MemCachedFile loaded file into memory in " + sw.ElapsedMilliseconds + "ms");
        }
        private string path = null;
        private byte[] data = null;
        private static Dictionary<string, MemCachedFile> _fallbackCache = new Dictionary<string, MemCachedFile>();

        public static MemCachedFile GetCachedFile(string physicalPath){
            string key = getCacheKey(physicalPath);
            MemCachedFile file = null;
            if (HttpContext.Current != null)
            {
                if (HttpContext.Current.Cache[key] != null)
                {
                    file = (MemCachedFile)HttpContext.Current.Cache[key];
                }
                else
                {
                    file = new MemCachedFile(physicalPath);
                    HttpContext.Current.Cache.Insert(key, file, new CacheDependency(physicalPath));
                }
            }
            else
            {
                //Has no invalidation, but this is only used for benchmarks. Only runs when there is no http session.
                lock(_fallbackCache){
                    if (_fallbackCache.ContainsKey(key)) file = _fallbackCache[key];
                    else
                    {
                        file = new MemCachedFile(physicalPath);
                        _fallbackCache[key] = file;
                    }
                }
            }
            return file;
        }
        private static string getCacheKey(string physicalPath)
        {
            return "MemCachedFile:" + physicalPath.ToLowerInvariant();
        }
    }
}