// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.Collections.Generic;

namespace ImageResizer.Plugins
{
    /// <summary>
    ///     For plugins that access the query string (important!)
    /// </summary>
    public interface IQuerystringPlugin
    {
        /// <summary>
        ///     If the plugin reads any values from the querystring, the names of the keys should be specified here.
        ///     This information is required so that the HttpModule knows when to handle an image request.
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetSupportedQuerystringKeys();
    }
}