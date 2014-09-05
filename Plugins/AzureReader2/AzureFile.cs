/* Copyright (c) 2011 Wouter A. Alberts and Nathanael D. Jones. See license.txt for your rights. */
using System;
using System.IO;
using System.Web.Hosting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ImageResizer.Plugins.AzureReader2
{

    /// <summary>
    /// Represents a virtual file that reads the file from Microsoft Azure Blob Storage.
    /// </summary>
    public class AzureFile : VirtualFile, IVirtualFile
    {
        private readonly AzureVirtualPathProvider parent;

        public AzureFile(string blobName, AzureVirtualPathProvider parentProvider)
            : base(blobName)
        {
            parent = parentProvider;
        }
        /// <summary>
        /// Attempts to download the blob into a MemoryStream instance and return it. Throws a FileNotFoundException if the blob or container doesn't exist.
        /// Can also throw other exceptions from the StorageClient
        /// </summary>
        public override Stream Open()
        {
            MemoryStream ms = new MemoryStream(4096); // 4kb is a good starting point.

            try
            {
                Uri blobUri = new Uri(string.Format("{0}/{1}", parent.CloudBlobClient.BaseUri.OriginalString.TrimEnd('/', '\\'), VirtualPath));
                ICloudBlob cloudBlob = parent.CloudBlobClient.GetBlobReferenceFromServer(blobUri);

                cloudBlob.DownloadToStream(ms);
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.ExtendedErrorInformation.ErrorCode == "BlobNotFound")
                {
                    throw new FileNotFoundException("Azure blob file not found", e);
                }
                if (e.RequestInformation.ExtendedErrorInformation.ErrorCode == "ContainerNotFound")
                {
                    throw new FileNotFoundException("Azure blob container not found", e);
                }
                throw;
            }

            ms.Seek(0, SeekOrigin.Begin); // Reset to beginning
            return ms;
        }
    }
}
