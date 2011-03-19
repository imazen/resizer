using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace fbs.ImageResizer.Caching {
    /// <summary>
    /// A collection of data and callbacks that can be passed to a caching object.
    /// </summary>
    public interface ICacheEventArgs {
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
        /// A string, which can contain any kind of data, which should be used as the basis for the cache key
        /// </summary>
        string CacheKey { get; }
        /// <summary>
        /// The content-type of the data
        /// </summary>
        string ContentType { get; }
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
