// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.Threading.Tasks;
using System.Web;
using ImageResizer.Caching;
using ImageResizer.Configuration;

namespace ImageResizer.Plugins.Basic
{
    /// <summary>
    ///     Default cache when DiskCache isn't installed.
    ///     Useful for debugging purposes but unsuited for production use, and will use large quantities of RAM. (Scales to
    ///     fewer than 5 concurrent requests).
    ///     Serves content directly to the client from memory.
    /// </summary>
    public class NoCache : IAsyncTyrantCache, IPlugin
    {
        /// <summary>
        ///     Installs the caching system as the first choice.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public IPlugin Install(Config c)
        {
            c.Plugins.add_plugin(this);
            return this;
        }

        /// <summary>
        ///     Removes the plugin.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            return true;
        }
        
        public bool CanProcess(HttpContext current, IAsyncResponsePlan e)
        {
            return true;
        }

        public Task ProcessAsync(HttpContext context, IAsyncResponsePlan e)
        {
            context.RemapHandler(new NoCacheAsyncHandler(e));
            return Task.FromResult(true);
        }
    }
}