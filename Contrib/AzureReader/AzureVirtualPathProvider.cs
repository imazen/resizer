using System;
using System.Security.Permissions;
using System.Web;
using System.Web.Hosting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using ImageResizer.Util;

namespace ImageResizer.Plugins.AzureReader {

    [AspNetHostingPermission(SecurityAction.Demand, Level = AspNetHostingPermissionLevel.Medium)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.High)]
    class AzureVirtualPathProvider : VirtualPathProvider {

        private string _virtualFilesystemPrefix = "~/azure/";
        private CloudBlobClient _cloudBlobClient = null;

        /// <summary>
        /// Requests starting with this path will be handled by this virtual path provider.
        /// Can be in app-relative form: "~/azure/". Will be translated to domain-relative form.
        /// </summary>
        public string VirtualFilesystemPrefix {
            get {
                return _virtualFilesystemPrefix;
            }
            set {
                _virtualFilesystemPrefix = value != null ? PathUtils.ResolveAppRelative(value) : value;
            }
        }

        public CloudBlobClient CloudBlobClient {
            get {
                return _cloudBlobClient;
            }
            set {
                _cloudBlobClient = value;
            }
        }
        
        public AzureVirtualPathProvider(string blobStorageConnection) {
            // Setup the connection to Windows Azure Storage
            var cloudStorageAccount = CloudStorageAccount.FromConfigurationSetting(blobStorageConnection);
            CloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
        }

        /// <summary>
        /// Determines whether a specified virtual path is within the virtual file system.
        /// </summary>
        /// <param name="virtualPath">An absolute virtual path.</param>
        /// <returns>
        /// True if the virtual path is within the virtual file sytem; otherwise, false.
        /// </returns>
        public bool IsPathVirtual(string virtualPath) {
            return (virtualPath.StartsWith(VirtualFilesystemPrefix, StringComparison.InvariantCultureIgnoreCase));
        }

        public override bool FileExists(string virtualPath) {
            if (IsPathVirtual(virtualPath)) {
                // Strip prefix from virtual path; keep container and blob
                string relativeBlobURL = virtualPath.Substring(VirtualFilesystemPrefix.Length).Trim('/', '\\');
                
                // Get a reference to the blob
                CloudBlob cloudBlob = CloudBlobClient.GetBlobReference(relativeBlobURL);

                try {
                    cloudBlob.FetchAttributes();
                    return true;
                }
                catch (StorageClientException e) {
                    if (e.ErrorCode == StorageErrorCode.ResourceNotFound) {
                        return false;
                    }
                    else {
                        throw;
                    }
                }
            }
            else {
                return Previous.FileExists(virtualPath);
            }
        }

        public override VirtualFile GetFile(string virtualPath) {
            if (IsPathVirtual(virtualPath)) {
                // Strip prefix from virtual path; keep container and blob
                string relativeBlobURL = virtualPath.Substring(VirtualFilesystemPrefix.Length).Trim('/', '\\');

                // Get a reference to the blob
                CloudBlob cloudBlob = CloudBlobClient.GetBlobReference(relativeBlobURL);

                try {
                    cloudBlob.FetchAttributes();
                    return new AzureFile(relativeBlobURL, this);
                }
                catch (StorageClientException e) {
                    if (e.ErrorCode == StorageErrorCode.ResourceNotFound) {
                        return Previous.GetFile(virtualPath);
                    }
                    else {
                        throw;
                    }
                }
            }
            else {
                return Previous.GetFile(virtualPath);
            }
        }        
    }
}
