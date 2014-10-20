using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;

namespace ImageResizer.Plugins.CustomOverlay {
    public interface IOverlayProvider:IQuerystringPlugin {
        /// <summary>
        /// Provides a collection of Overlay objects for the specified request parameters
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        IEnumerable<Overlay> GetOverlays(string virtualPath, NameValueCollection query);
    }
}
