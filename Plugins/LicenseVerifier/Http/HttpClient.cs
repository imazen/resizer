using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace ImageResizer.Plugins.LicenseVerifier.Http {
    public sealed class HttpClient : IHttpClient {
        private const string USER_AGENT = "ImageResizer-LicenseVerifier";

        public HttpResponse Send(HttpRequest httpRequest) {
            return GenerateHttpResponse(InitializeHttpWebRequest(httpRequest));
        }

        private static HttpWebRequest InitializeHttpWebRequest(HttpRequest httpRequest) {
            HttpWebRequest httpWebRequest = WebRequest.Create(httpRequest.Url) as HttpWebRequest;
            httpWebRequest.Accept = httpRequest.Accept;
            httpWebRequest.UserAgent = USER_AGENT;
            httpWebRequest.Method = httpRequest.Method;
            httpWebRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip | DecompressionMethods.None;

            if (httpWebRequest.Method == HttpMethod.Put || httpWebRequest.Method == HttpMethod.Post)
                AppendRequestContent(httpRequest, httpWebRequest);

            return httpWebRequest;
        }

        private static void AppendRequestContent(HttpRequest httpRequest, HttpWebRequest httpWebRequest) {
            if (!String.IsNullOrEmpty(httpRequest.ContentType) &&
                !String.IsNullOrEmpty(httpRequest.Content)) {
                httpWebRequest.ContentType = httpRequest.ContentType;
                httpWebRequest.ContentLength = httpRequest.ContentLength;
                using (var httpRequestStream = httpWebRequest.GetRequestStream()) {
                    byte[] requestBuffer = System.Text.Encoding.UTF8.GetBytes(httpRequest.Content);
                    httpRequestStream.Write(requestBuffer, 0, requestBuffer.Length);
                }
            }
        }

        private static HttpResponse GenerateHttpResponse(HttpWebRequest httpWebRequest) {
            var httpResponse = new HttpResponse();

            try {
                HttpWebResponse httpWebResponse = GenerateHttpWebResponse(httpWebRequest);
                MapResponses(httpWebResponse, httpResponse);
            }
            catch (Exception exception) {
                httpResponse.Error = exception;
            }

            return httpResponse;
        }

        private static HttpWebResponse GenerateHttpWebResponse(HttpWebRequest httpWebRequest) {
            HttpWebResponse httpWebResponse = null;

            try {
                httpWebResponse = httpWebRequest.GetResponse() as HttpWebResponse;
            }
            catch (WebException webException) {
                if (webException.InnerException is SocketException) {
                    throw;
                }
                else {
                    httpWebResponse = webException.Response as HttpWebResponse;
                }
            }

            return httpWebResponse;
        }

        private static void MapResponses(HttpWebResponse httpWebResponse, HttpResponse httpResponse) {
            using (httpWebResponse) {
                httpResponse.ResponseUri = httpWebResponse.ResponseUri;
                httpResponse.StatusCode = httpWebResponse.StatusCode;
                httpResponse.StatusDescription = httpWebResponse.StatusDescription;
                httpResponse.ContentType = httpWebResponse.ContentType;
                httpResponse.ContentLength = httpWebResponse.ContentLength;
                httpResponse.ContentEncoding = httpWebResponse.ContentEncoding;
                httpResponse.Content = GetContentFrom(httpWebResponse);

                foreach (string headerName in httpWebResponse.Headers.AllKeys) {
                    string headerValue = httpWebResponse.Headers[headerName];
                    httpResponse.Headers.Add(new HttpHeader { Name = headerName, Value = headerValue });
                }
            }
        }

        private static string GetContentFrom(HttpWebResponse httpWebResponse) {
            string content;

            using (StreamReader streamReader = new StreamReader(httpWebResponse.GetResponseStream())) {
                content = streamReader.ReadToEnd();
            }

            return content;
        }
    }
}
