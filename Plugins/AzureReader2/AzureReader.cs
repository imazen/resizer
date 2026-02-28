/* Copyright (c) 2011 Wouter A. Alberts and Nathanael D. Jones. See license.txt for your rights. */
using System;
using System.Collections.Specialized;
using System.Web;
using System.Web.Hosting;
using ImageResizer.Util;
using System.Collections.Generic;
using ImageResizer.Configuration.Issues;
using System.Security;
using ImageResizer.Configuration.Xml;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using ImageResizer.Storage;
using ImageResizer.ExtensionMethods;
using System.IO;
using System.Threading.Tasks;
using System.Configuration;
using System.Diagnostics;

namespace ImageResizer.Plugins.AzureReader2 {

    public class AzureReader2Plugin : BlobProviderBase, IMultiInstancePlugin {

        public BlobServiceClient BlobServiceClient { get; set; }
        string blobStorageConnection;
        string blobStorageEndpoint;


        public bool RedirectToBlobIfUnmodified { get; set; }

        public AzureReader2Plugin()
            : base()
        {
            this.VirtualFilesystemPrefix = "~/azure";

        }
        public AzureReader2Plugin(NameValueCollection args):this() {
            LoadConfiguration(args);
            blobStorageConnection = args["connectionstring"];
            blobStorageEndpoint = args.GetAsString("blobstorageendpoint", args.GetAsString("endpoint",null));
            RedirectToBlobIfUnmodified = args.Get<bool>("redirectToBlobIfUnmodified", true);

        }


        protected Task<BlobClient> GetBlobRefAsync(string virtualPath)
        {
            string subPath = StripPrefix(virtualPath).Trim('/', '\\');

            // The subpath format is "container/blob/path" — split on the first slash
            int slashIndex = subPath.IndexOf('/');
            if (slashIndex < 0)
            {
                // No slash means the path is just a container name with no blob
                throw new FileNotFoundException("Invalid blob path: no blob name specified");
            }

            string containerName = subPath.Substring(0, slashIndex);
            string blobName = subPath.Substring(slashIndex + 1);

            var blobClient = BlobServiceClient
                .GetBlobContainerClient(containerName)
                .GetBlobClient(blobName);

            return Task.FromResult(blobClient);
        }
        public override async Task<IBlobMetadata> FetchMetadataAsync(string virtualPath, NameValueCollection queryString)
        {

            try
            {
                var blobClient = await GetBlobRefAsync(virtualPath);
                var properties = await blobClient.GetPropertiesAsync();

                var meta = new BlobMetadata();
                meta.Exists = true;
                var utc = properties.Value.LastModified;
                meta.LastModifiedDateUtc = utc.UtcDateTime;
                return meta;
            }
            catch (RequestFailedException e) when (e.Status == 404)
            {
                return new BlobMetadata() { Exists = false };
            }
        }

        public override async Task<Stream> OpenAsync(string virtualPath, NameValueCollection queryString)
        {
            var time = Stopwatch.StartNew();
            MemoryStream ms = new MemoryStream(4096); // 4kb is a good starting point.

            try
            {
                var blobClient = await GetBlobRefAsync(virtualPath);
                await blobClient.DownloadToAsync(ms);
            }
            catch (RequestFailedException e) when (e.Status == 404)
            {
                throw new FileNotFoundException("Azure blob file not found", e);
            }

            ms.Seek(0, SeekOrigin.Begin); // Reset to beginning
            time.Stop();
            this.ReportReadTicks(time.ElapsedTicks, ms.Length);
            return ms;
        }

        public override IPlugin Install(Configuration.Config c) {
            if (string.IsNullOrEmpty(blobStorageConnection))
                throw new InvalidOperationException("AzureReader2 requires a named connection string or a connection string to be specified with the 'connectionString' attribute.");

            // Resolve the connection string: check appSettings first (backwards compat), then connectionStrings, then use raw value
            var connectionString = ConfigurationManager.AppSettings[blobStorageConnection];
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = ConfigurationManager.ConnectionStrings[blobStorageConnection]?.ConnectionString;
            }

            if (string.IsNullOrEmpty(connectionString)) { connectionString = blobStorageConnection; }

            try
            {
                BlobServiceClient = new BlobServiceClient(connectionString);

                if (string.IsNullOrEmpty(blobStorageEndpoint))
                {
                    blobStorageEndpoint = BlobServiceClient.Uri.ToString();
                }
            }
            catch (Exception ex) when (ex is FormatException || ex is ArgumentException || ex is UriFormatException)
            {
                throw new InvalidOperationException("Invalid AzureReader2 connectionString value; rejected by Azure SDK.", ex);
            }

            if (!blobStorageEndpoint.EndsWith("/"))
                blobStorageEndpoint += "/";

            // Register rewrite
            c.Pipeline.PostRewrite += Pipeline_PostRewrite;

            base.Install(c);

            return this;
        }
        /// <summary>
        /// Removes the plugin from the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public override bool Uninstall(Configuration.Config c)
        {
            c.Pipeline.PostRewrite -= Pipeline_PostRewrite;
            return base.Uninstall(c);
        }

        /// <summary>
        /// In case there is no querystring attached to the file (thus no operations on the fly) we can
        /// redirect directly to the blob. This let us offload traffic to blob storage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="context"></param>
        /// <param name="e"></param>
        void Pipeline_PostRewrite(IHttpModule sender, HttpContext context, Configuration.IUrlEventArgs e) {
            string prefix = VirtualFilesystemPrefix;

            // Check if prefix is within virtual file system and if there is no querystring
            if (RedirectToBlobIfUnmodified && Belongs(e.VirtualPath) && !c.Pipeline.HasPipelineDirective(e.QueryString)) {

                // Strip prefix from virtual path; keep container and blob
                string relativeBlobURL = e.VirtualPath.Substring(prefix.Length).TrimStart('/', '\\');

                // Redirect to blob
                //TODO: Add shared access signature if enabled
                context.Response.Redirect(blobStorageEndpoint + relativeBlobURL);
            }
        }




    }
}
