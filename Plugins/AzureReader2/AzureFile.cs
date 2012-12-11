/* Copyright (c) 2011 Wouter A. Alberts and Nathanael D. Jones. See license.txt for your rights. */
using System;
using System.IO;
using System.Web.Hosting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ImageResizer.Plugins.AzureReader2 {

    public class AzureFile : VirtualFile, IVirtualFile {

        protected readonly AzureVirtualPathProvider parent;

        public AzureFile(string blobName, AzureVirtualPathProvider parentProvider) : base(blobName) {
            parent = parentProvider;
        }
        /// <summary>
        /// Attempts to download the blob into a MemoryStream instance and return it. Throws a FileNotFoundException if the blob doesn't exist.
        /// </summary>
        /// <returns></returns>
        public override System.IO.Stream Open() {
            // Prefix already stripped from virtual path

            // Get a reference to the blob
            // mb: 12/8/2012 - the path needs to be a uri now, so combining blobclient baseuri with the virtualpath
            Uri blobUri = new Uri(string.Format("{0}/{1}", parent.CloudBlobClient.BaseUri.OriginalString, VirtualPath));
            ICloudBlob cloudBlob = parent.CloudBlobClient.GetBlobReferenceFromServer(blobUri);

            MemoryStream ms = new MemoryStream(4096); // 4kb is a good starting point.

            // Synchronously download
            try {
                cloudBlob.DownloadToStream(ms);
            }
            catch (StorageException e) {
                // mb: 12/8/2012 - not sure of the correctness of these following lines
                // in other areas we just check e.RequestInformation.HttpStatusCode == 404 for a Not Found error
                // don't know what the errorcodes that will be returned
                if (e.RequestInformation.ExtendedErrorInformation.ErrorCode == "BlobNotFound") {
                    throw new FileNotFoundException("Azure blob file not found", e);
                }
                else if (e.RequestInformation.ExtendedErrorInformation.ErrorCode == "ContainerNotFound")
                {
                    throw new FileNotFoundException("Azure blob container not found", e);
                }
                else {
                    throw;
                }
            }

            ms.Seek(0, SeekOrigin.Begin); // Reset to beginning
            return ms;
        }
    }
}
