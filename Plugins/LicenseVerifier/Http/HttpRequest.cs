using System;
using System.Collections.Generic;
using System.Net;

namespace ImageResizer.Plugins.LicenseVerifier.Http {
    public sealed class HttpRequest {
        public Uri Url { get; set; }
        public IList<HttpHeader> Headers { get; private set; }
        public string Accept { get; set; }
        public string Method { get; set; }
        public string ContentType { get; set; }
        public string Content { get; set; }

        public long ContentLength {
            get { return !String.IsNullOrEmpty(Content) ? Content.Length : 0; }
        }

        public int Timeout { get; set; }
        public ICredentials Credentials { get; set; }

        public HttpRequest() {
            Headers = new List<HttpHeader>();
        }
    }
}
