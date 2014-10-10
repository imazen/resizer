using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImageResizer.Caching;
using System.Security.Permissions;
using System.Security;
using System.Globalization;
using System.IO;
using ImageResizer.ExtensionMethods;
using System.Diagnostics;
using ImageResizer.Plugins.Basic;
using System.Web.Hosting;
using ImageResizer.Util;
using ProtoBuf;

namespace ImageResizer.Plugins.TinyCache {
    public class TinyCachePlugin:ICache, IPlugin {

        public TinyCachePlugin() {
        }


        protected string virtualCacheFile = (HostingEnvironment.ApplicationVirtualPath ?? string.Empty).TrimEnd('/') + "/App_Data/tiny_cache.cache";
        /// <summary>
        /// Sets the location of the cache file
        /// </summary>
        public string VirtualCacheFile {
            get {
                return virtualCacheFile;
            }
            set {
                //Default to application-relative path if no leading slash is present. 
                //Resolve the tilde if present.
                virtualCacheFile = string.IsNullOrEmpty(value) ? null : PathUtils.ResolveAppRelativeAssumeAppRelative(value);
            }
        }
        /// <summary>
        /// Returns the physical path of the cache directory specified in VirtualCacheFile.
        /// </summary>
        public string PhysicalCacheFile {
            get {
                if (!string.IsNullOrEmpty(VirtualCacheFile)) {
                    if (HostingEnvironment.ApplicationVirtualPath == null) {
                        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, this.VirtualCacheFile.TrimStart('/'));
                    }
                    else {
                        return HostingEnvironment.MapPath(VirtualCacheFile);
                    }
                }
                    

                return null;
            }
        }

        /// <summary>
        /// 30MB is the maxmimum size of the cache. Writing more than that do disk during an image request would cause a timeout for sure.
        /// </summary>
        public int MaxBytes { get { return 30 * 1024 * 1024; } }

        /// <summary>
        /// (De)Serializing more than 1024 items to disk during a request is risky. Sorting more than that can be slow as well.
        /// </summary>
        public int MaxItems { get { return 1024; } }

        /// <summary>
        /// We try to perform cleanup every 50 changes. We also flush to disk afterwards, but not more than once per 30s.
        /// </summary>
        private int ChangeThreshold { get { return 50; } }


        
        public bool CanOperate {
            get {
                return !string.IsNullOrEmpty(VirtualCacheFile) && HasFileIOPermission;
            }
        }

        /// <summary>
        /// Flushing to disk more than once every 30 seconds is bad.
        /// </summary>
        private int MillisecondsBetweenFlushes {get { return 30 * 1000; }}

        private bool? _hasFileioPermission;
        protected bool HasFileIOPermission {
            get {
                if (_hasFileioPermission == null) {
                    return PathUtils.HasIOPermission(new string[] { PhysicalCacheFile });
                }
                return _hasFileioPermission.Value;
            }
        }

        private DateTime lastFlush = DateTime.MinValue;

        private CacheFile cache = new CacheFile();

        [ProtoContract]
        private class CacheFile {

            [ProtoBuf.ProtoMember(1)]
            public int changes_since_cleanse = 0;

            [ProtoBuf.ProtoMember(2)]
            public Dictionary<string, CacheEntry> hash = new Dictionary<string,CacheEntry>();
        }

        private void EnsureDirExists() {
            string dir = Path.GetDirectoryName(PhysicalCacheFile);
            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
        }

        private void Flush(){
            lock (cache) {
                EnsureDirExists();
                using (var fs = new FileStream(PhysicalCacheFile, FileMode.Create, FileAccess.Write)) {
                    ProtoBuf.Serializer.Serialize<CacheFile>(fs, cache);
                    fs.Flush(true);
                }
                lastFlush = DateTime.UtcNow;
            }
        }


        private bool loaded = false;
        private void Load(){
            lock (cache) {
                if (loaded) return;
                if (File.Exists(PhysicalCacheFile)) {
                    using (var fs = new FileStream(PhysicalCacheFile, FileMode.Open, FileAccess.Read)) {
                        cache = ProtoBuf.Serializer.Deserialize<CacheFile>(fs);
                        //Todo- implement cross-process sync
                    }
                }
                lastFlush = DateTime.UtcNow;
                loaded = true;
            }
        }

