using System;
using System.Collections.Specialized;

namespace LitS3
{
    /// <summary>
    /// For COPY operations, indicates whether the metadata is copied from the source object or replaced
    /// with metadata provided in the request. If copied, it remains unchanged. Otherwise, all original
    /// metadata is replaced by the metadata you specify.
    /// </summary>
    public enum MetadataDirective
    {
        /// <summary> Check the contents of the Metadata property. If empty, Copy, else Replace. </summary>
        Automatic,
        /// <summary> Copy the metadata from the source object. This is the default. </summary>
        Copy,
        /// <summary> Replace the metadata with the metadata specified in the COPY operation. </summary>
        Replace
    }

    /// <summary>
    /// Copies an existing object in S3. Even if you successfully get a CopyObjectResponse
    /// without an exception, you should inspect it to see if any errors occurred while copying.
    /// </summary>
    public class CopyObjectRequest : S3Request<CopyObjectResponse>
    {
        // created on demand to save memory
        NameValueCollection metadata;

        public CopyObjectRequest(S3Service service, string bucketName, string sourceObjectKey,
            string destObjectKey)
            : this(service, bucketName, sourceObjectKey, bucketName, destObjectKey)
        {
        }

        public CopyObjectRequest(S3Service service, string sourceBucketName, string sourceObjectKey,
            string destBucketName, string destObjectKey)
            : base(service, "PUT", destBucketName, destObjectKey, null)
        {
            this.CannedAcl = CannedAcl.Private;
            this.MetadataDirective = MetadataDirective.Automatic;
            WebRequest.Headers[S3Headers.CopySource] = sourceBucketName + "/" + sourceObjectKey;
        }

        /// <summary>
        /// Gets or sets the "canned" access control to apply to this object. The default is
        /// Private. More complex permission sets than the CannedAcl values are allowed, but
        /// you must use SetObjectAclRequest (not currently implemented).
        /// </summary>
        public CannedAcl CannedAcl { get; set; }

        /// <summary>
        /// Gets or sets the ETag value that will be used to conditionally execute the COPY
        /// operation on S3. If the ETag value set here does not match the ETag of the source
        /// object, S3 will return a "412 Precondition Failed" error.
        /// </summary>
        public string CopyIfMatchETag
        {
            get { return WebRequest.Headers[S3Headers.CopySourceIfMatch]; }
            set { WebRequest.Headers[S3Headers.CopySourceIfMatch] = value; }
        }

        /// <summary>
        /// Gets or sets the ETag value that will be used to conditionally execute the COPY
        /// operation on S3. If the ETag value set here matches the ETag of the source object,
        /// S3 will return a "412 Precondition Failed" error.
        /// </summary>
        public string CopyIfNoneMatchETag
        {
            get { return WebRequest.Headers[S3Headers.CopySourceIfNoneMatch]; }
            set { WebRequest.Headers[S3Headers.CopySourceIfNoneMatch] = value; }
        }

        /// <summary>
        /// Gets or sets the date value that will be used to conditionally execute the COPY
        /// operation on S3. If the date value set here matches the "Last-Modified" data of
        /// the source object, S3 will return a "412 Precondition Failed" error.
        /// </summary>
        public DateTime? CopyIfUnmodifiedSince { get; set; }

        /// <summary>
        /// Gets or sets the date value that will be used to conditionally execute the COPY
        /// operation on S3. If the date value set here does not match the "Last-Modified" data
        /// of the source object, S3 will return a "412 Precondition Failed" error.
        /// </summary>
        public DateTime? CopyIfModifiedSince { get; set; }

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
        /// Gets or sets the metadata directive for the current COPY operation. The metadata
        /// directive tells S3 whether to copy the metadata from the source object (default) or
        /// replace the metadata of the new object with the provided metadata.
        /// </summary>
        public MetadataDirective MetadataDirective { get; set; }

        protected override void Authorize()
        {
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

            if (CopyIfUnmodifiedSince.HasValue)
                WebRequest.Headers[S3Headers.CopySourceIfUnmodifiedSince] = CopyIfUnmodifiedSince.Value.ToString("r");
            
            if (CopyIfModifiedSince.HasValue)
                WebRequest.Headers[S3Headers.CopySourceIfModifiedSince] = CopyIfModifiedSince.Value.ToString("r");

            if (MetadataDirective == MetadataDirective.Replace ||
                (MetadataDirective == MetadataDirective.Automatic && metadata != null))
            {
                WebRequest.Headers[S3Headers.MetadataDirective] = "REPLACE";

                if (metadata != null)
                    foreach (string key in metadata)
                        foreach (string value in metadata.GetValues(key))
                            WebRequest.Headers.Add(S3Headers.MetadataPrefix + key, value);
            }

            base.Authorize();
        }
    }

    public sealed class CopyObjectResponse : S3Response
    {
        /// <summary>
        /// Gets the last time this object was modified, as calculated internally and stored by S3.
        /// </summary>
        public DateTime LastModified { get; private set; }

        /// <summary>
        /// Gets the ETag of this object as calculated internally and stored by S3.
        /// </summary>
        public string ETag { get; private set; }

        /// <summary>
        /// Gets the error that occurred during the copy operation, if any.
        /// </summary>
        public S3Exception Error { get; private set; }
        
        protected override void ProcessResponse()
        {
            if (Reader.Name == "Error")
                Error = S3Exception.FromErrorResponse(Reader, null);
            else if (Reader.Name == "CopyObjectResult")
            {
                if (Reader.IsEmptyElement)
                    throw new Exception("Expected a non-empty <CopyObjectResult> element.");

                Reader.ReadStartElement("CopyObjectResult");

                this.LastModified = Reader.ReadElementContentAsDateTime("LastModified", "");
                this.ETag = Reader.ReadElementContentAsString("ETag", "");

                Reader.ReadEndElement();
            }
            else
                throw new Exception("Unknown S3 XML response tag: " + Reader.Name);
        }
    }
}
