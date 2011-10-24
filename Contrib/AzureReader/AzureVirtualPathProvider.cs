/* Copyright (c) 2011 Wouter A. Alberts and Nathanael D. Jones. See license.txt for your rights. */
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
    public class AzureVirtualPathProvider : VirtualPathProvider, IVirtualImageProvider {

        private string _virtualFilesystemPrefix = PathUtils.ResolveAppRelative("~/azure/");
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
                //Default to app-relative
                if (!value.StartsWith("/") && !value.StartsWith("~")) value = "~/" + value;
                if (!value.EndsWith("/")) value += "/";
                
                _virtualFilesystemPrefix = value != null ? PathUtils.ResolveAppRelative(value) : value;
            }
        }

        private bool _lazyExistenceCheck = false;
        /// <summary>
        /// If true, 
        /// </summary>
        public bool LazyExistenceCheck {
            get { return _lazyExistenceCheck; }
            set { _lazyExistenceCheck = value; }
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
        public bool IsPathVirtual(string virtualPath){
            return (virtualPath.StartsWith(VirtualFilesystemPrefix, StringComparison.OrdinalIgnoreCase));
        }
        /// <summary>
        /// Internal usage only
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public override bool FileExists(string virtualPath) {
            if (FileExists(virtualPath, null))
                return true;
            else {
                return Previous.FileExists(virtualPath);
            }
        }

        /// <summary>
        /// For internal use only
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public override VirtualFile GetFile(string virtualPath) {
            VirtualFile vf = (VirtualFile)GetFile(virtualPath, null);
            return (vf == null) ? Previous.GetFile(virtualPath) : vf;
        }

        /// <summary>
        /// Returns true if the specified file is within the azure virtual directory prefix, and if it exists. Returns true even if the file doesn't exist when LazyExistenceCheck=true
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public bool FileExists(string virtualPath, System.Collections.Specialized.NameValueCollection queryString) {
            if (IsPathVirtual(virtualPath)) {
                if (LazyExistenceCheck) return true;

                // Strip prefix from virtual path; keep container and blob
                string relativeBlobURL = virtualPath.Substring(VirtualFilesystemPrefix.Length).Trim('/', '\\');

                // Get a reference to the blob
                CloudBlob cloudBlob = CloudBlobClient.GetBlobReference(relativeBlobURL);

                try {
                    cloudBlob.FetchAttributes();
                    return true;
                } catch (StorageClientException e) {
                    if (e.ErrorCode == StorageErrorCode.ResourceNotFound) {
                        return false;
                    } else {
                        throw;
                    }
                }
            }
            return false;
        }

        public IVirtualFile GetFile(string virtualPath, System.Collections.Specialized.NameValueCollection queryString) {
            if (IsPathVirtual(virtualPath)) {
                // Strip prefix from virtual path; keep container and blob
                string relativeBlobURL = virtualPath.Substring(VirtualFilesystemPrefix.Length).Trim('/', '\\');


                try {
                    if (!LazyExistenceCheck) {
                        // Get a reference to the blob
                        CloudBlob cloudBlob = CloudBlobClient.GetBlobReference(relativeBlobURL);
                        cloudBlob.FetchAttributes();
                    }
                    return new AzureFile(relativeBlobURL, this);
                } catch (StorageClientException e) {
                    if (e.ErrorCode == StorageErrorCode.ResourceNotFound) {
                        return null;
                    } else {
                        throw;
                    }
                }
            }
            return null;
        }
    }
}
