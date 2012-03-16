using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;

namespace ImageResizer.Plugins.CustomOverlay {
    public interface IOverlayProvider {
        /// <summary>
        /// Provides an Overlay object for the specified request parameters
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        Overlay GetOverlayInfo(string virtualPath, NameValueCollection query);
    }
}
