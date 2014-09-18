using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Caching;
using ImageResizer.Plugins.SourceMemCache;
using ImageResizer.Plugins.DiskCache;
using System.Web;
using System.IO;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.MemCache {
    public class MemCachePlugin:IPlugin,ICache {


        
        /// <summary>
        /// Defaults to 10MB limit, and samples usage over the last 10 minutes when deciding what to remove. Stuff not used in the last 10 minutes gets discarded even if the limit hasn't been reached.
        /// </summary>
        public MemCachePlugin() : this(1024 * 1024 * 1024, new TimeSpan(0, 10, 0)) { }

        public MemCachePlugin(long maxBytes, TimeSpan usageWindow) {
            //Cleanup at most once per minute, unless hitting the limits. 
            cache = new ConstrainedCache<string, MemCacheResult>(StringComparer.OrdinalIgnoreCase, delegate(string key, MemCacheResult file) {
                return key.Length * 4 + file.BytesOccupied;
            }, maxBytes, usageWindow, new TimeSpan(0, 1, 0)); 

        }

        private ConstrainedCache<string, MemCacheResult> cache;
        private LockProvider locks = new LockProvider();

        public bool CanProcess(System.Web.HttpContext current, IResponseArgs e) {
            return "true".Equals(e.RewrittenQuerystring["mcache"], StringComparison.OrdinalIgnoreCase);
        }

        public void Process(System.Web.HttpContext current, IResponseArgs e) {

            //Use alternate cache key if provided
            string key = e.RequestKey;

            //If cached, serve it. 
            var c = cache.Get(key);
            if (c != null) {
                Serve(current, e, c);
                return;
            }
            //If not, let's cache it.
            locks.TryExecute(key, 3000, delegate() {
                c = cache.Get(key);
                if (c == null) {
                    using (var data = new MemoryStream()){
                        e.ResizeImageToStream(data);//Very long-running call
                        c = new MemCacheResult(data.CopyToBytes(true));
                    }
                    cache.Set(key, c);//Save to cache (may trigger cleanup)
                }
                Serve(current, e, c);
                return;
            });

        }

        private void Serve(HttpContext context, IResponseArgs e, MemCacheResult result) {
            context.RemapHandler(new MemCacheHandler(e,result.Data));
        }
        
        /// <summary>
        /// Adds the plugin to the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }
        /// <summary>
        /// Removes the plugin from the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

    }
}
