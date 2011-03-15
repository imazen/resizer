using System.Collections.Generic;
using System.Text;
using System.Web;

namespace LitS3
{
    /// <summary>
    /// A structure passed to ListObjectRequest which describes the range of items to search for.
    /// </summary>
    public class ListObjectsArgs
    {
        /// <summary>
        /// Limits the response to keys which begin with the indicated prefix.
        /// You can use prefixes to separate a bucket into different sets of keys 
        /// in a way similar to how a file system uses folders.
        /// </summary>
        public string Prefix { get; set; }

        /// <summary>
        /// Indicates where in the bucket to begin listing. The list will only include keys 
        /// that occur lexicographically after marker. This is convenient for pagination: To get 
        /// the next page of results use the last key of the current page as the marker.
        /// </summary>
        public string Marker { get; set; }

        /// <summary>
        /// Causes keys that contain the same string between the prefix and the first occurrence 
        /// of the delimiter to be rolled up into a single result element in the CommonPrefixes 
        /// collection. These rolled-up keys are not returned elsewhere in the response.
        /// </summary>
        public string Delimiter { get; set; }

        /// <summary>
        /// The maximum number of keys you'd like to see in the response body. The server might
        /// return fewer than this many keys, but will not return more.
        /// </summary>
        public int? MaxKeys { get; set; }

        internal string ToQueryString()
        {
            var builder = new StringBuilder();
            var sep = '?';

            if (!string.IsNullOrEmpty(Prefix))
            { builder.Append(sep).Append("prefix=").Append(HttpUtility.UrlEncode(Prefix)); sep = '&'; }

            if (!string.IsNullOrEmpty(Marker))
            { builder.Append(sep).Append("marker=").Append(HttpUtility.UrlEncode(Marker)); sep = '&'; }

            if (!string.IsNullOrEmpty(Delimiter))
            { builder.Append(sep).Append("delimiter=").Append(HttpUtility.UrlEncode(Delimiter)); sep = '&'; }

            if (MaxKeys.HasValue)
            { builder.Append(sep).Append("max-keys=").Append(MaxKeys.Value); sep = '&'; }

            return builder.ToString();
        }
    }

    /// <summary>
    /// An S3 request which searches for items in a bucket.
    /// </summary>
    public class ListObjectsRequest : S3Request<ListObjectsResponse>
    {
        public string BucketName { get; private set; }
        public ListObjectsArgs Args { get; private set; }

        public ListObjectsRequest(S3Service service, string bucketName, ListObjectsArgs args)
            : base(service, "GET", bucketName, null, args != null ? args.ToQueryString() : null)
        {
            this.BucketName = bucketName;
        }
    }

    /// <summary>
    /// An S3 response describing the requested contents of a bucket.
    /// </summary>
    public class ListObjectsResponse : S3Response
    {
        protected override void ProcessResponse()
        {
            // See http://docs.amazonwebservices.com/AmazonS3/2006-03-01/ListingKeysResponse.html

            Reader.ReadStartElement("ListBucketResult");

            // the response echoes back the request parameters, read those first in an assumed order
            this.BucketName = Reader.ReadElementContentAsString("Name", "");
            this.Prefix = Reader.ReadElementContentAsString("Prefix", "");
            this.Marker = Reader.ReadElementContentAsString("Marker", "");

            // this is optional
            if (Reader.Name == "NextMarker")
                this.NextMarker = Reader.ReadElementContentAsString("NextMarker", "");

            this.MaxKeys = Reader.ReadElementContentAsInt("MaxKeys", "");

            // this is optional
            if (Reader.Name == "Delimiter")
                this.Delimiter = Reader.ReadElementContentAsString("Delimiter", "");
            
            this.IsTruncated = Reader.ReadElementContentAsBoolean("IsTruncated", "");
        }

        /// <summary>
        /// Gets the bucket queried in the original request.
        /// </summary>
        public string BucketName { get; private set; }

        /// <summary>
        /// Gets the prefix specified in the original request.
        /// </summary>
        public string Prefix { get; private set; }

        /// <summary>
        /// Gets the marker specified in the original request.
        /// </summary>
        public string Marker { get; private set; }

        /// <summary>
        /// Gets the maximum number of keys specified in the original request.
        /// </summary>
        public int MaxKeys { get; private set; }

        /// <summary>
        /// Gets the delimiter specified in the original request.
        /// </summary>
        public string Delimiter { get; private set; }
        
        /// <summary>
        /// Gets whether the list of itmes was truncated by the server because too many matched
        /// or the number of items found was greater than the maximum keys specified in the request.
        /// </summary>
        public bool IsTruncated { get; private set; }

        /// <summary>
        /// Gets a marker you can use in a second ListObjectRequest to get the next range of items.
        /// This will be non-null only if IsTruncated is true and Delimiter is not null.
        /// </summary>
        public string NextMarker { get; private set; }

        /// <summary>
        /// Provides a forward-only reader for efficiently enumerating through the response
        /// list of objects and common prefixes.
        /// </summary>
        public IEnumerable<ListEntry> Entries
        {
            get
            {
                while (!Reader.IsEmptyElement && Reader.Name == "Contents")
                    yield return new ObjectEntry(Reader, Prefix, Delimiter);

                while (Reader.Name == "CommonPrefixes" && Reader.Read())
                    while (Reader.Name == "Prefix")
                        yield return new CommonPrefix(Reader, Prefix, Delimiter);
            }
        }
    }
}
