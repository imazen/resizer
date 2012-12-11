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

namespace ImageResizer.Plugins.AzureReader2 {

    public class AzureReader2Plugin : IPlugin, IIssueProvider, IMultiInstancePlugin, IRedactDiagnostics {

        AzureVirtualPathProvider vpp = null;
        string blobStorageConnection;
        string blobStorageEndpoint;
        string vPath;
        bool lazyExistenceCheck = false;

        public AzureReader2Plugin(NameValueCollection args) {
            blobStorageConnection = args["connectionstring"];
            blobStorageEndpoint = args["blobstorageendpoint"];
            if (string.IsNullOrEmpty(blobStorageEndpoint)) blobStorageEndpoint = args["endpoint"];
            vPath = args["prefix"];
            lazyExistenceCheck = Utils.getBool(args, "lazyExistenceCheck", lazyExistenceCheck);
            _registerAsVirtualPathProvider = Utils.getBool(args, "vpp", _registerAsVirtualPathProvider);
        }


        private bool _failedToRegisterVpp = false;
        /// <summary>
        /// True if the provider attempted to register itself as a VirtualPathProvider and failed due to limited security clearance.
        /// False if it did not attempt, or if it succeeded.
        /// </summary>
        public bool FailedToRegisterVpp {
            get { return _failedToRegisterVpp; }
        }

        private bool _registerAsVirtualPathProvider = true;
        /// <summary>
        /// True to register the plugin as  VPP, false to register it as a VIP. VIPs are only visible to the ImageResizer pipeline - i.e, only processed images are visible. 
        /// </summary>
        public bool RegisterAsVirtualPathProvider {
            get { return _registerAsVirtualPathProvider; }
            set { _registerAsVirtualPathProvider = value; }
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

            if (RegisterAsVirtualPathProvider) {
                try {
                    HostingEnvironment.RegisterVirtualPathProvider(vpp);
                } catch (SecurityException) {
                    this._failedToRegisterVpp = true;
                    c.Plugins.VirtualProviderPlugins.Add(vpp); //Fall back to VIP instead.
                }
            }

            // Register rewrite
            c.Pipeline.PostRewrite += Pipeline_PostRewrite;

            c.Plugins.add_plugin(this);

            return this;
        }

        public Configuration.Xml.Node RedactFrom(Node resizer) {
            foreach (Node n in resizer.queryUncached("plugins.add")) {
                if (n.Attrs["connectionString"] != null) n.Attrs.Set("connectionString", "[redacted]");
            }
            return resizer;
        }

        /// <summary>
        /// In case there is no querystring attached to the file (thus no operations on the fly) we can
        /// redirect directly to the blob. This let us take advantage of the CDN (if configured).
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
            //We can uninstall if it wasn't installed as a VPP
            if (!RegisterAsVirtualPathProvider || FailedToRegisterVpp) {
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
        public IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>();

            if (FailedToRegisterVpp)
                issues.Add(new Issue("AzureReader", "Failed to register as VirtualPathProvider.",
                    "Only the image resizer will be able to access files located in Azure Blob Storage - other systems will not be able to.", IssueSeverity.Error));


            return issues;
        }


    }
}
