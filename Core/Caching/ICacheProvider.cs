/* Copyright (c) 2011 Nathanael Jones. See license.txt */
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Caching {
    /// <summary>
    /// Provides cache selection logic
    /// </summary>
    public interface ICacheProvider {
        /// <summary>
        /// Selects a caching system for the specified request and response
        /// </summary>
        /// <param name="context"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        ICache GetCachingSystem(System.Web.HttpContext context, IResponseArgs responseArgs);
    }
}
