﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ImageResizer.Collections;
using ImageResizer.Util;
using System.Collections.Specialized;
using ImageResizer.ExtensionMethods;
using ImageResizer.Plugins.DiskCache;

namespace ImageResizer.Plugins.SourceMemCache {
    public class SourceMemCachePlugin: IPlugin, IVirtualFileCache {

        /// <summary>
        /// Defaults to 10MB limit, and samples usage over the last 10 minutes when deciding what to remove. Stuff not used in the last 10 minutes gets discarded even if the limit hasn't been reached.
        /// </summary>
        public SourceMemCachePlugin() : this(1024 * 1024 * 1024, new TimeSpan(0, 10, 0)) { }

        public SourceMemCachePlugin(long maxBytes, TimeSpan usageWindow) {
            //Cleanup at most once per minute, unless hitting the limits. 
            cache = new ConstrainedCache<string, CachedVirtualFile>(StringComparer.OrdinalIgnoreCase, delegate(string key, CachedVirtualFile file) {
                return key.Length * 4 + file.BytesOccupied;
            }, maxBytes, usageWindow, new TimeSpan(0, 1, 0)); 

        }

        private LockProvider locks = new LockProvider();

        private ConstrainedCache<string, CachedVirtualFile> cache;

        public IVirtualFile GetFileIfCached(string virtualPath, System.Collections.Specialized.NameValueCollection queryString, IVirtualFile original) {
            //Use alternate cache key if provided
            string key = original is IVirtualFileSourceCacheKey ? ((IVirtualFileSourceCacheKey)original).GetCacheKey(true) : original.VirtualPath;
            //If cached, serve it. 
            CachedVirtualFile c = cache.Get(key);
            if (c != null) return c;
            //If not, let's cache it.
            if ("mem".Equals(queryString["scache"], StringComparison.OrdinalIgnoreCase)) {
                locks.TryExecute(key, 3000, delegate() {
                    c = cache.Get(key);
                    if (c == null) {
                        using (Stream data = original.Open()) {//Very long-running call
                            c = new CachedVirtualFile(original.VirtualPath, StreamExtensions.CopyToBytes(data, true));
                        }
                        cache.Set(key, c);//Save to cache (may trigger cleanup)
                    }
                });
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

        private bool checkIntegrity = true;

        public System.IO.Stream Open() {
            if (checkIntegrity) {
                if (originalHash == -1) originalHash = CaluclateHash();
                else if (originalHash != CaluclateHash()) throw new AccessViolationException("A read-only memory stream was somehow modified.");
            }

            return new MemoryStream(data, false);
        }

        protected int originalHash = -1;
        protected int CaluclateHash() {
            unchecked {
                const int p = 16777619;
                int hash = (int)2166136261;

                for (int i = 0; i < data.Length; i++)
                    hash = (hash ^ data[i]) * p;

                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;
                return hash;
            }
        }

        public long BytesOccupied { get { return data.Length + virtualPath.Length * 4 + 32; } }


    }
}
