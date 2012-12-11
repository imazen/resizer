/* Copyright (c) 2011 Wouter A. Alberts and Nathanael D. Jones. See license.txt for your rights. */
using System;
using System.Security.Permissions;
using System.Web;
using System.Web.Hosting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using ImageResizer.Util;

namespace ImageResizer.Plugins.AzureReader2 {

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
                if (!value.EndsWith("/")) value += "/";
                _virtualFilesystemPrefix = value != null ? PathUtils.ResolveAppRelativeAssumeAppRelative(value) : value;
                
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
            // mb:12/8/2012 - this needs to be the actual connection string not the config file connection string name
            var cloudStorageAccount = CloudStorageAccount.Parse(blobStorageConnection);
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
                // mb:12/8/2012 - need to prepend the blob client base uri to the url
                string relativeBlobURL = string.Format("{0}/{1}",CloudBlobClient.BaseUri.OriginalString, virtualPath.Substring(VirtualFilesystemPrefix.Length).Trim('/', '\\'));

                // Get a reference to the blob
                // mb:12/8/2012 - this call now must be a uri
                ICloudBlob cloudBlob = CloudBlobClient.GetBlobReferenceFromServer(new Uri(relativeBlobURL));

                try {
                    cloudBlob.FetchAttributes();
                    return true;
                } catch (StorageException e) {
                    if (e.RequestInformation.HttpStatusCode == 404) {
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
                        // mb: 12/8/2012 - creating uri here to keep the above relativeBlobURL as is to create & return an AzureFile
                        Uri relativeBlobUri = new Uri(string.Format("{0}/{1}", CloudBlobClient.BaseUri.OriginalString, relativeBlobURL));
                        ICloudBlob cloudBlob = CloudBlobClient.GetBlobReferenceFromServer(relativeBlobUri);
                        cloudBlob.FetchAttributes();
                    }
                    return new AzureFile(relativeBlobURL, this);
                } catch (StorageException e) {
                    if (e.RequestInformation.HttpStatusCode == 404) {
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
