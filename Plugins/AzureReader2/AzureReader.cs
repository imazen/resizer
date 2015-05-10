/* Copyright (c) 2011 Wouter A. Alberts and Nathanael D. Jones. See license.txt for your rights. */
using System;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Web;
using System.Web.Hosting;
using ImageResizer.Util;
using System.Collections.Generic;
using ImageResizer.Configuration.Issues;
using System.Security;
using ImageResizer.Configuration.Xml;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace ImageResizer.Plugins.AzureReader2
{
    /// <summary>
    /// A plugin that allows reading images directly from Microsoft Azure Blob Storage instead of local disk.
    /// </summary>
    public class AzureReader2Plugin : IPlugin, IIssueProvider, IMultiInstancePlugin, IRedactDiagnostics
    {
        const string defaultPrefix = "~/azure/";
        AzureVirtualPathProvider vpp;
        readonly string blobStorageConnection;
        string blobStorageEndpoint;
        readonly int sharedAccessExpiryTime = 60;
        string vPath;
        readonly bool lazyExistenceCheck;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args">
        /// The following settings can be passed in the NameValueCollection:
        /// connectionSting: The connection string, or name of connection string to be used for Blob Storage. (REQUIRED)
        /// blobStorageEndpoint: The http(s) base URL for the files. Can be used if you have a custom CNAME for your Storage Account or require HTTPS. Otherwise derived from connection string.
        /// endpoint: alias for blobStorageEndpoint
        /// SharedAccessExpiryTime: Number of minutes to give read access to a private blob when generating a Shared Access Signature. Defaults to 60 minutes.
        /// prefix: can be used to override the default virtual path ("~/azure/")
        /// vpp: register as Virtual Path Provider? Defaults to false.
        /// </param>
        public AzureReader2Plugin(NameValueCollection args)
        {
            FailedToRegisterVpp = false;
            blobStorageConnection = args["connectionstring"];
            blobStorageEndpoint = args["blobstorageendpoint"];
            sharedAccessExpiryTime = ParseUtils.ParsePrimitive(args["SharedAccessExpiryTime"], sharedAccessExpiryTime);
            if (string.IsNullOrEmpty(blobStorageEndpoint)) blobStorageEndpoint = args["endpoint"];
            vPath = args["prefix"];
            lazyExistenceCheck = ParseUtils.ParsePrimitive(args["lazyExistenceCheck"], lazyExistenceCheck);
            RegisterAsVirtualPathProvider = ParseUtils.ParsePrimitive(args["vpp"], RegisterAsVirtualPathProvider);
        }

        /// <summary>
        /// True if the provider attempted to register itself as a VirtualPathProvider and failed due to limited security clearance.
        /// False if it did not attempt, or if it succeeded.
        /// </summary>
        public bool FailedToRegisterVpp { get; private set; }

        /// <summary>
        /// True to register the plugin as  VPP, false to register it as a VIP. VIPs are only visible to the ImageResizer pipeline - i.e, only processed images are visible. 
        /// </summary>
        public bool RegisterAsVirtualPathProvider { get; set; }

        /// <summary>
        /// Installs the AzureReader2 plugin into the ImageResizer configuration provided
        /// </summary>
        /// <param name="c">The configuration to install the plugin into</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Throws an exception if the configuration is invalid</exception>
        public IPlugin Install(Configuration.Config c)
        {
            if (vpp != null)
                throw new InvalidOperationException("This plugin can only be installed once, and cannot be uninstalled and reinstalled.");

            if (string.IsNullOrEmpty(blobStorageConnection))
                throw new InvalidOperationException("This plugin needs a connection string for the Azure blob storage.");

            if (string.IsNullOrEmpty(blobStorageEndpoint))
            {
                CloudStorageAccount cloudStorageAccount;
                if (CloudStorageAccount.TryParse(blobStorageConnection, out cloudStorageAccount))
                {
                    blobStorageEndpoint = cloudStorageAccount.BlobEndpoint.ToString();
                }
                else
                {
                    throw new InvalidOperationException("This plugin needs a correct blob storage connection to be able to automatically find the storage endpoint.");
                }
            }

            if (!blobStorageEndpoint.EndsWith("/"))
                blobStorageEndpoint += "/";

            if (string.IsNullOrEmpty(vPath))
                vPath = defaultPrefix;

            vpp = new AzureVirtualPathProvider(blobStorageConnection)
            {
                VirtualFilesystemPrefix = vPath,
                LazyExistenceCheck = lazyExistenceCheck
            };

            if (RegisterAsVirtualPathProvider)
            {
                try
                {
                    HostingEnvironment.RegisterVirtualPathProvider(vpp);
                }
                catch (SecurityException)
                {
                    FailedToRegisterVpp = true;
                    c.Plugins.VirtualProviderPlugins.Add(vpp); //Fall back to VIP instead.
                }
            }

            // Register rewrite
            c.Pipeline.PostRewrite += Pipeline_PostRewrite;
            c.Plugins.add_plugin(this);
            return this;
        }

        /// <summary>
        /// Internal use only. Removes connection string from config before displaying.
        /// </summary>
        /// <param name="resizer"></param>
        /// <returns></returns>
        public Node RedactFrom(Node resizer)
        {
            var nodes = resizer.queryUncached("plugins.add");
            if (nodes == null) return resizer;
            foreach (Node n in nodes)
            {
                if (n.Attrs["connectionString"] != null) n.Attrs.Set("connectionString", "[redacted]");
            }
            return resizer;
        }

        /// <summary>
        /// In case there is no querystring attached to the file (thus no operations on the fly) we can
        /// redirect directly to the blob. This let us offload traffic to blob storage.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="context"></param>
        /// <param name="e"></param>
        void Pipeline_PostRewrite(IHttpModule sender, HttpContext context, Configuration.IUrlEventArgs e)
        {
            string prefix = vpp.VirtualFilesystemPrefix;

            // Check if prefix is within virtual file system and if there is no querystring
            if (e.VirtualPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && e.QueryString.Count == 0)
            {

                // Strip prefix from virtual path; keep container and blob
                string relativeBlobURL = e.VirtualPath.Substring(prefix.Length).TrimStart('/', '\\');
                string sharedAccessSignature = "";

                // If Lazy Existence Check is disabled, check with Blob Storage, and append a Shared Access Signature to the redirect URL if the blob is private.
                if (!lazyExistenceCheck)
                {
                    CloudBlobClient cloudBlobClient = CloudStorageAccount.Parse(blobStorageConnection).CreateCloudBlobClient();
                    try
                    {
                        ICloudBlob blob = cloudBlobClient.GetBlobReferenceFromServer(new Uri(blobStorageEndpoint + relativeBlobURL));
                        sharedAccessSignature = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy()
                        {
                            Permissions = SharedAccessBlobPermissions.Read,
                            SharedAccessExpiryTime = new DateTimeOffset(DateTime.UtcNow.AddMinutes(sharedAccessExpiryTime))
                        });
                    }
                    catch (StorageException storageException)
                    {
                        if (storageException.RequestInformation.HttpStatusCode == (int)HttpStatusCode.NotFound)
                        {
                            throw new FileNotFoundException("AzureReader2: File not found", storageException);
                        }
                    }
                }

                // Redirect to blob
                context.Response.Redirect(blobStorageEndpoint + relativeBlobURL + sharedAccessSignature);
            }
        }

        /// <summary>
        /// Uninstall the plugin from the provided configuration. Only possible if not registered as a Virtual Path Provider.
        /// </summary>
        /// <param name="c">The configuration to uninstall the plugin from</param>
        /// <returns></returns>
        public bool Uninstall(Configuration.Config c)
        {
            // We can uninstall if it wasn't installed as a VPP
            if (!RegisterAsVirtualPathProvider || FailedToRegisterVpp)
            {
                c.Plugins.VirtualProviderPlugins.Remove(vpp);
                c.Pipeline.PostRewrite -= Pipeline_PostRewrite;
                c.Plugins.remove_plugin(this);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Provides the diagnostics system with a list of configuration issues
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IIssue> GetIssues()
        {
            List<IIssue> issues = new List<IIssue>();

            if (FailedToRegisterVpp)
                issues.Add(new Issue("AzureReader2", "Failed to register as VirtualPathProvider.",
                    "Only the image resizer will be able to access files located in Azure Blob Storage - other systems will not be able to.", IssueSeverity.Error));

            return issues;
        }
    }
}
