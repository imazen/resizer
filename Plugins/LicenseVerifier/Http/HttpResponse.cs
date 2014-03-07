using System;
using System.Collections.Generic;
using System.Net;

namespace ImageResizer.Plugins.LicenseVerifier.Http {
    public sealed class HttpResponse {
        public Uri ResponseUri { get; set; }
        public IList<HttpHeader> Headers { get; private set; }
        public HttpStatusCode StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public string ContentType { get; set; }
        public long ContentLength { get; set; }
        public string ContentEncoding { get; set; }
        public string Content { get; set; }
        public Exception Error { get; set; }

        public HttpResponse() {
            Headers = new List<HttpHeader>();
        }
    }
}
