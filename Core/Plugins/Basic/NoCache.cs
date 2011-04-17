using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Plugins;
using ImageResizer.Caching;

namespace ImageResizer.Plugins.Basic {
    /// <summary>
    /// Default cache when DiskCache isn't installed. 
    /// Useful for debugging purposes but unsuited for production use, and will use large quanities of RAM. (Scales to fewer than 5 concurrent requests).
    /// Serves content directly to the client.
    /// </summary>
    public class NoCache :ICache, IPlugin {
        /// <summary>
        /// Installs the caching system as the first choice.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this); return this;
        }
        /// <summary>
        /// Removes the plugin. 
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this); return true;
        }

        /// <summary>
        /// Sends the response directly to the client with no caching logic.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="e"></param>
        public void Process(System.Web.HttpContext context, IResponseArgs e) {
            context.RemapHandler(new NoCacheHandler(e));
        }


        public bool CanProcess(System.Web.HttpContext current, IResponseArgs e) {
            return true;
        }
    }

}
