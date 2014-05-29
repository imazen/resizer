/* Copyright (c) 2011 Wouter A. Alberts and Nathanael D. Jones. See license.txt for your rights. */
using System;
using System.Collections.Specialized;
using System.Web;
using System.Web.Hosting;
using ImageResizer.Util;
using System.Collections.Generic;
using ImageResizer.Configuration.Issues;
using System.Security;
using ImageResizer.Configuration.Xml;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage.Blob;
using ImageResizer.Storage;
using System.IO;

namespace ImageResizer.Plugins.AzureReader2 {

    public class AzureReader2Plugin : BlobProviderBase, IMultiInstancePlugin {

        public CloudBlobClient CloudBlobClient { get; set; }
        string blobStorageConnection;
        string blobStorageEndpoint;


        public bool RedirectToBlobIfUnmodified { get; set; }

        public AzureReader2Plugin(NameValueCollection args):base() {
            LoadConfiguration(args);
            blobStorageConnection = args["connectionstring"];
            blobStorageEndpoint = args["blobstorageendpoint"];
            if (string.IsNullOrEmpty(blobStorageEndpoint)) blobStorageEndpoint = args["endpoint"];
       }


        protected ICloudBlob GetBlobRef(string virtualPath)
        {
            string subPath = StripPrefix(virtualPath).Trim('/', '\\');
            string relativeBlobURL = string.Format("{0}/{1}", CloudBlobClient.BaseUri.OriginalString.TrimEnd('/', '\\'), subPath);

            return CloudBlobClient.GetBlobReferenceFromServer(new Uri(relativeBlobURL));
        }
        public override IBlobMetadata FetchMetadata(string virtualPath, NameValueCollection queryString)
        {
            
            try
            {
                var cloudBlob = GetBlobRef(virtualPath);

                cloudBlob.FetchAttributes();

                var meta = new BlobMetadata();
                meta.Exists = true; //Otherwise an exception would have happened at FetchAttributes
                var utc = cloudBlob.Properties.LastModified;
                if (utc != null)
                {
                    meta.LastModifiedDateUtc = utc.Value.UtcDateTime;
                }

                return meta;
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 404)
                {
                    return new BlobMetadata() { Exists = false };
                }
                else
                {
                    throw;
                }
            }
        }

        public override Stream Open(string virtualPath, NameValueCollection queryString)
        {

            MemoryStream ms = new MemoryStream(4096); // 4kb is a good starting point.

            // Synchronously download
            try
            {
                var cloudBlob = GetBlobRef(virtualPath);
                cloudBlob.DownloadToStream(ms);
            }
            catch (StorageException e)
            {
                // mb: 12/8/2012 - not sure of the correctness of these following lines
                // in other areas we just check e.RequestInformation.HttpStatusCode == 404 for a Not Found error
                // don't know what the errorcodes that will be returned
                if (e.RequestInformation.ExtendedErrorInformation.ErrorCode == "BlobNotFound")
                {
                    throw new FileNotFoundException("Azure blob file not found", e);
                }
                else if (e.RequestInformation.ExtendedErrorInformation.ErrorCode == "ContainerNotFound")
                {
                    throw new FileNotFoundException("Azure blob container not found", e);
                }
                else
                {
                    throw;
                }
            }

            ms.Seek(0, SeekOrigin.Begin); // Reset to beginning
            return ms;
        }

        public IPlugin Install(Configuration.Config c) {
            if (string.IsNullOrEmpty(blobStorageConnection))
                throw new InvalidOperationException("This plugin needs a connection string for the Azure blob storage.");

            if (string.IsNullOrEmpty(blobStorageEndpoint))
                throw new InvalidOperationException("This plugin needs a blob end point; the default will be [http|https]://myaccount.blob.core.windows.net.");

            if (!blobStorageEndpoint.EndsWith("/"))
                blobStorageEndpoint += "/";

            // Setup the connection to Windows Azure Storage

            // The 1.x Azure SDK offers a CloudStorageAccount.FromConfigurationSetting()
            // method that looks up the connection string from the fabric's configuration
            // and creates the CloudStorageAccount.  In 2.x, that method has disappeared
            // and we have to talk to the CloudConfigurationManager directly.
            var connectionString = CloudConfigurationManager.GetSetting(blobStorageConnection);

            // Earlier versions of AzureReader2 simply assumed/required that the
            // 'blobStorageConnection' value was the connection string itself, and
            // not a config key.  Therefore, we fall back to that behavior if the
            // configuration lookup fails.
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = blobStorageConnection;
            }

            var cloudStorageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
            // Register rewrite
            c.Pipeline.PostRewrite += Pipeline_PostRewrite;

            c.Plugins.add_plugin(this);

            return this;
        }

        /// <summary>
        /// In case there is no querystring attached to the file (thus no operations on the fly) we can
        /// redirect directly to the blob. This let us take advantage of the CDN (if configured).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="context"></param>
        /// <param name="e"></param>
        void Pipeline_PostRewrite(IHttpModule sender, HttpContext context, Configuration.IUrlEventArgs e) {
            string prefix = VirtualFilesystemPrefix;

            // Check if prefix is within virtual file system and if there is no querystring
            if (RedirectToBlobIfUnmodified && Belongs(e.VirtualPath) && e.QueryString.Count == 0) {

                // Strip prefix from virtual path; keep container and blob
                string relativeBlobURL = e.VirtualPath.Substring(prefix.Length).TrimStart('/', '\\');

                // Redirect to blob
                context.Response.Redirect(blobStorageEndpoint + relativeBlobURL);
            }
        }




    }
}
