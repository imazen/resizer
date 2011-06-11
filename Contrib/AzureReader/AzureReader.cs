using System;
using System.Collections.Specialized;
using System.Web;
using System.Web.Hosting;
using ImageResizer.Util;

namespace ImageResizer.Plugins.AzureReader {

    public class AzureReader : IPlugin {

        AzureVirtualPathProvider vpp = null;
        string blobStorageConnection;
        string vPath;

        public AzureReader(NameValueCollection args) {
            blobStorageConnection = args["connectionstring"];
            vPath = args["prefix"];
        }

        public IPlugin Install(Configuration.Config c) {
            if (vpp != null)
                throw new InvalidOperationException("This plugin can only be installed once, and cannot be uninstalled and reinstalled.");

            if (string.IsNullOrEmpty(blobStorageConnection))
                throw new InvalidOperationException("This plugin needs a connection string for the Azure blob storage.");

            if (string.IsNullOrEmpty(vPath))
                vPath = "~/azure/";

            vpp = new AzureVirtualPathProvider(blobStorageConnection);
            vpp.VirtualFilesystemPrefix = vPath;

            // Registers the virtual path provider.
            HostingEnvironment.RegisterVirtualPathProvider(vpp);

            c.Plugins.add_plugin(this);
            c.Pipeline.RewriteDefaults += Pipeline_RewriteDefaults;
            return this;
        }

        void Pipeline_RewriteDefaults(IHttpModule sender, HttpContext context, Configuration.IUrlEventArgs e) {

        }

        void Pipeline_PostRewrite(IHttpModule sender, HttpContext context, Configuration.IUrlEventArgs e) {
            string prefix = PathUtils.ResolveAppRelative(vPath);

            // Check if prefix is within virtual file system and if there is no querystring
            if (e.VirtualPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && e.QueryString.Count == 0) {
                string url = vpp.GetBlobURI(e.VirtualPath);

                if (!string.IsNullOrWhiteSpace(url)) {
                    context.Response.Redirect(url);
                }
            }
        }

        public bool Uninstall(Configuration.Config c) {
            return false;
        }
    }
}
