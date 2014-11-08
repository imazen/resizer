/* Copyright (c) 2014 Imazen See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Hosting;
using System.Collections.Specialized;
using ImageResizer.Configuration.Issues;
using System.Security;
using ImageResizer.Util;
using System.Web;
using ImageResizer.ExtensionMethods;
using Amazon.S3;
using ImageResizer.Configuration.Xml;
using ImageResizer.Storage;
using Amazon.S3.Model;
using System.IO;
using System.Threading.Tasks;

namespace ImageResizer.Plugins.S3Reader2 {
    public class S3Reader2 : BlobProviderBase, IMultiInstancePlugin, IRedactDiagnostics {

        AmazonS3Config s3config = null;
        public S3Reader2():base()
        {
            VirtualFilesystemPrefix = "~/s3/";
            s3config = new AmazonS3Config();
            UseHttps = false;
            AllowedBuckets = new string[]{};
        }

        
        public S3Reader2(NameValueCollection args ):this() {
            LoadConfiguration(args);
            UseHttps = args.Get("useHttps", args.Get("useSsl", UseHttps));
            Region = args.GetAsString("region", "us-east-1");

            SetAllowedBuckets(args.GetAsString("buckets","").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));

            if (!string.IsNullOrEmpty(args["accessKeyId"]) && !string.IsNullOrEmpty(args["secretAccessKey"])) {
                S3Client = new AmazonS3Client(args["accessKeyId"], args["secretAccessKey"], s3config);
            } else {
                S3Client = new AmazonS3Client(null, s3config);
            }
        }

        /// <summary>
        /// If true, communications with S3 will happen over HTTPS.
        /// </summary>
        public bool UseHttps { get { return !s3config.UseHttp; } set { s3config.UseHttp = !value; } }

        /// <summary>
        /// Removes sensitive S3 access keys from the given XML configuration node.
        /// </summary>
        /// <param name="resizer"></param>
        /// <returns></returns>
        public Configuration.Xml.Node RedactFrom(Node resizer) {
            resizer = base.RedactFrom(resizer);
            foreach (Node n in resizer.queryUncached("plugins.add")) {
                if (n.Attrs["accessKeyId"] != null) n.Attrs.Set("accessKeyId", "[redacted]");
                if (n.Attrs["secretAccessKey"] != null) n.Attrs.Set("secretAccessKey", "[redacted]");
            }
            return resizer;
        }
    
        /// <summary>
        /// Configure AWS access keys
        /// </summary>
        public AmazonS3Client S3Client { get; set; }

        /// <summary>
        /// Get or set the AWS region by system name (like us-east-1)
        /// </summary>
        public string Region
        {
            get { return this.s3config != null && this.s3config.RegionEndpoint != null ? this.s3config.RegionEndpoint.SystemName : null; }
            set
            {
                this.s3config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(value);
            }
        }

        public delegate void RewriteBucketAndKeyPath(S3Reader2 sender, S3PathEventArgs e);

        /// <summary>
        /// Important! You should handle this event and throw an exception if a bucket that you do not own is requested. Otherwise other people's buckets could be accessed using your server.
        /// </summary>
        public event RewriteBucketAndKeyPath PreS3RequestFilter;

        /// <summary>
        /// Execites the PreS3RequestFilter event and returns the result.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string FilterPath(string path)
        {
            S3PathEventArgs e = new S3PathEventArgs(path);
            if (PreS3RequestFilter != null) PreS3RequestFilter(this, e);
            return e.Path;
        }

        public S3PathEventArgs ParseAndFilterPath(string virtualPath){
            var path = StripPrefix(virtualPath);

            var e = new S3PathEventArgs(path);
            if (PreS3RequestFilter != null) PreS3RequestFilter(this, e);

            if (string.IsNullOrEmpty(e.Bucket))
                throw new ArgumentException("S3 path must specify a bucket" + e.Path);
             if (string.IsNullOrEmpty(e.Key))
                throw new ArgumentException("S3 path must specify a key" + e.Path);
            return e;
        }

        public override async Task<IBlobMetadata> FetchMetadataAsync(string virtualPath, NameValueCollection queryString)
        {
            var path = ParseAndFilterPath(virtualPath);
            //Looks like we have to execute a head request
            var request = new GetObjectMetadataRequest() { BucketName = path.Bucket, Key = path.Key };

            try
            {
                GetObjectMetadataResponse response = await S3Client.GetObjectMetadataAsync(request);

                return new BlobMetadata(){ Exists = true, LastModifiedDateUtc = response.LastModified};
            }
            catch (AmazonS3Exception s3e)
            {
                if (s3e.StatusCode == System.Net.HttpStatusCode.NotFound || s3e.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    return new BlobMetadata(){Exists = false};
                }
                else throw;
            }     
        }



        public override async Task<Stream> OpenAsync(string virtualPath, NameValueCollection queryString)
        {
            var path = ParseAndFilterPath(virtualPath);
            //Synchronously download to memory stream
            try {
                var req = new Amazon.S3.Model.GetObjectRequest() { BucketName = path.Bucket, Key = path.Key };

                using (var s = await S3Client.GetObjectAsync(req)){
                    return (Stream) await s.ResponseStream.CopyToMemoryStreamAsync();
                }
            } catch (AmazonS3Exception se) {
                if (se.StatusCode == System.Net.HttpStatusCode.NotFound || "NoSuchKey".Equals(se.ErrorCode, StringComparison.OrdinalIgnoreCase)) throw new FileNotFoundException("Amazon S3 file not found", se);
                else if ( se.StatusCode == System.Net.HttpStatusCode.Forbidden || "AccessDenied".Equals(se.ErrorCode, StringComparison.OrdinalIgnoreCase)) throw new FileNotFoundException("Amazon S3 access denied - file may not exist", se);
                else throw;
            }
        }

        protected string[] AllowedBuckets{get;set;}

        public void SetAllowedBuckets(IEnumerable<string> buckets)
        {   
            var a = buckets.ToArray();
            for (int i = 0; i < a.Length; i++)
                a[i] = a[i].Trim();
            AllowedBuckets = a;
        }

        public IEnumerable<string> GetAllowedBuckets()
        {
            return AllowedBuckets.ToArray();
        }


        
        public override IPlugin Install(Configuration.Config c) {

            if (AllowedBuckets.Length < 1)
                c.configurationSectionIssues.AcceptIssue(new Issue("S3Reader", "S3Reader cannot function without a list of permitted bucket names.",
                "Please specify a comma-delimited list of buckets in the <add name='S3Reader' buckets='bucketa,bucketb' /> element.",
                 IssueSeverity.ConfigurationError));

            this.PreS3RequestFilter += S3Reader2_PreS3RequestFilter;

            base.Install(c);
            
            return this;

        }

        public override bool Uninstall(Configuration.Config c)
        {
            this.PreS3RequestFilter -= S3Reader2_PreS3RequestFilter;
            return base.Uninstall(c);
        }

        void S3Reader2_PreS3RequestFilter(S3Reader2 sender, S3PathEventArgs e)
        {
            e.AssertBucketMatches(AllowedBuckets);
        }


    }
}
