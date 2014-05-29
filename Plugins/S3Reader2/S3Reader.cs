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

namespace ImageResizer.Plugins.S3Reader2 {
    public class S3Reader2 : BlobProviderBase, IMultiInstancePlugin, IRedactDiagnostics {

        string buckets;
        AmazonS3Config s3config = null;
        public S3Reader2(NameValueCollection args ) {

            VirtualFilesystemPrefix = "~/s3/";
            s3config = new AmazonS3Config();

            buckets = args["buckets"];
            Region = args["region"] ?? "us-east-1";


            s3config.UseHttp = !args.Get("useSsl", false);

            if (!string.IsNullOrEmpty(args["accessKeyId"]) && !string.IsNullOrEmpty(args["secretAccessKey"])) {
                S3Client = new AmazonS3Client(args["accessKeyId"], args["secretAccessKey"], s3config);
            } else {

                S3Client = new AmazonS3Client(null, s3config);
            }
      
        }


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

        public override IBlobMetadata FetchMetadata(string virtualPath, NameValueCollection queryString)
        {
            var path = ParseAndFilterPath(virtualPath);
            //Looks like we have to execute a head request
            var request = new GetObjectMetadataRequest() { BucketName = path.Bucket, Key = path.Key };

            try
            {
                GetObjectMetadataResponse response = S3Client.GetObjectMetadata(request);

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



        public override Stream Open(string virtualPath, NameValueCollection queryString)
        {
            var path = ParseAndFilterPath(virtualPath);
            //Synchronously download to memory stream
            try {
                var req = new Amazon.S3.Model.GetObjectRequest() { BucketName = path.Bucket, Key = path.Key };

                using (var s = S3Client.GetObject(req)){
                    return s.ResponseStream.CopyToMemoryStream();
                }
            } catch (AmazonS3Exception se) {
                if (se.StatusCode == System.Net.HttpStatusCode.NotFound || "NoSuchKey".Equals(se.ErrorCode, StringComparison.OrdinalIgnoreCase)) throw new FileNotFoundException("Amazon S3 file not found", se);
                else if ( se.StatusCode == System.Net.HttpStatusCode.Forbidden || "AccessDenied".Equals(se.ErrorCode, StringComparison.OrdinalIgnoreCase)) throw new FileNotFoundException("Amazon S3 access denied - file may not exist", se);
                else throw;
            }
            return null;
        }


        private string[] bucketArray = null;
        public IPlugin Install(Configuration.Config c) {

            if (!string.IsNullOrEmpty(buckets)) bucketArray = buckets.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            else c.configurationSectionIssues.AcceptIssue(new Issue("S3Reader", "S3Reader cannot function without a list of permitted bucket names.",
                "Please specify a comma-delimited list of buckets in the <add name='S3Reader' buckets='bucketa,bucketb' /> element.",
                 IssueSeverity.ConfigurationError));

            for (int i = 0; i < bucketArray.Length; i++)
                bucketArray[i] = bucketArray[i].Trim();


            this.PreS3RequestFilter += S3Reader2_PreS3RequestFilter;
           
            c.Plugins.Install(this);
            
            return this;

        }

        void S3Reader2_PreS3RequestFilter(S3Reader2 sender, S3PathEventArgs e)
        {
            if (bucketArray == null) e.ThrowException();
            e.AssertBucketMatches(bucketArray);
        }


    }
}
