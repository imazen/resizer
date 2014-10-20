using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

namespace LibCassini.Client {
    public class ClientResponse {

        public ClientResponse(HttpWebResponse response) {
            this.response = response;
            if (response == null) return;
            body = response.GetResponseStream();
            responseHeaders = response.Headers;
            statusCode = response.StatusCode;
        }

        private HttpWebResponse response = null;

        public HttpWebResponse Response {
            get { return response; }
        }

        private HttpStatusCode statusCode = HttpStatusCode.RequestTimeout;

        public HttpStatusCode StatusCode {
            get { return statusCode; }
        }

        private Stream body = null;

        public Stream Body {
            get { return body; }
        }

        private WebHeaderCollection responseHeaders = null;

        public WebHeaderCollection ResponseHeaders {
            get { return responseHeaders; }
        }
    }
}
