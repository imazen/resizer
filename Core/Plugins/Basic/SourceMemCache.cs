using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ImageResizer.Collections;
using ImageResizer.Util;

namespace ImageResizer.Plugins.Basic {
    public class SourceMemCache: IPlugin, IVirtualFileCache {

        public SourceMemCache(long maxBytes, TimeSpan usageWindow) {
            //Cleanup at most once per minute, unless hitting the limits. 
            cache = new ConstrainedCache<string, CachedVirtualFile>(StringComparer.OrdinalIgnoreCase, delegate(string key, CachedVirtualFile file) {
                return key.Length * 4 + file.BytesOccupied;
            }, maxBytes, usageWindow, new TimeSpan(0, 1, 0)); 

        }

        private ConstrainedCache<string, CachedVirtualFile> cache;

        public IVirtualFile GetFileIfCached(string virtualPath, System.Collections.Specialized.NameValueCollection queryString, IVirtualFile original) {
            //Use alternate cache key if provided
            string key = original is IVirtualFileSourceCacheKey ? ((IVirtualFileSourceCacheKey)original).GetCacheKey(true) : original.VirtualPath;
            //If cached, serve it. 
            CachedVirtualFile c = cache.Get(key);
            if (c != null) return c;
            //If not, let's cache it.
            if ("true".Equals(queryString["memcache"], StringComparison.OrdinalIgnoreCase)) {
                //Optimization idea - use LockProvider to prevent duplicate requests. Would mean merging with DiskCache :(
                c = new CachedVirtualFile(original.VirtualPath, StreamUtils.CopyToBytes(original.Open())); //Very long-running call
                cache.Set(key, c); //Save to cache (may trigger cleanup)
                return c;
            }
            return null;
        }

        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }


    }

    public class CachedVirtualFile : IVirtualFile {

        public CachedVirtualFile(string virtualPath, byte[] data) {
            this.virtualPath = virtualPath;
            this.data = data;
        }
        string virtualPath;
        byte[] data;
        public string VirtualPath {
            get { return virtualPath; }
        }

        public System.IO.Stream Open() {
            return new MemoryStream(data, false);
        }

        public long BytesOccupied { get { return data.Length + virtualPath.Length * 4 + 32; } }
    }
}
