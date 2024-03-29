// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections.Specialized;

namespace ImageResizer.Caching
{
    /// <summary>
    ///     IResponseArgs implementation
    /// </summary>
    public class ResponseArgs : IResponseArgs
    {
        public ResponseArgs()
        {
            HasModifiedDate = false;
        }

        /// <summary>
        ///     Obsolete. Do not use;  RequestKey will include the modified date if present.
        /// </summary>
        [Obsolete("RequestKey will include the modified date if present. No longer populated")]
        public ModifiedDateDelegate GetModifiedDateUTC { get; set; }


        public GetSourceImageDelegate GetSourceImage { get; set; }

        /// <summary>
        ///     A callback method that will resize and encode the image into a stream.
        /// </summary>
        public ResizeImageDelegate ResizeImageToStream { get; set; }

        /// <summary>
        ///     A value derived from the request. Can be used as a cache key.
        /// </summary>
        public string RequestKey { get; set; }

        public string SuggestedExtension { get; set; }


        /// <summary>
        ///     Obsolete. Do not use;  RequestKey will include the modified date if present.
        /// </summary>
        [Obsolete("RequestKey will include the modified date if present. No longer populated")]
        public bool HasModifiedDate { get; set; }

        protected IResponseHeaders responseHeaders = new ResponseHeaders();

        /// <summary>
        ///     The content-type of the data, among other things. Set ResponseHeaders.ApplyDuringPreSendRequestHeaders to
        ///     automatically
        ///     write caching headers based on ResponseHeaders values.
        ///     Caching systems that use redirects may use this data as hints when configuring caching on the remote server.
        /// </summary>
        public IResponseHeaders ResponseHeaders
        {
            get => responseHeaders;
            set => responseHeaders = value;
        }


        protected NameValueCollection rewrittenQuerystring;

        /// <summary>
        ///     The rewritten querystring. Can be useful for caching systems that accept querystring arguments.
        /// </summary>
        public NameValueCollection RewrittenQuerystring
        {
            get => rewrittenQuerystring;
            set => rewrittenQuerystring = value;
        }
    }
}