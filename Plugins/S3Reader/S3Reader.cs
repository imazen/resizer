/* Copyright (c) 2011 Nathanael Jones. See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Hosting;
using System.Collections.Specialized;
using LitS3;
using ImageResizer.Configuration.Issues;
using System.Security;

namespace ImageResizer.Plugins.S3Reader {
    public class S3Reader : IPlugin, IMultiInstancePlugin {

        string buckets, vpath;
        bool includeModifiedDate = false;
        bool asVpp = false;
        public S3Reader(NameValueCollection args ) {
            s3config = new S3Service();

            buckets = args["buckets"];
            vpath = args["prefix"];

            asVpp = Util.Utils.getBool(args, "vpp", true);
            s3config.UseSsl = Util.Utils.getBool(args, "useSsl", false);
            s3config.UseSubdomains = Util.Utils.getBool(args, "useSubdomains", s3config.UseSubdomains);


            if (!string.IsNullOrEmpty(args["accessKeyId"])) s3config.AccessKeyID = args["accessKeyId"];
            if (!string.IsNullOrEmpty(args["secretAccessKey"])) s3config.SecretAccessKey = args["secretAccessKey"];

            


            includeModifiedDate = Util.Utils.getBool(args, "includeModifiedDate", includeModifiedDate);

            includeModifiedDate = Util.Utils.getBool(args, "checkForModifiedFiles", includeModifiedDate);
            
        }
        private S3Service s3config = null;

        /// <summary>
        /// Configure S3 authentication and encryption details
        /// </summary>
        public S3Service S3config {
            get { return s3config; }
            set { s3config = value; }
        }
        S3VirtualPathProvider vpp = null;

        public IPlugin Install(Configuration.Config c) {

            if (vpp != null) throw new InvalidOperationException("This plugin can only be installed once, and cannot be uninstalled and reinstalled.");

            if (string.IsNullOrEmpty(vpath)) vpath = "~/s3/";

            string[] bucketArray = null;
            if (!string.IsNullOrEmpty(buckets)) bucketArray = buckets.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            else c.configurationSectionIssues.AcceptIssue(new Issue("S3Reader", "S3Reader cannot function without a list of permitted bucket names.",
                "Please specify a comma-delimited list of buckets in the <add name='S3Reader' buckets='bucketa,bucketb' /> element.",
                 IssueSeverity.ConfigurationError));

            for (int i = 0; i < bucketArray.Length; i++)
                bucketArray[i] = bucketArray[i].Trim();


            vpp = new S3VirtualPathProvider(delegate(S3VirtualPathProvider s, S3PathEventArgs ev) {
                if (bucketArray == null) ev.ThrowException();
                ev.AssertBucketMatches(bucketArray);
            }, !includeModifiedDate);

            
            vpp.Service = s3config;

            vpp.VirtualFilesystemPrefix = vpath;
           // vpp.MetadataAbsoluteExpiration

            if (asVpp) {
                try {
                    //Registers the virtual path provider.
                    HostingEnvironment.RegisterVirtualPathProvider(vpp);
                } catch (SecurityException) {
                    asVpp = false;
                    c.configurationSectionIssues.AcceptIssue(new Issue("S3Reader", "S3Reader could not be installed as a VirtualPathProvider due to missing AspNetHostingPermission."
                    ,"It was installed as an IVirtualImageProvider instead, which means that only image URLs will be accessible, and only if they contain a querystring.\n" +
                    "Set vpp=false to tell S3Reader to register as an IVirtualImageProvider instead. <add name='S3Reader' vpp=false />", IssueSeverity.Error));
                }
            }
            if (!asVpp) {
                c.Plugins.VirtualProviderPlugins.Add(vpp);
            }

            //Register a url default. All incoming urls that point to this reader will automatically have 'cache=auto' unless another cache value has been specified.
            c.Pipeline.RewriteDefaults += new Configuration.UrlRewritingEventHandler(Pipeline_RewriteDefaults);

            c.Plugins.add_plugin(this);
            
            return this;

        }

        void Pipeline_RewriteDefaults(System.Web.IHttpModule sender, System.Web.HttpContext context, Configuration.IUrlEventArgs e) {
            //Always request caching for this VPP. Will not override existing values.
            if (vpp.IsPathVirtual(e.VirtualPath)) e.QueryString["cache"] = ServerCacheMode.Always.ToString();
        }

        /// <summary>
        /// This plugin can only be removed if it was installed as an IVirtualImageProvider (vpp="false")
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Uninstall(Configuration.Config c) {
            if (!asVpp) {
                c.Plugins.VirtualProviderPlugins.Remove(vpp);
                return true;
            }else
            return false;
        }

    }
}
