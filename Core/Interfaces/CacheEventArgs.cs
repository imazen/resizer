using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace fbs.ImageResizer {
    public class CacheEventArgs : fbs.ImageResizer.Interfaces.ICacheEventArgs1 {

        public CacheEventArgs() { }
        /// <summary>
        /// A callback method that will resize and encode the image into a stream.
        /// </summary>
        public delegate void ResizeImageDelegate(Stream s);
        /// <summary>
        /// A callback method to return the last modified date of the source file if available, or DateTime.MinValue if not.
        /// </summary>
        /// <returns></returns>
        public delegate DateTime ModifiedDateDelegate();


        private ModifiedDateDelegate getModifiedDateUTC;
        /// <summary>
        /// A callback method to return the last modified date of the source file if available, or DateTime.MinValue if not.
        /// </summary>
        /// <returns></returns>
        public ModifiedDateDelegate GetModifiedDateUTC
        {
          get { return getModifiedDateUTC; }
          set { getModifiedDateUTC = value; }
        }

        private ResizeImageDelegate resizeImageToStream;
        /// <summary>
        /// A callback method that will resize and encode the image into a stream.
        /// </summary>
        public ResizeImageDelegate ResizeImageToStream
        {
          get { return resizeImageToStream; }
          set { resizeImageToStream = value; }
        }


        private string contentType = null;
        /// <summary>
        /// The mime-type of the encoded image
        /// </summary>
        public string ContentType
        {
            get { return contentType; }
            set { contentType = value; }
        }
       
        private string cacheKey = null;
        /// <summary>
        /// A string to use as a cache key. May be hashed for normalization purposes.
        /// </summary>
        public string CacheKey
        {
          get { return cacheKey; }
          set { cacheKey = value; }
        }


        private bool hasModifiedDate;
        /// <summary>
        /// True if the source file/record has a modified date
        /// </summary>
        public bool HasModifiedDate {
            get { return hasModifiedDate; }
            set { hasModifiedDate = value; }
        }
    }
}
