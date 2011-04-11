using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;

namespace LitS3
{
    /// <summary>
    /// Gets an S3 object, partial object, or just object metadata.
    /// </summary>
    public class GetObjectRequest : S3Request<GetObjectResponse>
    {
        // store this to emulate the behavior of WebRequest.IfModifiedSince
        DateTime? ifUnmodifiedSince;
        bool rangeWasAdded;
        
        public GetObjectRequest(S3Service service, string bucketName, string key)
            : this(service, bucketName, key, false)
        {
        }

        public GetObjectRequest(S3Service service, string bucketName, string key, bool metadataOnly)
            : base(service, metadataOnly ? "HEAD" : "GET", bucketName, key, null)
        {
        }

        /// <summary>
        /// Gets or sets: Return the object only if it has been modified since the 
        /// specified time, otherwise return a 304 (not modified).
        /// </summary>
        public DateTime IfModifiedSince
        {
            get { return WebRequest.IfModifiedSince; }
            set { WebRequest.IfModifiedSince = value; }
        }

        /// <summary>
        /// Gets or sets: Return the object only if it has not been modified since 
        /// the specified time, otherwise return a 412 (precondition failed).
        /// </summary>
        public DateTime IfUnmodifiedSince
        {
            get { return ifUnmodifiedSince ?? DateTime.Now; }
            set { ifUnmodifiedSince = value; }
        }

        /// <summary>
        /// Requests a partial object. You can only add one range request.
        /// </summary>
        /// <param name="range">
        /// Example: Assume an object with a size of 1000 bytes.
        /// AddRange(300) will return all bytes from byte 300 (bytes 300-999), that is: 700 bytes in total.
        /// AddRange(-300) will return the last 300 bytes (bytes 700-999), that is: 300 bytes in total.
        /// </param>
        public void AddRange(int range)
        {
            if (rangeWasAdded)
                throw new InvalidOperationException("S3 only supports a single range specifier.");

            WebRequest.AddRange(range);
            rangeWasAdded = true;
        }

        /// <summary>
        /// Requests a partial object. You can only add one range request.
        /// </summary>
        /// <param name="from">
        /// Example: Assume an object with a size of 1000 bytes.
        /// AddRange(0,499) will return the first 500 bytes (byte offsets 0-499, inclusive).
        /// AddRange(500,999) will return The second 500 bytes (byte offsets 500-999, inclusive).
        /// </param>
        public void AddRange(int from, int to)
        {
            if (rangeWasAdded)
                throw new InvalidOperationException("S3 only supports a single range specifier.");
            
            WebRequest.AddRange(from, to);
            rangeWasAdded = true;
        }

        /// <summary>
        /// Gets or sets: Return the object only if its entity tag (ETag) is the same as the 
        /// one specified, otherwise return a 412 (precondition failed).
        /// </summary>
        public string IfMatch { get; set; }

        protected override void Authorize()
        {
            if (ifUnmodifiedSince.HasValue)
                WebRequest.Headers[HttpRequestHeader.IfUnmodifiedSince] =
                    ifUnmodifiedSince.Value.ToUniversalTime().ToString("r");
            
            if (IfMatch != null)
                WebRequest.Headers[HttpRequestHeader.IfMatch] = IfMatch;

            base.Authorize();
        }
    }

    /// <summary>
    /// Represents the S3 response containing the queried object's metadata and/or data.
    /// </summary>
    public sealed class GetObjectResponse : S3Response
    {
        // created on demand to save memory
        NameValueCollection metadata;

        protected override void ProcessResponse()
        {
            // look for metadata headers
            foreach (string key in WebResponse.Headers)
            {
                if (key.StartsWith(S3Headers.MetadataPrefix))
                {
                    string trimmedKey = key.Substring(S3Headers.MetadataPrefix.Length);

                    foreach (string value in WebResponse.Headers.GetValues(key))
                        Metadata.Add(trimmedKey, value);
                }
                else if (key == S3Headers.MissingMetadata)
                {
                    MissingMetadataHeaders = int.Parse(WebResponse.Headers[S3Headers.MissingMetadata]);
                }
            }
        }

        /// <summary>
        /// Gets the number of metadata entries that were not returned due to the limitations of
        /// the REST API.
        /// </summary>
        public int MissingMetadataHeaders { get; private set; }

        /// <summary>
        /// Gets a collection of name/value metadata pairs associated with this object.
        /// Note that LitS3 manages removing the special "x-amz-meta" header prefix for you.
        /// </summary>
        public NameValueCollection Metadata
        {
            get { return metadata ?? (metadata = new NameValueCollection()); }
        }

        /// <summary>
        /// Gets the last time this object was modified, as calculated internally and stored by S3.
        /// </summary>
        public DateTime LastModified
        {
            get { return WebResponse.LastModified; }
        }

        /// <summary>
        /// Gets the ETag of this object as calculated internally and stored by S3.
        /// </summary>
        public string ETag
        {
            get { return WebResponse.Headers[HttpResponseHeader.ETag]; }
        }

        /// <summary>
        /// Gets the MIME type of the object. This is set to the same value specified when
        /// adding the object. The default content type is binary/octet-stream.
        /// </summary>
        public string ContentType
        {
            get { return WebResponse.ContentType; }
        }

        /// <summary>
        /// Gets the optioanl disposition header, if one was specified when adding the object.
        /// The exception is BitTorrent files which have a non-empty default disposition.
        /// </summary>
        public string ContentDisposition
        {
            get { return WebResponse.Headers["Content-Disposition"]; }
        }

        /// <summary>
        /// Gets the size of the response data stream.
        /// </summary>
        public long ContentLength
        {
            get { return WebResponse.ContentLength; }
        }

        /// <summary>
        /// Gets the HTTP header describing the range of bytes returned in the event that 
        /// a partial object was requested using AddObject.AddRange().
        /// </summary>
        public string ContentRange
        {
            get { return WebResponse.Headers[HttpResponseHeader.ContentRange]; }
        }

        /// <summary>
        /// Gets a stream containing the object data (if included).
        /// </summary>
        public Stream GetResponseStream()
        {
            return WebResponse.GetResponseStream();
        }
    }
}