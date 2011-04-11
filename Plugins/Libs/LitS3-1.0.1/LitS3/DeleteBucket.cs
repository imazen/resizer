using System;
using System.Net;

namespace LitS3
{
    /// <summary>
    /// Deletes a bucket from S3. The bucket must be empty.
    /// </summary>
    public class DeleteBucketRequest : S3Request<DeleteBucketResponse>
    {
        public DeleteBucketRequest(S3Service service, string bucketName)
            : base(service, "DELETE", bucketName, null, null)
        {
        }
    }

    /// <summary>
    /// Represents an S3 response for a deleted bucket.
    /// </summary>
    public sealed class DeleteBucketResponse : S3Response
    {
        protected override void ProcessResponse()
        {
            if (WebResponse.StatusCode != HttpStatusCode.NoContent)
                throw new Exception("Unexpected status code: " + WebResponse.StatusCode);
        }
    }
}
