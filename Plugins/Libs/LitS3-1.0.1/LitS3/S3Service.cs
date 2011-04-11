using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace LitS3
{
    /// <summary>
    /// Describes how to connect to a particular S3 server.
    /// </summary>
    public class S3Service
    {
        string secretAccessKey;
        S3Authorizer authorizer;

        /// <summary>
        /// Reports progress for any operation that adds an object to a bucket.
        /// </summary>
        public event EventHandler<S3ProgressEventArgs> AddObjectProgress;

        /// <summary>
        /// Reports progress for any operation that gets an object from a bucket.
        /// </summary>
        public event EventHandler<S3ProgressEventArgs> GetObjectProgress;

        /// <summary>
        /// Fired before an S3Request operating against this service is authorized and sent
        /// out to the S3 server. This is a good opportunity to modify every S3Request 
        /// created internally by this class, for instance, to set the Proxy server or ServicePoint.
        /// </summary>
        public event EventHandler<S3RequestArgs> BeforeAuthorize;

        /// <summary>
        /// Gets or sets the hostname of the s3 server, usually "s3.amazonaws.com" unless you
        /// are using a 3rd party S3 implementation.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets whether to connect to the server using SSL. The default is true.
        /// </summary>
        public bool UseSsl { get; set; }

        /// <summary>
        /// Gets or sets whether to prepend the bucket name as a subdomain when accessing a bucket.
        /// This is Amazon's preferred method, however the property defaults to false for greater
        /// compatibility with existing buckets.
        /// </summary>
        public bool UseSubdomains { get; set; }

        /// <summary>
        /// Gets or sets a custom port to use to connect to the S3 server. The default is zero, which
        /// will let this class auto-select the port based on the UseSsl property.
        /// </summary>
        public int CustomPort { get; set; }

        /// <summary>
        /// Gets or sets the Amazon Access Key ID to use for authentication purposes.
        /// </summary>
        public string AccessKeyID { get; set; }

        /// <summary>
        /// Gets or sets the Amazon Secret Access Key to use for authentication purposes.
        /// </summary>
        public string SecretAccessKey
        {
            get { return secretAccessKey; }
            set
            {
                secretAccessKey = value;
                authorizer = !string.IsNullOrEmpty(value) ? new S3Authorizer(this) : null;
            }
        }

        /// <summary>
        /// Gets or sets the default delimiter to use when calling ListObjects(). The default is
        /// a forward-slash "/".
        /// </summary>
        public string DefaultDelimiter { get; set; }

        /// <summary>
        /// Creates a new S3Service with the default values.
        /// </summary>
        public S3Service()
        {
            this.Host = "s3.amazonaws.com";
            this.UseSsl = true;
            this.DefaultDelimiter = "/";
        }

        internal void AuthorizeRequest(S3Request request, HttpWebRequest webRequest, string bucketName)
        {
            if (BeforeAuthorize != null)
                BeforeAuthorize(this, new S3RequestArgs(request));

            // if you haven't set a secret access key, we can't authorize anything! maybe
            // you're talking to a mock S3 server. At any rate, the server will complain
            // if it expects the authorization.
            if (authorizer != null)
                authorizer.AuthorizeRequest(webRequest, bucketName);
        }

        #region Basic Operations

        /// <summary>
        /// Lists all buckets owned by you.
        /// </summary>
        public IList<Bucket> GetAllBuckets()
        {
            using (GetAllBucketsResponse response = new GetAllBucketsRequest(this).GetResponse())
                return response.Buckets.ToList();
        }

        /// <summary>
        /// Performs the given action on each of your buckets without loading the list of
        /// buckets completely into memory.
        /// </summary>
        public void ForEachBucket(Action<Bucket> action)
        {
            using (GetAllBucketsResponse response = new GetAllBucketsRequest(this).GetResponse())
                foreach (Bucket bucket in response.Buckets)
                    action(bucket);
        }

        /// <summary>
        /// Creates a bucket in the default storage location automatically determined by Amazon.
        /// </summary>
        /// <param name="bucketName">The name of the bucket, which will be checked against
        /// the BucketNameChecking.Strict requirements.</param>
        public void CreateBucket(string bucketName)
        {
            new CreateBucketRequest(this, bucketName, false).GetResponse().Close();
        }

        /// <summary>
        /// Creates a bucket in the Amazon Europe storage location.
        /// </summary>
        public void CreateBucketInEurope(string bucketName)
        {
            new CreateBucketRequest(this, bucketName, true).GetResponse().Close();
        }

        /// <summary>
        /// Queries S3 about the existance and ownership of the given bucket name.
        /// </summary>
        public BucketAccess QueryBucket(string bucketName)
        {
            try
            {
                // recommended technique from amazon: try and list contents of the bucket with 0 maxkeys
                var args = new ListObjectsArgs { MaxKeys = 0 };
                new ListObjectsRequest(this, bucketName, args).GetResponse().Close();

                return BucketAccess.Accessible;
            }
            catch (S3Exception exception)
            {
                switch (exception.ErrorCode)
                {
                    case S3ErrorCode.NoSuchBucket: return BucketAccess.NoSuchBucket;
                    case S3ErrorCode.AccessDenied: return BucketAccess.NotAccessible;
                    default: throw;
                }
            }
        }

        /// <summary>
        /// Returns true if the given object exists in the given bucket.
        /// </summary>
        public bool ObjectExists(string bucketName, string key)
        {
            var request = new GetObjectRequest(this, bucketName, key, true);

            // This is the recommended method from the S3 API docs.
            try
            {
                using (GetObjectResponse response = request.GetResponse())
                    return true;
            }
            catch (WebException exception)
            {
                var response = exception.Response as HttpWebResponse;
                if (response != null && response.StatusCode == HttpStatusCode.NotFound)
                    return false;
                else
                    throw;
            }
        }

        /// <summary>
        /// Queries S3 to determine whether the given bucket resides in the Europe location.
        /// </summary>
        public bool IsBucketInEurope(string bucketName)
        {
            var request = new GetBucketLocationRequest(this, bucketName);

            using (GetBucketLocationResponse response = request.GetResponse())
                return response.IsEurope;
        }

        /// <summary>
        /// Queries a bucket for a listing of objects it contains. Only objects with keys
        /// beginning with the given prefix will be returned. The DefaultDelimiter will
        /// be used. If you expect a large number of objects to be returned, consider using
        /// ListAllObjects().
        /// </summary>
        public IList<ListEntry> ListObjects(string bucketName, string prefix)
        {
            var args = new ListObjectsArgs { Prefix = prefix, Delimiter = DefaultDelimiter };
            var request = new ListObjectsRequest(this, bucketName, args);

            using (ListObjectsResponse response = request.GetResponse())
            {
                if (response.IsTruncated)
                    throw new Exception("The server truncated the list of items requested. Consider using the ListObjectsRequest class to query for large numbers of items.");

                return response.Entries.ToList();
            }
        }

        /// <summary>
        /// Queries a bucket for a listing of all objects it contains. The DefaultDelimiter will
        /// be used.
        /// </summary>
        public IEnumerable<ListEntry> ListAllObjects(string bucketName)
        {
            return ListAllObjects(bucketName, null, DefaultDelimiter);
        }

        /// <summary>
        /// Queries a bucket for a listing of objects it contains. Only objects with keys
        /// beginning with the given prefix will be returned. The DefaultDelimiter will
        /// be used.
        /// </summary>
        public IEnumerable<ListEntry> ListAllObjects(string bucketName, string prefix)
        {
            return ListAllObjects(bucketName, prefix, DefaultDelimiter);
        }

        /// <summary>
        /// Queries a bucket for a listing of objects it contains. Only objects with keys
        /// beginning with the given prefix will be returned.
        /// </summary>
        public IEnumerable<ListEntry> ListAllObjects(string bucketName, string prefix, string delimiter)
        {
            var args = new ListObjectsArgs
            {
                Prefix = prefix,
                Delimiter = delimiter
            };

            while (true)
            {
                var request = new ListObjectsRequest(this, bucketName, args);

                using (var response = request.GetResponse())
                {
                    ListEntry lastEntry = null;

                    foreach (var entry in response.Entries)
                    {
                        lastEntry = entry;
                        yield return entry;
                    }

                    if (response.IsTruncated)
                    {
                        // if you specified a delimiter, S3 is supposed to give us the marker
                        // name to use in order to get the next set of "stuff".
                        if (response.NextMarker != null)
                            args.Marker = response.NextMarker;
                        // if you didn't specify a delimiter, you won't get any CommonPrefixes,
                        // so we'll use the last ObjectEntry's key as the next delimiter.
                        else if (lastEntry is ObjectEntry)
                            args.Marker = (lastEntry as ObjectEntry).Key;
                        else
                            throw new Exception("S3 Server is misbehaving.");
                    }
                    else
                        break; // we're done!
                }
            }
        }

        /// <summary>
        /// Queries a bucket for a listing of objects it contains and performs the given
        /// action on each object. The DefaultDelimiter will be used.
        /// </summary>
        public void ForEachObject(string bucketName, Action<ListEntry> action)
        {
            ForEachObject(bucketName, null, action);
        }

        /// <summary>
        /// Queries a bucket for a listing of objects it contains and performs the given
        /// action on each object. Only objects with keys beginning with the given prefix 
        /// will be returned. The DefaultDelimiter will be used.
        /// </summary>
        public void ForEachObject(string bucketName, string prefix, Action<ListEntry> action)
        {
            foreach (ListEntry entry in ListAllObjects(bucketName, prefix))
                action(entry);
        }

        /// <summary>
        /// Deletes the bucket with the given name.
        /// </summary>
        public void DeleteBucket(string bucketName)
        {
            new DeleteBucketRequest(this, bucketName).GetResponse().Close();
        }

        /// <summary>
        /// Deletes the object in the specified bucket with the specified key.
        /// </summary>
        public void DeleteObject(string bucketName, string key)
        {
            new DeleteObjectRequest(this, bucketName, key).GetResponse().Close();
        }

        #endregion

        #region Public Uri construction

        /// <summary>
        /// This constructs a Uri suitable for accessing the given object in the given bucket.
        /// It is not authorized, so it will only work for objects with anonymous read access.
        /// This method itself does not communicate with S3 and will return immediately.
        /// </summary>
        public string GetUrl(string bucketName, string key)
        {
            var uriString = new StringBuilder();
            uriString.Append("http://");

            if (UseSubdomains)
                uriString.Append(bucketName).Append('.');

            uriString.Append(Host);

            if (CustomPort != 0)
                uriString.Append(':').Append(CustomPort);

            uriString.Append('/');

            if (!UseSubdomains)
                uriString.Append(bucketName).Append('/');

            // EscapeDataString allows keys to have any characters, including "+".
            uriString.Append(Uri.EscapeDataString(key));

            return uriString.ToString();
        }

        /// <summary>
        /// Creates a pre-authorized URI valid for performing a GET on the given S3 object
        /// in the given bucket. This is useful for constructing a URL to hand over to a 3rd party
        /// (such as a web browser). The Uri will automatically expire after the time given.
        /// This method itself does not communicate with S3 and will return immediately.
        /// </summary>
        /// <remarks>
        /// You might expect this method to return a System.Uri instead of a string. It turns out
        /// there is a tricky issue with constructing Uri objects from these pre-authenticated
        /// url strings: The Uri.ToString() method will convert a properly-encoded "+" character back
        /// into a raw "+", which is interpreted by Amazon S3 as a space (standard URI conventions).
        /// So the signature will be misread if you were to take the Uri.ToString() and feed
        /// it to a browser. So instead, we'll give you a properly escaped URL string which 
        /// will always work in a browser. If you want to, say, use it in a WebRequest instead, 
        /// it turns out that WebRequest will leave it escaped properly and everything will work.
        /// </remarks>
        public string GetAuthorizedUrl(string bucketName, string key, DateTime expires)
        {
            string authorization = authorizer.AuthorizeQueryString(bucketName, key, expires);
            
            var uriString = new StringBuilder(GetUrl(bucketName, key))
                .Append("?AWSAccessKeyId=").Append(AccessKeyID)
                .Append("&Expires=").Append(expires.SecondsSinceEpoch())
                .Append("&Signature=").Append(Uri.EscapeDataString(authorization));

            return uriString.ToString();
        }

        #endregion

        #region AddObject and overloads

        /// <summary>
        /// Adds an object to S3 by acquiring the upload stream then allowing the given
        /// function to handle writing data into it.
        /// </summary>
        public void AddObject(string bucketName, string key, long bytes, string contentType,
            CannedAcl acl, Action<Stream> action)
        {
            var request = new AddObjectRequest(this, bucketName, key)
            {
                ContentLength = bytes,
                CannedAcl = acl
            };

            if (contentType != null) // if specified
                request.ContentType = contentType;

            request.PerformWithRequestStream(action);
        }

        /// <summary>
        /// Adds an object to S3 by acquiring the upload stream then allowing the given
        /// function to handle writing data into it.
        /// </summary>
        public void AddObject(string bucketName, string key, long bytes, Action<Stream> action)
        {
            AddObject(bucketName, key, bytes, null, default(CannedAcl), action);
        }

        /// <summary>
        /// Adds an object to S3 by reading the specified amount of data from the given stream.
        /// </summary>
        public void AddObject(Stream inputStream, long bytes, string bucketName, string key, 
            string contentType, CannedAcl acl)
        {
            AddObject(bucketName, key, bytes, contentType, acl, stream =>
            {
                CopyStream(inputStream, stream, bytes,
                    CreateProgressCallback(bucketName, key, bytes, AddObjectProgress));
                stream.Flush();
            });
        }

        /// <summary>
        /// Adds an object to S3 by reading the specified amount of data from the given stream.
        /// </summary>
        public void AddObject(Stream inputStream, long bytes, string bucketName, string key)
        {
            AddObject(inputStream, bytes, bucketName, key, null, default(CannedAcl));
        }

        /// <summary>
        /// Adds an object to S3 by reading all the data in the given stream. The stream must support
        /// the Length property.
        /// </summary>
        public void AddObject(Stream inputStream, string bucketName, string key,
            string contentType, CannedAcl acl)
        {
            AddObject(inputStream, inputStream.Length, bucketName, key, contentType, acl);
        }

        /// <summary>
        /// Adds an object to S3 by reading all the data in the given stream. The stream must support
        /// the Length property.
        /// </summary>
        public void AddObject(Stream inputStream, string bucketName, string key)
        {
            AddObject(inputStream, inputStream.Length, bucketName, key, null, default(CannedAcl));
        }

        /// <summary>
        /// Uploads the contents of an existing local file to S3.
        /// </summary>
        public void AddObject(string inputFile, string bucketName, string key,
            string contentType, CannedAcl acl)
        {
            using (Stream inputStream = File.OpenRead(inputFile))
                AddObject(inputStream, inputStream.Length, bucketName, key, contentType, acl);
        }

        /// <summary>
        /// Uploads the contents of an existing local file to S3.
        /// </summary>
        public void AddObject(string inputFile, string bucketName, string key)
        {
            AddObject(inputFile, bucketName, key, null, default(CannedAcl));
        }

        /// <summary>
        /// Uploads the contents of a string to S3. This method is only appropriate for
        /// small objects and testing. The UTF-8 encoding will be used.
        /// </summary>
        public void AddObjectString(string contents, string bucketName, string key,
            string contentType, CannedAcl acl)
        {
            using (var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(contents)))
                AddObject(inputStream, bucketName, key, contentType, acl);
        }

        /// <summary>
        /// Uploads the contents of a string to S3. This method is only appropriate for
        /// small objects and testing. The UTF-8 encoding will be used.
        /// </summary>
        public void AddObjectString(string contents, string bucketName, string key)
        {
            AddObjectString(contents, bucketName, key, null, default(CannedAcl));
        }

        #endregion

        #region CopyObject

        /// <summary>
        /// Copies an object from one bucket to another with the given canned ACL.
        /// </summary>
        public void CopyObject(string sourceBucketName, string sourceKey,
            string destBucketName, string destKey, CannedAcl acl)
        {
            var request = new CopyObjectRequest(this, sourceBucketName, sourceKey,
                destBucketName, destKey) { CannedAcl = acl };

            CopyObjectResponse response = request.GetResponse();
            response.Close();

            if (response.Error != null)
                throw response.Error;
        }

        /// <summary>
        /// Copies an object from one bucket to another.
        /// </summary>
        public void CopyObject(string sourceBucketName, string sourceKey, 
            string destBucketName, string destKey)
        {
            var request = new CopyObjectRequest(this, sourceBucketName, sourceKey,
                destBucketName, destKey);

            CopyObjectResponse response = request.GetResponse();
            response.Close();

            if (response.Error != null)
                throw response.Error;
        }

        /// <summary>
        /// Copies an object within a bucket and assigns the given canned ACL.
        /// </summary>
        public void CopyObject(string bucketName, string sourceKey, string destKey, CannedAcl acl)
        {
            CopyObject(bucketName, sourceKey, bucketName, destKey, acl);
        }

        /// <summary>
        /// Copies an object within a bucket.
        /// </summary>
        public void CopyObject(string bucketName, string sourceKey, string destKey)
        {
            CopyObject(bucketName, sourceKey, bucketName, destKey);
        }

        #endregion

        #region GetObject and overloads

        /// <summary>
        /// Gets a data stream for an existing object in S3. It is your responsibility to close
        /// the Stream when you are finished.
        /// </summary>
        public Stream GetObjectStream(string bucketName, string key, 
            out long contentLength, out string contentType)
        {
            var request = new GetObjectRequest(this, bucketName, key);
            GetObjectResponse response = request.GetResponse();
            contentLength = response.ContentLength;
            contentType = response.ContentType;
            return response.GetResponseStream();
        }

        /// <summary>
        /// Gets a data stream for an existing object in S3. It is your responsibility to close
        /// the Stream when you are finished.
        /// </summary>
        public Stream GetObjectStream(string bucketName, string key)
        {
            long contentLength;
            string contentType;
            return GetObjectStream(bucketName, key, out contentLength, out contentType);
        }

        /// <summary>
        /// Gets an existing object in S3 and copies its data to the given Stream.
        /// </summary>
        public void GetObject(string bucketName, string key, Stream outputStream, 
            out long contentLength, out string contentType)
        {
            using (Stream objectStream = GetObjectStream(bucketName, key, out contentLength, out contentType))
                CopyStream(objectStream, outputStream, contentLength,
                    CreateProgressCallback(bucketName, key, contentLength, GetObjectProgress));
        }

        /// <summary>
        /// Gets an existing object in S3 and copies its data to the given Stream.
        /// </summary>
        public void GetObject(string bucketName, string key, Stream outputStream)
        {
            long contentLength;
            string contentType;
            GetObject(bucketName, key, outputStream, out contentLength, out contentType);
        }

        /// <summary>
        /// Downloads an existing object in S3 to the given local file path.
        /// </summary>
        public void GetObject(string bucketName, string key, string outputFile, out string contentType)
        {
            long contentLength;
            using (Stream outputStream = File.Create(outputFile))
                GetObject(bucketName, key, outputStream, out contentLength, out contentType);
        }

        /// <summary>
        /// Downloads an existing object in S3 to the given local file path.
        /// </summary>
        public void GetObject(string bucketName, string key, string outputFile)
        {
            string contentType;
            GetObject(bucketName, key, outputFile, out contentType);
        }

        /// <summary>
        /// Downloads an existing object in S3 and loads the entire contents into a string.
        /// This is only appropriate for very small objects and for testing.
        /// </summary>
        public string GetObjectString(string bucketName, string key, out string contentType)
        {
            using (var outputStream = new MemoryStream())
            {
                long contentLength;
                GetObject(bucketName, key, outputStream, out contentLength, out contentType);
                return Encoding.UTF8.GetString(outputStream.GetBuffer(), 0, (int)contentLength);
            }
        }

        /// <summary>
        /// Downloads an existing object in S3 and loads the entire contents into a string.
        /// This is only appropriate for very small objects and for testing.
        /// </summary>
        public string GetObjectString(string bucketName, string key)
        {
            string contentType;
            return GetObjectString(bucketName, key, out contentType);
        }

        #endregion

        #region CopyStream

        static void CopyStream(Stream source, Stream dest, long length, Action<long> progressCallback)
        {
            var buffer = new byte[8192];
        
            if (progressCallback != null)
                progressCallback(0);

            long totalBytesRead = 0;
            while (totalBytesRead < length) // reuse this local var
            {
                int bytesRead = source.Read(buffer, 0, buffer.Length);

                if (bytesRead > 0)
                    dest.Write(buffer, 0, bytesRead);
                else
                    throw new Exception("Unexpected end of stream while copying.");

                totalBytesRead += bytesRead;
                
                if (progressCallback != null) 
                    progressCallback(totalBytesRead);
            }
        }

        private Action<long> CreateProgressCallback(string bucketName, string key, long length,
            EventHandler<S3ProgressEventArgs> handler)
        {
            return handler != null
                 ? bytes => handler(this, new S3ProgressEventArgs(bucketName, key, bytes, length))
                 : (Action<long>) null;
        }

        #endregion
    }
}
