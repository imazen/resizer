using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Web.Caching;
using System.Diagnostics;
using ImageResizer.Configuration;
using System.Collections.Specialized;
using ImageResizer.Util;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.PsdComposer
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
        public object GetSubkey(string subkey)
        {
            lock (subkey_syncobj)
            {
                if (!subkeys.ContainsKey(subkey)) return null;
                return subkeys[subkey];
            }
        }
        public void SetSubkey(string subkey, object item)
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
        MemCachedFile( IVirtualFile file ) {
            if (file == null) throw new FileNotFoundException();
            this.path = file.VirtualPath;

            using (Stream s = file.Open()) {
                this.data = s.CopyToBytes();
            }
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
            using (System.IO.FileStream fs = new FileStream(physicalPath, FileMode.Open, FileAccess.Read))
            {
                this.data = fs.CopyToBytes();
            }

            sw.Stop();
            if (HttpContext.Current != null)
                HttpContext.Current.Trace.Write("MemCachedFile loaded file into memory in " + sw.ElapsedMilliseconds + "ms");
        }
        private string path = null;
        private byte[] data = null;
        /// <summary>
        /// Used only when there is not http session.
        /// </summary>
        private static Dictionary<string, MemCachedFile> _fallbackCache = new Dictionary<string, MemCachedFile>();

        
        public static MemCachedFile GetCachedVirtualFile(string path, IVirtualImageProvider provider, NameValueCollection queryString){
            string key = provider != null ? getVirtualCacheKey(path,queryString) : getCacheKey(path);
            MemCachedFile file = null;
            if (HttpContext.Current != null)
            {
                if (HttpContext.Current.Cache[key] != null)
                {
                    file = (MemCachedFile)HttpContext.Current.Cache[key];
                }
                else
                {
                    IVirtualFile vfile = provider != null ? provider.GetFile(path, queryString) : null;
                    if (vfile == null) throw new FileNotFoundException("The specified virtual file could not be found: \"" + path + "\" Associated querystring: \"" + PathUtils.BuildQueryString(queryString) + "\".");
                    file = vfile != null ? new MemCachedFile(vfile) : new MemCachedFile(path);
                    if (provider == null)
                        HttpContext.Current.Cache.Insert(key, file, new CacheDependency(path));
                    else
                        HttpContext.Current.Cache.Insert(key, file);
                }
            }
            else
            {
                //Has no invalidation, but this is only used for benchmarks. Only runs when there is no http session.
                lock(_fallbackCache){
                    if (_fallbackCache.ContainsKey(key)) file = _fallbackCache[key];
                    else
                    {
                        file = provider != null ? new MemCachedFile(provider.GetFile(path, queryString)) : new MemCachedFile(path);
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
        private static string getVirtualCacheKey(string virtualPath, NameValueCollection query) {
            return "MemCachedVirtualFile:" + virtualPath.ToLowerInvariant() + PathUtils.BuildQueryString(query);
        }
    }
}