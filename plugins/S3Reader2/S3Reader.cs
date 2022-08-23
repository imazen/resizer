/* Copyright (c) 2014 Imazen See license.txt for your rights. */

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using ImageResizer.Configuration;
using Imazen.Common.Issues;
using ImageResizer.Configuration.Xml;
using ImageResizer.ExtensionMethods;
using ImageResizer.Storage;

namespace ImageResizer.Plugins.S3Reader2
{
    public class S3Reader2 : BlobProviderBase, IMultiInstancePlugin, IRedactDiagnostics
    {
        private AmazonS3Config s3config = null;

        public S3Reader2() : base()
        {
            VirtualFilesystemPrefix = "~/s3/";
            s3config = new AmazonS3Config();
            UseHttps = false;
            AllowedBuckets = new string[] { };
        }


        public S3Reader2(NameValueCollection args) : this()
        {
            LoadConfiguration(args);
            UseHttps = args.Get("useHttps", args.Get("useSsl", UseHttps));
            Region = args.GetAsString("region", "us-east-1");

            SetAllowedBuckets(args.GetAsString("buckets", "")
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));

            if (!string.IsNullOrEmpty(args["accessKeyId"]) && !string.IsNullOrEmpty(args["secretAccessKey"]))
                S3Client = new AmazonS3Client(args["accessKeyId"], args["secretAccessKey"], s3config);
            else if (!string.IsNullOrEmpty(args["useProfile"]) && args["useProfile"] == "true")
                S3Client = new AmazonS3Client(s3config);
            else
                S3Client = new AmazonS3Client(null, s3config);
        }

        /// <summary>
        ///     If true, communications with S3 will happen over HTTPS.
        /// </summary>
        public bool UseHttps
        {
            get => !s3config.UseHttp;
            set => s3config.UseHttp = !value;
        }

        /// <summary>
        ///     Removes sensitive S3 access keys from the given XML configuration node.
        /// </summary>
        /// <param name="resizer"></param>
        /// <returns></returns>
        public new Node RedactFrom(Node resizer)
        {
            return base.RedactFrom(resizer)
                ?.RedactAttributes("plugins.add", new[] { "accessKeyId", "secretAccessKey" });
        }

        /// <summary>
        ///     Configure AWS access keys
        /// </summary>
        public AmazonS3Client S3Client { get; set; }

        /// <summary>
        ///     Get or set the AWS region by system name (like us-east-1)
        /// </summary>
        public string Region
        {
            get => s3config != null && s3config.RegionEndpoint != null ? s3config.RegionEndpoint.SystemName : null;
            set => s3config.RegionEndpoint = RegionEndpoint.GetBySystemName(value);
        }

        public delegate void RewriteBucketAndKeyPath(S3Reader2 sender, S3PathEventArgs e);

        /// <summary>
        ///     Important! You should handle this event and throw an exception if a bucket that you do not own is requested.
        ///     Otherwise other people's buckets could be accessed using your server.
        /// </summary>
        public event RewriteBucketAndKeyPath PreS3RequestFilter;

        /// <summary>
        ///     Executes the PreS3RequestFilter event and returns the result.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string FilterPath(string path)
        {
            var e = new S3PathEventArgs(path);
            if (PreS3RequestFilter != null) PreS3RequestFilter(this, e);
            return e.Path;
        }

        public S3PathEventArgs ParseAndFilterPath(string virtualPath)
        {
            var path = StripPrefix(virtualPath);

            var e = new S3PathEventArgs(path);
            if (PreS3RequestFilter != null) PreS3RequestFilter(this, e);

            if (string.IsNullOrEmpty(e.Bucket))
                throw new ArgumentException("S3 path must specify a bucket" + e.Path);
            if (string.IsNullOrEmpty(e.Key))
                throw new ArgumentException("S3 path must specify a key" + e.Path);
            return e;
        }

        public override async Task<IBlobMetadata> FetchMetadataAsync(string virtualPath,
            NameValueCollection queryString)
        {
            var path = ParseAndFilterPath(virtualPath);
            //Looks like we have to execute a head request
            var request = new GetObjectMetadataRequest() { BucketName = path.Bucket, Key = path.Key };

            try
            {
                var response = await S3Client.GetObjectMetadataAsync(request);

                return new BlobMetadata() { Exists = true, LastModifiedDateUtc = response.LastModified };
            }
            catch (AmazonS3Exception s3e)
            {
                if (s3e.StatusCode == HttpStatusCode.NotFound || s3e.StatusCode == HttpStatusCode.Forbidden)
                    return new BlobMetadata() { Exists = false };
                else throw;
            }
        }


        public override async Task<Stream> OpenAsync(string virtualPath, NameValueCollection queryString)
        {
            var path = ParseAndFilterPath(virtualPath);
            var time = Stopwatch.StartNew();
            long bytesFetched = 0;
            //Synchronously download to memory stream
            try
            {
                var req = new GetObjectRequest() { BucketName = path.Bucket, Key = path.Key };

                using (var s = await S3Client.GetObjectAsync(req))
                {
                    using (var stream = s.ResponseStream)
                    {
                        var copy = (Stream)await stream.CopyToMemoryStreamAsync();
                        bytesFetched = copy.Length;
                        return copy;
                    }
                }
            }
            catch (AmazonS3Exception se)
            {
                if (se.StatusCode == HttpStatusCode.NotFound ||
                    "NoSuchKey".Equals(se.ErrorCode, StringComparison.OrdinalIgnoreCase))
                    throw new FileNotFoundException("Amazon S3 file not found", se);
                else if (se.StatusCode == HttpStatusCode.Forbidden ||
                         "AccessDenied".Equals(se.ErrorCode, StringComparison.OrdinalIgnoreCase))
                    throw new FileNotFoundException("Amazon S3 access denied - file may not exist", se);
                else throw;
            }
            finally
            {
                time.Stop();
                ReportReadTicks(time.ElapsedTicks, bytesFetched);
            }
        }

        protected string[] AllowedBuckets { get; set; }

        public void SetAllowedBuckets(IEnumerable<string> buckets)
        {
            var a = buckets.ToArray();
            for (var i = 0; i < a.Length; i++)
                a[i] = a[i].Trim();
            AllowedBuckets = a;
        }

        public IEnumerable<string> GetAllowedBuckets()
        {
            return AllowedBuckets.ToArray();
        }


        public override IPlugin Install(Config c)
        {
            if (AllowedBuckets.Length < 1)
                c.configurationSectionIssues.AcceptIssue(new Issue("S3Reader",
                    "S3Reader cannot function without a list of permitted bucket names.",
                    "Please specify a comma-delimited list of buckets in the <add name='S3Reader' buckets='bucketa,bucketb' /> element.",
                    IssueSeverity.ConfigurationError));

            PreS3RequestFilter += S3Reader2_PreS3RequestFilter;

            base.Install(c);

            return this;
        }

        public override bool Uninstall(Config c)
        {
            PreS3RequestFilter -= S3Reader2_PreS3RequestFilter;
            return base.Uninstall(c);
        }

        private void S3Reader2_PreS3RequestFilter(S3Reader2 sender, S3PathEventArgs e)
        {
            e.AssertBucketMatches(AllowedBuckets);
        }
    }
}