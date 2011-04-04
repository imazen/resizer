using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Web;
using System.Collections.Specialized;

namespace ImageResizer.Caching {
    /// <summary>
    /// A callback method that will resize, encode, and write the data to the given stream.
    /// </summary>
    public delegate void ResizeImageDelegate(Stream s);
    /// <summary>
    /// A callback method to return the last modified date of the source file if available, or DateTime.MinValue if not available.
    /// </summary>
    /// <returns></returns>
    public delegate DateTime ModifiedDateDelegate();

    /// <summary>
    /// A collection of data and callbacks that can be passed to a caching object.
    /// </summary>
    public interface IResponseArgs {


        /// <summary>
        /// A string derived from the request, which can contain any kind of data. To get a cache key that varies with the source modified date, 
        /// it should be combined with the value of GetModifiedDateUTC() and hashed.
        /// </summary>
        string RequestKey { get; }
        /// <summary>
        /// The rewritten querystring. Can be useful for caching systems that accept querystring arguments.
        /// </summary>
        NameValueCollection RewrittenQuerystring { get; }

        /// <summary>
        /// A file extension appropriate for the resulting data. May be different than the extension on the original request.
        /// </summary>
        string SuggestedExtension { get; }

        /// <summary>
        /// The content-type of the data, among other things. Set ResponseHeaders.ApplyDuringPreSendRequestHeaders to automatically
        /// write caching headers based on ResponseHeaders values.
        /// Caching systems that use redirects may use this data as hints when configuring caching on the remote server.
        /// </summary>
        IResponseHeaders ResponseHeaders { get; set; }
        /// <summary>
        /// A delegate that returns the modified date of the source data.
        /// </summary>
        ModifiedDateDelegate GetModifiedDateUTC { get; }
        /// <summary>
        /// True if a modified date is available for verifying cache integrity.
        /// </summary>
        bool HasModifiedDate { get; }
        /// <summary>
        /// A callback method that will resize, encode, and write the data to the given stream.
        /// </summary>
        ResizeImageDelegate ResizeImageToStream { get; }

    }
}
