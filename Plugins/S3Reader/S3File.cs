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
using LitS3;
using ImageResizer.Configuration;
using ImageResizer.Resizing;

namespace ImageResizer.Plugins.S3Reader {

    public class S3File : VirtualFile, IVirtualFile, IVirtualFileWithModifiedDate {
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
                var request = new GetObjectRequest(provider.Service, bucket, key, true);
                try {
                    using (GetObjectResponse response = request.GetResponse()) {
                        //Exists.
                        _exists = true;
                        _fileModifiedDate = response.LastModified;
                    }
                } catch (System.Net.WebException exception) {
                    var response = exception.Response as System.Net.HttpWebResponse;
                    if (response != null && response.StatusCode == System.Net.HttpStatusCode.NotFound) {
                        //Doesn't exist
                        _exists = false;
                        _fileModifiedDate = null;
                    } else
                        throw;
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
            MemoryStream ms = new MemoryStream(4096); //4kb is a good starting point.
            //Synchronously download
            try {
                provider.Service.GetObject(bucket, key, ms);
            } catch (S3Exception se) {
               // if (HttpContext.Current != null && HttpContext.Current.Items[Config.Current.Pipeline.ResponseArgsKey]
                if (se.ErrorCode == S3ErrorCode.NoSuchKey) throw new FileNotFoundException("Amazon S3 file not found", se);
                else if (se.ErrorCode == S3ErrorCode.AccessDenied) throw new FileNotFoundException("Amazon S3 access denied - file may not exist", se);
                else throw se;
                    //LitS3.S3ErrorCode.PermanentRedirect
            }
            ms.Seek(0, SeekOrigin.Begin); //Reset to beginning
            return ms;
        }

        public DateTime ModifiedDateUTC {
            get {
                if (_fileModifiedDate == null && provider.FastMode) return DateTime.MinValue; //In fast mode, Return flag value that means no info available.
                if (_fileModifiedDate == null) UpdateMetadata();
                return _fileModifiedDate.Value;
            }
        }

    }
}

