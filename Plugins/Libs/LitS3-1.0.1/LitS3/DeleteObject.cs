using System;
using System.Net;

namespace LitS3
{
    /// <summary>
    /// Deletes an object in an S3 bucket.
    /// </summary>
    public class DeleteObjectRequest : S3Request<DeleteObjectResponse>
    {
        public DeleteObjectRequest(S3Service service, string bucketName, string key)
            : base(service, "DELETE", bucketName, key, null)
        {
        }
    }

    /// <summary>
    /// Represents the response for deleting an S3 object.
    /// </summary>
    public sealed class DeleteObjectResponse : S3Response
    {
        protected override void ProcessResponse()
        {
            if (WebResponse.StatusCode != HttpStatusCode.NoContent)
                throw new Exception("Unexpected status code: " + WebResponse.StatusCode);
        }
    }
}
