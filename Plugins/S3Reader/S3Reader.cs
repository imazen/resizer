/* Copyright (c) 2011 Nathanael Jones. See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Hosting;
using System.Collections.Specialized;
using LitS3;
using ImageResizer.Configuration.Issues;

namespace ImageResizer.Plugins.S3Reader {
    public class S3Reader : IPlugin {

        string buckets, vpath;
        bool includeModifiedDate = false;
        public S3Reader(NameValueCollection args ) {
            s3config = new S3Service();
            s3config.UseSsl = false;

            buckets = args["buckets"];
            vpath = args["prefix"];
            includeModifiedDate = Util.Utils.getBool(args, "includeModifiedDate", includeModifiedDate);
            
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
            if (!string.IsNullOrEmpty(buckets)) buckets.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            else c.configurationSectionIssues.AcceptIssue(new Issue("S3Reader", "S3Reader cannot function without a list of permitted bucket names.",
                "Please specify a comma-delimited list of buckets in the <add name='S3Reader' buckets='bucketa,bucketb' /> element.",
                 IssueSeverity.ConfigurationError));


            vpp = new S3VirtualPathProvider(delegate(S3VirtualPathProvider s, S3PathEventArgs ev) {
                if (bucketArray == null) ev.ThrowException();
                ev.AssertBucketMatches(bucketArray);
            }, !includeModifiedDate);


            vpp.Service = s3config;
            vpp.VirtualFilesystemPrefix = vpath;
           // vpp.MetadataAbsoluteExpiration

            //Registers the virtual path provider.
            HostingEnvironment.RegisterVirtualPathProvider(vpp);

            //Register a url default. All incoming urls that point to this reader will automatically have 'cache=auto' unless another cache value has been specified.
            c.Pipeline.RewriteDefaults += new Configuration.UrlRewritingEventHandler(Pipeline_RewriteDefaults);

            c.Plugins.add_plugin(this);
            
            return this;

        }

        void Pipeline_RewriteDefaults(System.Web.IHttpModule sender, System.Web.HttpContext context, Configuration.IUrlEventArgs e) {
            //Always request caching for this VPP. Will not override existing values.
            if (vpp.IsPathVirtual(e.VirtualPath)) e.QueryString["cache"] = ServerCacheMode.Always.ToString();
        }

        public bool Uninstall(Configuration.Config c) {
            return false;
        }

    }
}
