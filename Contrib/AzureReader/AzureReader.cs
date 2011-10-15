/* Copyright (c) 2011 Wouter A. Alberts and Nathanael D. Jones. See license.txt for your rights. */
using System;
using System.Collections.Specialized;
using System.Web;
using System.Web.Hosting;
using ImageResizer.Util;

namespace ImageResizer.Plugins.AzureReader {

    public class AzureReader : IPlugin {

        AzureVirtualPathProvider vpp = null;
        string blobStorageConnection;
        string blobStorageEndpoint;
        string vPath;
        bool lazyExistenceCheck = false;

        public AzureReader(NameValueCollection args) {
            blobStorageConnection = args["connectionstring"];
            blobStorageEndpoint = args["blobstorageendpoint"];
            vPath = args["prefix"];
            lazyExistenceCheck = Utils.getBool(args, "lazyExistenceCheck", lazyExistenceCheck);
        }

        public IPlugin Install(Configuration.Config c) {
            if (vpp != null)
                throw new InvalidOperationException("This plugin can only be installed once, and cannot be uninstalled and reinstalled.");

            if (string.IsNullOrEmpty(blobStorageConnection))
                throw new InvalidOperationException("This plugin needs a connection string for the Azure blob storage.");

            if (string.IsNullOrEmpty(blobStorageEndpoint))
                throw new InvalidOperationException("This plugin needs a blob end point; the default will be [http|https]://myaccount.blob.core.windows.net.");

            if (!blobStorageEndpoint.EndsWith("/"))
                blobStorageEndpoint += "/";

            if (string.IsNullOrEmpty(vPath))
                vPath = "~/azure/";

            vpp = new AzureVirtualPathProvider(blobStorageConnection);
            vpp.VirtualFilesystemPrefix = vPath;
            vpp.LazyExistenceCheck = lazyExistenceCheck;

            // Registers the virtual path provider
            HostingEnvironment.RegisterVirtualPathProvider(vpp);

            // Register rewrite
            c.Pipeline.PostRewrite += new Configuration.UrlRewritingEventHandler(Pipeline_PostRewrite);

            c.Plugins.add_plugin(this);

            return this;
        }

        /// <summary>
        /// In case there is no querystring attached to the file (thus no operations on the fly) we can
        /// redirect directly to the blob. This let us make advantage of the CDN (if configured).
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="context"></param>
        /// <param name="e"></param>
        void Pipeline_PostRewrite(IHttpModule sender, HttpContext context, Configuration.IUrlEventArgs e) {
            string prefix = vpp.VirtualFilesystemPrefix;

            // Check if prefix is within virtual file system and if there is no querystring
            if (e.VirtualPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && e.QueryString.Count == 0) {

                // Strip prefix from virtual path; keep container and blob
                string relativeBlobURL = e.VirtualPath.Substring(vPath.Length -1).Trim('/', '\\');

                // Redirect to blob
                context.Response.Redirect(blobStorageEndpoint + relativeBlobURL);
            }
        }

        public bool Uninstall(Configuration.Config c) {
            return false;
        }
    }
}
