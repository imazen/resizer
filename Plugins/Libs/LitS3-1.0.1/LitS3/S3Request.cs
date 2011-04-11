using System;
using System.Net;
using System.Text;

namespace LitS3
{
    /// <summary>
    /// The base class for all S3 requests.
    /// </summary>
    public abstract class S3Request
    {
        string bucketName; // remember this for signing the request later

        /// <summary>
        /// Gets the service this request will operate against.
        /// </summary>
        public S3Service Service { get; private set; }

        protected HttpWebRequest WebRequest { get; private set; }

        internal S3Request(S3Service service, string method, string bucketName, string objectKey,
            string queryString)
        {
            this.Service = service;
            this.bucketName = bucketName;
            this.WebRequest = CreateWebRequest(method, objectKey, queryString);
        }

        HttpWebRequest CreateWebRequest(string method, string objectKey, string queryString)
        {
            var uriString = new StringBuilder(Service.UseSsl ? "https://" : "http://");

            if (bucketName != null && Service.UseSubdomains)
                uriString.Append(bucketName).Append('.');

            uriString.Append(Service.Host);

            if (Service.CustomPort != 0)
                uriString.Append(':').Append(Service.CustomPort);

            uriString.Append('/');

            if (bucketName != null && !Service.UseSubdomains)
                uriString.Append(bucketName).Append('/');

            // EscapeDataString allows you to use basically any key for an object, including
            // keys with tricky URI characters like "+".
            if (objectKey != null)
                uriString.Append(Uri.EscapeDataString(objectKey));

            if (queryString != null)
                uriString.Append(queryString);

            var uri = new Uri(uriString.ToString());

            HttpWebRequest request = (HttpWebRequest)System.Net.WebRequest.Create(uri);
            request.Method = method;
            request.AllowWriteStreamBuffering = true; // AddObject will make this false
            request.AllowAutoRedirect = true;

            // S3 will never "timeout" a request. However, network delays may still cause a
            // timeout according to WebRequest's ReadWriteTimeout property, which you can modify.
            request.Timeout = int.MaxValue;
            
            return request;
        }

        #region Expose S3-relevant mirrored properties of HttpWebRequest

        /// <summary>
        /// Gets a value that indicates whether a response has been received from S3.
        /// </summary>
        public bool HaveResponse
        {
            get { return WebRequest.HaveResponse; }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether to make a persistent connection to S3.
        /// </summary>
        public bool KeepAlive
        {
            get { return WebRequest.KeepAlive; }
            set { WebRequest.KeepAlive = value; }
        }

        /// <summary>
        /// Gets or sets proxy information for this request.
        /// </summary>
        public IWebProxy Proxy
        {
            get { return WebRequest.Proxy; }
            set { WebRequest.Proxy = value; }
        }

        /// <summary>
        /// Gets or sets a time-out in milliseconds when writing to or reading from a stream.
        /// The default value is 5 minutes.
        /// </summary>
        public int ReadWriteTimeout
        {
            get { return WebRequest.ReadWriteTimeout; }
            set { WebRequest.ReadWriteTimeout = value; }
        }

        /// <summary>
        /// Gets the service point to use for this request. See remarks on this property if you
        /// plan on using Expect100Continue.
        /// </summary>
        /// <remarks>
        /// In specific circumstances, the S3 request will hang indefinitely if Expect100Continue is 
        /// set to true and S3 immediately responds with a HTTP 5xx server error after the request is 
        /// issued and before any data is written to the stream. The downside to leaving this property 
        /// at false is that you'll waste bandwidth and time if S3 knows the PUT is going to fail at 
        /// the very start of the request.
        /// </remarks>
        public ServicePoint ServicePoint
        {
            get { return WebRequest.ServicePoint; }
        }

        #endregion

        protected void AuthorizeIfNecessary()
        {
            if (!S3Authorizer.IsAuthorized(WebRequest)) Authorize();
        }

        protected virtual void Authorize()
        {
            if (S3Authorizer.IsAuthorized(WebRequest))
                throw new InvalidOperationException("This request has already been authorized.");

            Service.AuthorizeRequest(this, WebRequest, bucketName);
        }

        protected void TryThrowS3Exception(WebException exception)
        {
            // if this is a protocol error and the response type is XML, we can expect that
            // S3 sent us an <Error> message.
            if (exception.Status == WebExceptionStatus.ProtocolError &&
                exception.Response.ContentType == "application/xml" &&
                (exception.Response.ContentLength > 0 || 
                 exception.Response.Headers[HttpResponseHeader.TransferEncoding] == "chunked"))
            {
                var wrapped = S3Exception.FromWebException(exception);
                if (wrapped != null)
                    throw wrapped; // do this on a separate statement so the debugger can re-execute
            }
        }
    }

    /// <summary>
    /// Describes an event involving an S3Request.
    /// </summary>
    public class S3RequestArgs : EventArgs
    {
        public S3Request Request { get; private set; }

        public S3RequestArgs(S3Request request)
        {
            this.Request = request;
        }
    }

    /// <summary>
    /// Common base class for all concrete S3Requests, pairs each one tightly with its S3Response
    /// counterpart.
    /// </summary>
    public abstract class S3Request<TResponse> : S3Request
        where TResponse : S3Response, new()
    {
        internal S3Request(S3Service service, string method, string bucketName, string objectKey,
            string queryString)
            : base(service, method, bucketName, objectKey, queryString)
        {
        }

        /// <summary>
        /// Gets the S3 REST response synchronously.
        /// </summary>
        public virtual TResponse GetResponse()
        {
            AuthorizeIfNecessary();

            try
            {
                return new TResponse { WebResponse = (HttpWebResponse)WebRequest.GetResponse() };
            }
            catch (WebException exception)
            {
                TryThrowS3Exception(exception);
                throw;
            }
        }

        /// <summary>
        /// Begins an asynchronous request to S3.
        /// </summary>
        public virtual IAsyncResult BeginGetResponse(AsyncCallback callback, object state)
        {
            AuthorizeIfNecessary();
            return WebRequest.BeginGetResponse(callback, state);
        }

        /// <summary>
        /// Ends an asynchronous call to BeginGetResponse().
        /// </summary>
        public virtual TResponse EndGetResponse(IAsyncResult asyncResult)
        {
            try
            {
                return new TResponse { WebResponse = (HttpWebResponse)WebRequest.EndGetResponse(asyncResult) };
            }
            catch (WebException exception)
            {
                TryThrowS3Exception(exception);
                throw;
            }
        }

        /// <summary>
        /// Cancels an asynchronous request to S3.
        /// </summary>
        public void Abort()
        {
            WebRequest.Abort();
        }
    }
}
