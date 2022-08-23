/* Copyright (c) 2011 Wouter A. Alberts and Nathanael D. Jones. See license.txt for your rights. */

using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using ImageResizer.Configuration;
using ImageResizer.ExtensionMethods;
using ImageResizer.Storage;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Imazen.Common.Storage;
using Microsoft.Azure;

namespace ImageResizer.Plugins.AzureReader2
{
    internal class AzureBlob :IBlobData, IBlobMetadata
    {
        private readonly Response<BlobDownloadInfo> response;

        internal AzureBlob(Response<BlobDownloadInfo> r)
        {
            response = r;
        }

        public bool? Exists => true;
        public DateTime? LastModifiedDateUtc => response.Value.Details.LastModified.UtcDateTime;
        public Stream OpenRead()
        {
            return response.Value.Content;
        }

        public void Dispose()
        {
            response?.Value?.Dispose();
        }
    }
    
    public class AzureReader2Plugin : BlobProviderBase, IMultiInstancePlugin
    {
        private BlobServiceClient BlobServiceClient { get; set; }
        private string blobStorageConnection;
        private string blobStorageEndpoint;


        public bool RedirectToBlobIfUnmodified { get; set; }

        public AzureReader2Plugin()
            : base()
        {
            VirtualFilesystemPrefix = "~/azure";
        }

        public AzureReader2Plugin(NameValueCollection args) : this()
        {
            LoadConfiguration(args);
            blobStorageConnection = args["connectionstring"];
            blobStorageEndpoint = args.GetAsString("blobstorageendpoint", args.GetAsString("endpoint", null));

            RedirectToBlobIfUnmodified = args.Get<bool>("redirectToBlobIfUnmodified", true);
        }


        private async Task<IBlobData> GetBlobRefAsync(string virtualPath)
        {
            var subPath = StripPrefix(virtualPath).Trim('/', '\\');
            
            var indexOfFirstSlash = subPath.IndexOf('/');
            if (indexOfFirstSlash < 0)
            {
                throw new BlobMissingException($"No container name specified in virtual path (/container/blob)");
            }
            else
            {
                var container = subPath.Substring(0, indexOfFirstSlash);
                var key  = subPath.Substring(indexOfFirstSlash + 1);

                try
                {
                    var blobClient = BlobServiceClient.GetBlobContainerClient(container).GetBlobClient(key);

                    var s = await blobClient.DownloadAsync();
                    return new AzureBlob(s);
                }
                catch (RequestFailedException e)
                {
                    if (e.Status == 404)
                    {
                        throw new BlobMissingException($"Azure blob \"{key}\" not found.\n({e.Message})", e);
                    }

                    throw;

                }

            }
            
        }

        public override async Task<IBlobMetadata> FetchMetadataAsync(string virtualPath,
            NameValueCollection queryString)
        {
            try
            {
                return (IBlobMetadata) await GetBlobRefAsync(virtualPath);
            }
            catch (BlobMissingException)
            {
                return new BlobMetadata() { Exists = false };
            }
        }

        public override async Task<Stream> OpenAsync(string virtualPath, NameValueCollection queryString)
        {
            var time = Stopwatch.StartNew();
            var blob = await GetBlobRefAsync(virtualPath);
            using (var stream = blob.OpenRead())
            {
                var ms = await stream.CopyToMemoryStreamAsync();
                time.Stop();
                ReportReadTicks(time.ElapsedTicks, ms.Length);
                return ms;
            }
        }

        public override IPlugin Install(Config c)
        {
            if (string.IsNullOrEmpty(blobStorageConnection))
                throw new InvalidOperationException(
                    "AzureReader2 requires a named connection string or a connection string to be specified with the 'connectionString' attribute.");

            // Setup the connection to Windows Azure Storage
            // for compatibility, look up the appSetting first.
            var connectionString = CloudConfigurationManager.GetSetting(blobStorageConnection);
            if (string.IsNullOrEmpty(connectionString))
                connectionString = ConfigurationManager.ConnectionStrings[blobStorageConnection]?.ConnectionString;

            if (string.IsNullOrEmpty(connectionString)) connectionString = blobStorageConnection;

           // BlobClientOptions options = new BlobClientOptions();
           BlobClientOptions options = new BlobClientOptions(
               BlobClientOptions.ServiceVersion.V2021_06_08);

            BlobServiceClient =  new BlobServiceClient(connectionString, options);

            if (blobStorageEndpoint != null)
            {
                // TODO: log issue, we ignore the endpoint setting now?
            }

            // Register rewrite
            c.Pipeline.PostRewrite += Pipeline_PostRewrite;

            base.Install(c);

            return this;
        }

        /// <summary>
        ///     Removes the plugin from the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public override bool Uninstall(Config c)
        {
            c.Pipeline.PostRewrite -= Pipeline_PostRewrite;
            return base.Uninstall(c);
        }

        /// <summary>
        ///     In case there is no querystring attached to the file (thus no operations on the fly) we can
        ///     redirect directly to the blob. This let us offload traffic to blob storage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="context"></param>
        /// <param name="e"></param>
        private void Pipeline_PostRewrite(IHttpModule sender, HttpContext context, IUrlEventArgs e)
        {
            var prefix = VirtualFilesystemPrefix;

            // Check if prefix is within virtual file system and if there is no querystring
            if (RedirectToBlobIfUnmodified && Belongs(e.VirtualPath) && !c.Pipeline.HasPipelineDirective(e.QueryString))
            {
                // Strip prefix from virtual path; keep container and blob
                var relativeBlobURL = e.VirtualPath.Substring(prefix.Length).TrimStart('/', '\\');

                // Redirect to blob
                //TODO: Add shared access signature if enabled
                context.Response.Redirect(blobStorageEndpoint + relativeBlobURL);
            }
        }
    }
}