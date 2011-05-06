/* Copyright (c) 2011 Nathanael Jones. See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections.Specialized;

namespace ImageResizer.Caching {
    /// <summary>
    /// IResponseArgs implementation
    /// </summary>
    public class ResponseArgs : IResponseArgs {

        public ResponseArgs() { }

        protected ModifiedDateDelegate getModifiedDateUTC;
        /// <summary>
        /// A callback method to return the last modified date of the source file if available, or DateTime.MinValue if not.
        /// </summary>
        /// <returns></returns>
        public ModifiedDateDelegate GetModifiedDateUTC
        {
          get { return getModifiedDateUTC; }
          set { getModifiedDateUTC = value; }
        }

        protected ResizeImageDelegate resizeImageToStream;
        /// <summary>
        /// A callback method that will resize and encode the image into a stream.
        /// </summary>
        public ResizeImageDelegate ResizeImageToStream
        {
          get { return resizeImageToStream; }
          set { resizeImageToStream = value; }
        }

        protected string requestKey = null;
        /// <summary>
        /// A value derived from the request. Can be used as a cache key. 
        /// </summary>
        public string RequestKey
        {
            get { return requestKey; }
            set { requestKey = value; }
        }

        protected string suggestedExtension = null;

        public string SuggestedExtension {
            get { return suggestedExtension; }
            set { suggestedExtension = value; }
        }

        protected bool hasModifiedDate;
        /// <summary>
        /// True if the source file/record has a modified date
        /// </summary>
        public bool HasModifiedDate {
            get { return hasModifiedDate; }
            set { hasModifiedDate = value; }
        }


        protected IResponseHeaders responseHeaders = new ResponseHeaders();

        public IResponseHeaders ResponseHeaders {
            get { return responseHeaders; }
            set { responseHeaders = value; }
        }


        protected NameValueCollection rewrittenQuerystring;

        public NameValueCollection RewrittenQuerystring {
            get { return rewrittenQuerystring; }
            set { rewrittenQuerystring = value; }
        }
    }
}
