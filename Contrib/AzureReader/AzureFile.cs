using System;
using System.IO;
using System.Web.Hosting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace ImageResizer.Plugins.AzureReader {

    class AzureFile : VirtualFile {

        protected readonly AzureVirtualPathProvider parent;

        public AzureFile(string blobName, AzureVirtualPathProvider parentProvider) : base(blobName) {
            parent = parentProvider;
        }

        public override System.IO.Stream Open() {
            // Strip prefix from virtual path; keep container and blob
            string relativeBlobURL = VirtualPath.Substring(parent.VirtualFilesystemPrefix.Length).Trim('/', '\\');

            // Get a reference to the blob
            CloudBlob cloudBlob = parent.CloudBlobClient.GetBlobReference(relativeBlobURL);

            MemoryStream ms = new MemoryStream(4096); // 4kb is a good starting point.

            // Synchronously download
            try {
                // Perhaps this would be a future optimization?
                // return cloudBlob.OpenRead();

                cloudBlob.DownloadToStream(ms);
            }
            catch (StorageClientException e) {
                if (e.ErrorCode == StorageErrorCode.ResourceNotFound) {
                    throw new FileNotFoundException("Azure blob file not found", e);
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
