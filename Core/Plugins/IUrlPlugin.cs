using System;
using System.Collections.Generic;
using System.Text;

namespace fbs.ImageResizer.Plugins {
    public interface IUrlPlugin {
        /// <summary>
        /// If the plugin adds support for new file extensions (such as "psd"), they should be returned by this method.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetSupportedFileExtensions();
        /// <summary>
        /// If the plugin reads any values from the querystring, the names of the keys should be specified here. 
        /// This information is required so that the HttpModule knows when to handle an image request.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetSupportedQuerystringKeys();
    }
}
