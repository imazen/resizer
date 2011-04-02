using System;
using System.Collections.Generic;
using System.Text;

namespace fbs.ImageResizer.Plugins {
    public interface IQuerystringPlugin {
        /// <summary>
        /// If the plugin reads any values from the querystring, the names of the keys should be specified here. 
        /// This information is required so that the HttpModule knows when to handle an image request.
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetSupportedQuerystringKeys();
    }
}
