using System;
using System.IO;
using System.Net;
using System.Text;

namespace LitS3
{
    /// <summary>
    /// Creates a new bucket hosted by S3.
    /// </summary>
    public class CreateBucketRequest : S3Request<CreateBucketResponse>
    {
        const string EuropeConstraint = 
            "<CreateBucketConfiguration><LocationConstraint>EU</LocationConstraint></CreateBucketConfiguration>";

        /// <param name="createInEurope">
        /// True if you want to request that Amazon create this bucket in the Europe location. Otherwise,
        /// false to let Amazon decide.
        /// </param>
        public CreateBucketRequest(S3Service service, string bucketName, bool createInEurope)
            : base (service, "PUT", bucketName, null, null)
        {
            if (createInEurope)
                WebRequest.ContentLength = EuropeConstraint.Length;
        }

        bool EuropeRequested { get { return WebRequest.ContentLength > 0; } }

        void WriteEuropeConstraint(Stream stream)
        {
            var writer = new StreamWriter(stream, Encoding.ASCII);
            writer.Write(EuropeConstraint);
            writer.Flush();
        }

        public override CreateBucketResponse GetResponse()
        {
            AuthorizeIfNecessary(); // authorize before getting the request stream!

            // create in europe?
            if (EuropeRequested)
                using (Stream stream = WebRequest.GetRequestStream())
                    WriteEuropeConstraint(stream);

            return base.GetResponse();
        }

        public override IAsyncResult BeginGetResponse(AsyncCallback callback, object state)
        {
            throw new InvalidOperationException("BeginGetResponse() is not supported for this class yet.");
        }
    }

    /// <summary>
    /// Represents an S3 response for a created bucket.
    /// </summary>
    public class CreateBucketResponse : S3Response
    {
        /// <summary>
        /// The location of the created bucket, as returned by Amazon in the Location header.
        /// </summary>
        public string Location { get; private set; }

        protected override void ProcessResponse()
        {
            Location = WebResponse.Headers[HttpResponseHeader.Location];
        }
    }
}
