/* Copyright (c) 2011 Wouter A. Alberts and Nathanael D. Jones. See license.txt for your rights. */
using System;
using System.IO;
using System.Web.Hosting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace ImageResizer.Plugins.AzureReader {

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
            CloudBlob cloudBlob = parent.CloudBlobClient.GetBlobReference(VirtualPath);

            MemoryStream ms = new MemoryStream(4096); // 4kb is a good starting point.

            // Synchronously download
            try {
                cloudBlob.DownloadToStream(ms);
            }
            catch (StorageClientException e) {
                if (e.ErrorCode == StorageErrorCode.ResourceNotFound) {
                    throw new FileNotFoundException("Azure blob file not found", e);
                }
                else if (e.ErrorCode == StorageErrorCode.ContainerNotFound) {
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
