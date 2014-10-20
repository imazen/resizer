/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins {
    /// <summary>
    /// For plugins that access the query string (important!)
    /// </summary>
    public interface IQuerystringPlugin {
        /// <summary>
        /// If the plugin reads any values from the querystring, the names of the keys should be specified here. 
        /// This information is required so that the HttpModule knows when to handle an image request.
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetSupportedQuerystringKeys();
    }
}
