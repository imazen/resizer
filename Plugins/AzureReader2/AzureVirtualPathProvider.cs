/* Copyright (c) 2011 Wouter A. Alberts and Nathanael D. Jones. See license.txt for your rights. */
using System;
using System.Collections.Specialized;
using System.Web.Hosting;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using ImageResizer.Util;

namespace ImageResizer.Plugins.AzureReader2
{
    /// <summary>
    /// A virtual path provider for files stored on Microsoft Azure Blob Storage.
    /// </summary>
    public class AzureVirtualPathProvider : VirtualPathProvider, IVirtualImageProvider
    {

        private string _virtualFilesystemPrefix = PathUtils.ResolveAppRelative("~/azure/");
        /// <summary>
        /// Requests starting with this path will be handled by this virtual path provider.
        /// Can be in app-relative form: "~/azure/". Will be translated to domain-relative form.
        /// </summary>
        public string VirtualFilesystemPrefix
        {
            get
            {
                return _virtualFilesystemPrefix;
            }
            set
            {
                if (!value.EndsWith("/")) value += "/";
                _virtualFilesystemPrefix = PathUtils.ResolveAppRelativeAssumeAppRelative(value);
            }
        }

        /// <summary>
        /// If true, do not check if blob exists before trying to read it. This is faster and slightly cheaper 
        /// (saves one storage transaction), but adds the risk of redirecting to a blob that will return a 404
        /// from blob storage to the end user.
        /// </summary>
        public bool LazyExistenceCheck { get; set; }

        internal CloudBlobClient CloudBlobClient { get; set; }

        internal AzureVirtualPathProvider(string blobStorageConnection)
        {
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
        }

        /// <summary>
        /// Determines whether a specified virtual path is within the virtual file system.
        /// </summary>
        /// <param name="virtualPath">An absolute virtual path.</param>
        /// <returns>
        /// True if the virtual path is within the virtual file sytem; otherwise, false.
        /// </returns>
        public bool IsPathVirtual(string virtualPath)
        {
            return (virtualPath.StartsWith(VirtualFilesystemPrefix, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Internal usage only
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public override bool FileExists(string virtualPath)
        {
            if (FileExists(virtualPath, null)) return true;
            return Previous.FileExists(virtualPath);
        }

        /// <summary>
        /// Returns true if the specified file is within the azure virtual directory prefix, and if it exists. Returns true even if the file doesn't exist when LazyExistenceCheck=true
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public bool FileExists(string virtualPath, NameValueCollection queryString)
        {
            if (!IsPathVirtual(virtualPath)) return false;
            if (LazyExistenceCheck) return true;

            try
            {
                // Strip prefix from virtual path; keep container and blob
                string relativeBlobURL = string.Format("{0}/{1}", CloudBlobClient.BaseUri.OriginalString.TrimEnd('/', '\\'), virtualPath.Substring(VirtualFilesystemPrefix.Length).Trim('/', '\\'));

                // Get a reference to the blob, this will throw if blob does not exist
                CloudBlobClient.GetBlobReferenceFromServer(new Uri(relativeBlobURL));
                return true;
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 404)
                {
                    return false;
                }
                throw;
            }
        }

        /// <summary>
        /// For internal use only
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public override VirtualFile GetFile(string virtualPath)
        {
            VirtualFile vf = (VirtualFile)GetFile(virtualPath, null);
            return vf ?? Previous.GetFile(virtualPath);
        }

        /// <summary>
        /// For internal use only
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public IVirtualFile GetFile(string virtualPath, NameValueCollection queryString)
        {
            if (!IsPathVirtual(virtualPath)) return null;

            // Strip prefix from virtual path; keep container and blob
            string relativeBlobURL = virtualPath.Substring(VirtualFilesystemPrefix.Length).Trim('/', '\\');

            try
            {
                return new AzureFile(relativeBlobURL, this);
            }
            catch (StorageException e)
            {
                if (e.RequestInformation.HttpStatusCode == 404)
                {
                    return null;
                }
                throw;
            }
        }
    }
}
