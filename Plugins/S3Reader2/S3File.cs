/* Copyright (c) 2011 Nathanael Jones. See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Web;
using System.Web.Hosting;
using System.Web.Caching;
using System.Security.Permissions;
using ImageResizer.Configuration;
using ImageResizer.Resizing;
using Amazon.S3.Model;
using ImageResizer.ExtensionMethods;
using Amazon.S3;

namespace ImageResizer.Plugins.S3Reader2 {

    public class S3File : VirtualFile, IVirtualFile, IVirtualFileWithModifiedDate, IVirtualFileSourceCacheKey {
        private string bucket;
        private string key;
        private S3VirtualPathProvider provider;

        private Nullable<bool> _exists = null;
        private Nullable<DateTime> _fileModifiedDate = null;

        /// <summary>
        /// Updates the exists and modified date
        /// </summary>
        public void UpdateMetadata() {
            //Try to pull it out of the .net cache
            string ckey = provider.VirtualFilesystemPrefix + "/" + bucket + "/" + key;
            Cache c = HttpContext.Current.Cache;
            object o = c.Get(ckey);
            if (o is DateTime) {
                _exists = true;
                _fileModifiedDate = (Nullable<DateTime>)o;
            } else if (o is bool) {
                _exists = false;
                _fileModifiedDate = null;
            } else {
                //Looks like we have to execute a head request
                var request = new GetObjectMetadataRequest(){ BucketName = bucket, Key = key};

                try {
                    GetObjectMetadataResponse response = provider.S3Client.GetObjectMetadata(request);
                    //Exists
                    _exists = true;
                    _fileModifiedDate = response.LastModified;
                    
                } catch (AmazonS3Exception s3e) {
                    if (s3e.StatusCode == System.Net.HttpStatusCode.NotFound || s3e.StatusCode == System.Net.HttpStatusCode.Forbidden) {
                        //Doesn't exist
                        _exists = false;
                        _fileModifiedDate = null;
                    } else throw;
                }
                //Now, save to the .net cache
                object obj = (_fileModifiedDate == null) ? (object)false : (object)_fileModifiedDate.Value;
                //If MetadataAbsoluteExpiration is MaxValue, use DateTime.MaxValue.
                c.Insert(ckey, obj, null, provider.MetadataAbsoluteExpiration == TimeSpan.MaxValue ? DateTime.MaxValue : DateTime.UtcNow.Add(provider.MetadataAbsoluteExpiration), provider.MetadataSlidingExpiration);
                
            }
        }

        public bool Exists {
            get {
                if (_exists == null && provider.FastMode) return true; //Assume it exists in fast mode.
                if (_exists == null) UpdateMetadata();
                return _exists.Value;
            }
        }

        public S3File(string virtualPath, S3VirtualPathProvider provider)
            : base(virtualPath) {
            this.provider = provider;
            //Must be inside virtual filesystem folder
            if (!provider.IsPathVirtual(virtualPath)) throw new ArgumentException("S3 file path must be located within " + provider.VirtualFilesystemPrefix);

            /////////// Parse path into bucket and key

            //Strip prefix
            String path = VirtualPath.Substring(provider.VirtualFilesystemPrefix.Length);

            //strip leading slashes
            path = path.TrimStart(new char[] { '/', '\\' });

            //Now execute filter!
            path = provider.FilterPath(path);

            //strip leading slashes again
            path = path.TrimStart(new char[] { '/', '\\' });


            int keyStartsAt = path.IndexOf('/');
            if (keyStartsAt < 0) throw new ArgumentException("S3 file path must specify a bucket" + path);
            //Get bucket
            bucket = path.Substring(0, keyStartsAt);
            //Get key
            key = path.Substring(keyStartsAt + 1).TrimStart(new char[] { '/', '\\' });
        }



        public override Stream Open() {
            //Synchronously download to memory stream
            try {
                var req = new Amazon.S3.Model.GetObjectRequest() { BucketName = bucket, Key = key };

                using (var s = provider.S3Client.GetObject(req)){
                    return StreamExtensions.CopyToMemoryStream(s.ResponseStream);
                }
            } catch (AmazonS3Exception se) {
                if (se.StatusCode == System.Net.HttpStatusCode.NotFound || "NoSuchKey".Equals(se.ErrorCode, StringComparison.OrdinalIgnoreCase)) throw new FileNotFoundException("Amazon S3 file not found", se);
                else if ( se.StatusCode == System.Net.HttpStatusCode.Forbidden || "AccessDenied".Equals(se.ErrorCode, StringComparison.OrdinalIgnoreCase)) throw new FileNotFoundException("Amazon S3 access denied - file may not exist", se);
                else throw;
            }
            return null;
        }

        public DateTime ModifiedDateUTC {
            get {
                if (_fileModifiedDate == null && provider.FastMode) return DateTime.MinValue; //In fast mode, Return flag value that means no info available.
                if (_fileModifiedDate == null) UpdateMetadata();
                return _fileModifiedDate.Value;
            }
        }


        public string GetCacheKey(bool includeModifiedDate) {
            return VirtualPath + (includeModifiedDate ? ("_" + ModifiedDateUTC.Ticks.ToString()) : "");
        }
    }
}

