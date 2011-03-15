
namespace LitS3
{
    /// <summary>
    /// Queries S3 for the hosted location of a bucket.
    /// </summary>
    public class GetBucketLocationRequest : S3Request<GetBucketLocationResponse>
    {
        public GetBucketLocationRequest(S3Service service, string bucketName)
            : base(service, "GET", bucketName, null, "?location")
        {
        }
    }

    /// <summary>
    /// The S3 response for the hosted location of a bucket.
    /// </summary>
    public sealed class GetBucketLocationResponse : S3Response
    {
        /// <summary>
        /// Gets true if the bucket was created in the Europe location.
        /// </summary>
        public bool IsEurope { get; private set; }

        protected override void ProcessResponse()
        {
            string location = Reader.ReadElementContentAsString("LocationConstraint", "");

            if (location == "EU")
                IsEurope = true;
        }
    }
}
