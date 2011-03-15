using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Net.Cache;

namespace LitS3
{
    /// <summary>
    /// Uploads an object to S3.
    /// </summary>
    public class AddObjectRequest : S3Request<AddObjectResponse>
    {
        // created on demand to save memory
        NameValueCollection metadata;
        bool contentLengthWasSet;

        public AddObjectRequest(S3Service service, string bucketName, string objectKey)
            : base(service, "PUT", bucketName, objectKey, null)
        {
            this.CannedAcl = CannedAcl.Private;
            // disabled for now - results in flaky behavior (see remarks on S3Request.ServicePoint)
            // WebRequest.ServicePoint.Expect100Continue = true;
            WebRequest.AllowWriteStreamBuffering = false; // important! we could be sending a LOT of data
        }

        /// <summary>
        /// Gets or sets the "canned" access control to apply to this object. The default is
        /// Private. More complex permission sets than the CannedAcl values are allowed, but
        /// you must use SetObjectAclRequest (not currently implemented).
        /// </summary>
        public CannedAcl CannedAcl { get; set; }

        /// <summary>
        /// Gets or sets the optional expiration date of the object. If specified, it will be 
        /// stored by S3 and returned as a standard Expires header when the object is retrieved.
        /// </summary>
        public DateTime? Expires { get; set; }

        /// <summary>
        /// Gets a collection where you can store name/value metadata pairs to be stored along
        /// with this object. Since we are using the REST API, the names and values are limited
        /// to ASCII encoding. Additionally, Amazon imposes a 2k limit on the total HTTP header
        /// size which includes metadata. Note that LitS3 manages adding the special "x-amz-meta"
        /// prefix for you.
        /// </summary>
        public NameValueCollection Metadata
        {
            get { return metadata ?? (metadata = new NameValueCollection()); }
        }

        /// <summary>
        /// Gets or sets the cache control for this request as the raw HTTP header you would like
        /// S3 to return along with your object when requested. An example value for this might
        /// be "max-age=3600, must-revalidate".
        /// </summary>
        public string CacheControl { get; set; }

        /// <summary>
        /// Gets or sets the MIME type of this object. It will be stored by S3 and returned as a
        /// standard Content-Type header when the object is retrieved.
        /// </summary>
        public string ContentType
        {
            get { return WebRequest.ContentType; }
            set { WebRequest.ContentType = value; }
        }

        /// <summary>
        /// Gets or sets the size of the object you are adding. Setting this property is required.
        /// </summary>
        public long ContentLength
        {
            get { return WebRequest.ContentLength; }
            set { WebRequest.ContentLength = value; contentLengthWasSet = true; }
        }

        /// <summary>
        /// Gets or sets the base64 encoded 128-bit MD5 digest of the message (without the headers)
        /// according to RFC 1864.
        /// </summary>
        public string ContentMD5
        {
            get { return WebRequest.Headers[HttpRequestHeader.ContentMd5]; }
            set { WebRequest.Headers[HttpRequestHeader.ContentMd5] = value; }
        }

        /// <summary>
        /// Gets or sets presentational information for the object. It will be stored by S3 and
        /// returned as a standard Content-Disposition header when the object is retrieved.
        /// </summary>
        /// <remarks>
        /// One use of this header is to cause a browser to download this resource as a file attachment
        /// instead of displaying it inline. For that behavior, use a string like:
        /// "Content-disposition: attachment; filename=mytextfile.txt"
        /// </remarks>
        public string ContentDisposition
        {
            get { return WebRequest.Headers["Content-Disposition"]; }
            set { WebRequest.Headers["Content-Disposition"] = value; }
        }

        /// <summary>
        /// Gets or sets the specified encoding of the object data. It will be stored by S3 
        /// and returned as a standard Content-Encoding header when the object is retrieved.
        /// </summary>
        public string ContentEncoding
        {
            get { return WebRequest.Headers[HttpRequestHeader.ContentEncoding]; }
            set { WebRequest.Headers[HttpRequestHeader.ContentEncoding] = value; }
        }

        protected override void Authorize()
        {
            // sanity check
            if (!contentLengthWasSet)
                throw new InvalidOperationException("Amazon S3 requires that you specify ContentLength when adding an object.");

            // write canned ACL, if it's not private (which is implied by default)
            switch (CannedAcl)
            {
                case CannedAcl.PublicRead:
                    WebRequest.Headers[S3Headers.CannedAcl] = "public-read"; break;
                case CannedAcl.PublicReadWrite:
                    WebRequest.Headers[S3Headers.CannedAcl] = "public-read-write"; break;
                case CannedAcl.AuthenticatedRead:
                    WebRequest.Headers[S3Headers.CannedAcl] = "authenticated-read"; break;
            }

            if (Expires.HasValue)
                WebRequest.Headers[HttpRequestHeader.Expires] = Expires.Value.ToUniversalTime().ToString("r");

            if (CacheControl != null)
                WebRequest.Headers[HttpRequestHeader.CacheControl] = CacheControl;

            if (metadata != null)
                foreach (string key in metadata)
                    foreach (string value in metadata.GetValues(key))
                        WebRequest.Headers.Add(S3Headers.MetadataPrefix + key, value);

            base.Authorize();
        }

        /// <summary>
        /// Submits the request to the server and retrieves a Stream for writing object data to.
        /// </summary>
        public Stream GetRequestStream()
        {
            AuthorizeIfNecessary();
            return WebRequest.GetRequestStream();
        }

        /// <summary>
        /// Submits the request to the server and performs the given action with the request
        /// stream which should be filled with the object's data. The GetResponse() method
        /// will automatically be called after the action is executed.
        /// </summary>
        public void PerformWithRequestStream(Action<Stream> action)
        {
            using (Stream stream = GetRequestStream())
                action(stream);

            GetResponse().Close();
        }

        /// <summary>
        /// Begins an asynchronous request for a Stream object to use to write object data.
        /// </summary>
        public IAsyncResult BeginGetRequestStream(AsyncCallback callback, object state)
        {
            AuthorizeIfNecessary();
            return WebRequest.BeginGetRequestStream(callback, state);
        }

        /// <summary>
        /// Ends an asynchronous call to BeginGetRequestStream(). 
        /// </summary>
        public Stream EndGetRequestStream(IAsyncResult asyncResult)
        {
            return WebRequest.EndGetRequestStream(asyncResult);
        }
    }

    /// <summary>
    /// Represents the response returned by S3 after adding an object with AddObjectRequest.
    /// </summary>
    public sealed class AddObjectResponse : S3Response
    {
        /// <summary>
        /// Gets the added object's ETag as calculated by S3. You can compare this to an ETag you
        /// calculate locally to verify that S3 received the file correctly.
        /// </summary>
        public string ETag { get; private set; }

        protected override void ProcessResponse()
        {
            ETag = WebResponse.Headers[HttpResponseHeader.ETag];
        }
    }
}