        private void CleanseAndFlush() {
            Cleanse();
            Flush();
        }


        private void Cleanse(){
            lock(cache){

                var entries_by_priority_asc = cache.hash.OrderBy( e => e.Value.GetPreservationPriority()).ToArray();
                var usedBytes = entries_by_priority_asc.Sum(e => e.Value.sizeInBytes  * (e.Value.data == null ? 0 : 1));
                int entry_count = entries_by_priority_asc.Length;

                //Nullify until we've met the space quota.
                //Remove entries until we mee the max item quota.
                for (var i = 0; i < entries_by_priority_asc.Length; i++ ) {
                    var e = entries_by_priority_asc[i];
                    if (usedBytes > MaxBytes) {
                        if (e.Value.data != null) usedBytes -= e.Value.data.Length;
                        e.Value.data = null;
                    }
                    if (entry_count > MaxItems) {
                        cache.hash.Remove(e.Key);
                        entry_count--;
                    }
                }
                cache.changes_since_cleanse = 0;
            }
        }
        private void MarkRead(CacheEntry e){
            var now = DateTime.UtcNow;
            if (e.recent_reads == null) e.recent_reads = new Queue<DateTime>(8);
            if (e.recent_reads.Count > 7) e.recent_reads.Dequeue();
                    e.recent_reads.Enqueue(now);
                    e.read_count++;
        }
            
    

        private bool TryGetData(string key, out byte[] data, out CacheEntry entry){
            var now = DateTime.UtcNow;
            lock(cache){
                if (cache.hash.TryGetValue(key, out entry)){
                    //Take a reference before we exit the lock
                    data = entry.data;
                    
                    if (data != null){
                        MarkRead(entry);
                        return true;
                    };
                }
            }
            data = null;
            return false;
        }


        private CacheEntry PutData(string key, byte[] data, long ms_cost){
            var now = DateTime.UtcNow;
            lock(cache){
                cache.changes_since_cleanse++;
                CacheEntry entry;
                if (cache.hash.TryGetValue(key, out entry)){
                    if (entry.data == null) entry.recreated_count++;
                }else {
                    entry = new CacheEntry();
                    cache.hash[key] = entry;
                    entry.written = now;
                    entry.loaded = now;
                }
                entry.data = data;
                entry.sizeInBytes = data.Length;
                entry.cost_ms = (int)ms_cost;

                MarkRead(entry);
                return entry;
            }
        }
   
     
        public bool CanProcess(System.Web.HttpContext current, IResponseArgs e) {
            if (e == null) {
                throw new ArgumentNullException("e");
            }

            if (((ResizeSettings)e.RewrittenQuerystring).Cache == ServerCacheMode.No) return false;
            return CanOperate;
        }

        public void Process(System.Web.HttpContext current, IResponseArgs e) {
            
            var key = e.RequestKey;

            CacheEntry entry;
            byte[] data;

            //Ensure cache is loaded
            if (!loaded) Load();

            if (!TryGetData(key ,out data, out entry)){
                Stopwatch sw = new Stopwatch();
                sw.Start();
                //Cache miss - process request, outside of lock
                MemoryStream ms = new MemoryStream(4096);
                e.ResizeImageToStream(ms);
                data = StreamExtensions.CopyToBytes(ms,true);
                sw.Stop();
                //Save to cache
                entry = PutData(key,data, sw.ElapsedMilliseconds);
            }
            
            ((ResponseArgs)e).ResizeImageToStream = delegate(Stream s) {
                s.Write(data,0,data.Length);
            };
            
            current.RemapHandler(new NoCacheHandler(e));

            if (cache.changes_since_cleanse > ChangeThreshold) CleanseAndFlush();
            else if (cache.changes_since_cleanse > 0 && DateTime.UtcNow.Subtract(lastFlush) > new TimeSpan(0, 0, 30)) {
                Flush();
            }
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
}
