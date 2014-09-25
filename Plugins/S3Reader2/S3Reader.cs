/* Copyright (c) 2014 Imazen See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Hosting;
using System.Collections.Specialized;
using ImageResizer.Configuration.Issues;
using System.Security;
using ImageResizer.Util;
using System.Web;
using ImageResizer.ExtensionMethods;
using Amazon.S3;
using ImageResizer.Configuration.Xml;

namespace ImageResizer.Plugins.S3Reader2
{
    public class S3Reader2 : IPlugin, IMultiInstancePlugin, IRedactDiagnostics
    {

        string buckets, vpath;
        bool includeModifiedDate = false;
        bool asVpp = false;
        AmazonS3Config s3config = null;
        public S3Reader2(NameValueCollection args)
        {

            s3config = new AmazonS3Config();

            buckets = args["buckets"];
            vpath = args["prefix"];

            asVpp = args.Get("vpp", true);

            Region = args["region"] ?? "us-east-1";


            s3config.UseHttp = !args.Get("useSsl", false);

            if (!string.IsNullOrEmpty(args["accessKeyId"]) && !string.IsNullOrEmpty(args["secretAccessKey"]))
            {
                S3Client = new AmazonS3Client(args["accessKeyId"], args["secretAccessKey"], s3config);
            }
            else
            {

                S3Client = new AmazonS3Client(null, s3config);
            }

            includeModifiedDate = args.Get("includeModifiedDate", includeModifiedDate);

            includeModifiedDate = args.Get("checkForModifiedFiles", includeModifiedDate);

            RequireImageExtension = args.Get("requireImageExtension", RequireImageExtension);
            UntrustedData = args.Get("untrustedData", UntrustedData);
            CacheUnmodifiedFiles = args.Get("cacheUnmodifiedFiles", CacheUnmodifiedFiles);

        }


        public Configuration.Xml.Node RedactFrom(Node resizer)
        {
            if (resizer == null || resizer.queryUncached("plugins.add") == null)
                return resizer;
            foreach (Node n in resizer.queryUncached("plugins.add"))
            {
                if (n.Attrs["accessKeyId"] != null)
                    n.Attrs.Set("accessKeyId", "[redacted]");
                if (n.Attrs["secretAccessKey"] != null)
                    n.Attrs.Set("secretAccessKey", "[redacted]");
            }
            return resizer;
        }

        /// <summary>
        /// Configure AWS access keys
        /// </summary>
        public AmazonS3Client S3Client { get; set; }


        public string Region
        {
            get { return this.s3config != null && this.s3config.RegionEndpoint != null ? this.s3config.RegionEndpoint.SystemName : null; }
            set
            {
                this.s3config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(value);
            }
        }

        private bool _requireImageExtension = true;
        /// <summary>
        /// (default true) When false, all URLs inside the PathPrefix folder will be assumed to be images, and will be handled by this plugin.
        /// You should still use image extensions, otherwise we don't know what content type to send with the response, and browsers will choke. 
        /// It's  also the cleanest way to tell the image resizer what kind of file type you'd like back when you request resizing.
        /// This setting is designed to support non-image file serving from the DB.
        /// It will also cause conflicts if PathPrefix overlaps with a folder name used for something else.
        /// </summary>
        public bool RequireImageExtension
        {
            get { return _requireImageExtension; }
            set { _requireImageExtension = value; }
        }

        private bool _untrustedData = false;
        /// <summary>
        /// (default: false) When true, all requests will be re-encoded before being served to the client. Invalid or malicious images will fail with an error if they cannot be read as images.
        /// This should prevent malicious files from being served to the client.
        /// </summary>
        public bool UntrustedData
        {
            get { return _untrustedData; }
            set { _untrustedData = value; }
        }

        private bool _cacheUnmodifiedFiles = true;
        /// <summary>
        /// (default true). When true, files and unmodified images (i.e, no querystring) will be cached to disk (if they are requested that way) instead of only caching requests for resized images.
        /// DiskCache plugin must be installed for this to have any effect.
        /// </summary>
        public bool CacheUnmodifiedFiles
        {
            get { return _cacheUnmodifiedFiles; }
            set { _cacheUnmodifiedFiles = value; }
        }


        S3VirtualPathProvider vpp = null;

        public IPlugin Install(Configuration.Config c)
        {

            if (vpp != null)
                throw new InvalidOperationException("This plugin can only be installed once, and cannot be uninstalled and reinstalled.");

            if (string.IsNullOrEmpty(vpath))
                vpath = "~/s3/";

            string[] bucketArray = null;
            if (!string.IsNullOrEmpty(buckets))
                bucketArray = buckets.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            else
                c.configurationSectionIssues.AcceptIssue(new Issue("S3Reader", "S3Reader cannot function without a list of permitted bucket names.",
                    "Please specify a comma-delimited list of buckets in the <add name='S3Reader' buckets='bucketa,bucketb' /> element.",
                IssueSeverity.ConfigurationError));

            for (int i = 0; i < bucketArray.Length; i++)
                bucketArray[i] = bucketArray[i].Trim();


            vpp = new S3VirtualPathProvider(this.S3Client, vpath, TimeSpan.MaxValue, new TimeSpan(0, 1, 0, 0), delegate(S3VirtualPathProvider s, S3PathEventArgs ev)
            {
                if (bucketArray == null)
                    ev.ThrowException();
                ev.AssertBucketMatches(bucketArray);
            }, !includeModifiedDate);




            c.Pipeline.PostAuthorizeRequestStart += delegate(IHttpModule sender2, HttpContext context)
            {
                //Only work with database images
                //This allows us to resize database images without putting ".jpg" after the ID in the path.
                if (!RequireImageExtension && vpp.IsPathVirtual(c.Pipeline.PreRewritePath))
                    c.Pipeline.SkipFileTypeCheck = true; //Skip the file extension check. FakeExtensions will still be stripped.
            };


            c.Pipeline.RewriteDefaults += delegate(IHttpModule sender, HttpContext context, Configuration.IUrlEventArgs e)
            {
                //Only work with database images
                //Non-images will be served as-is
                //Cache all file types, whether they are processed or not.
                if (CacheUnmodifiedFiles && vpp.IsPathVirtual(e.VirtualPath))
                    e.QueryString["cache"] = ServerCacheMode.Always.ToString();


            };
            c.Pipeline.PostRewrite += delegate(IHttpModule sender, HttpContext context, Configuration.IUrlEventArgs e)
            {
                //Only work with database images
                //If the data is untrusted, always re-encode each file.
                if (UntrustedData && vpp.IsPathVirtual(e.VirtualPath))
                    e.QueryString["process"] = ImageResizer.ProcessWhen.Always.ToString();

            };

            if (asVpp)
            {
                try
                {
                    //Registers the virtual path provider.
                    HostingEnvironment.RegisterVirtualPathProvider(vpp);
                }
                catch (SecurityException)
                {
                    asVpp = false;
                    c.configurationSectionIssues.AcceptIssue(new Issue("S3Reader", "S3Reader could not be installed as a VirtualPathProvider due to missing AspNetHostingPermission."
                    , "It was installed as an IVirtualImageProvider instead, which means that only image URLs will be accessible, and only if they contain a querystring.\n" +
                    "Set vpp=false to tell S3Reader to register as an IVirtualImageProvider instead. <add name='S3Reader' vpp=false />", IssueSeverity.Error));
                }
            }
            if (!asVpp)
            {
                c.Plugins.VirtualProviderPlugins.Add(vpp);
            }

            //
            c.Plugins.add_plugin(this);

            return this;

        }


        /// <summary>
        /// This plugin can only be removed if it was installed as an IVirtualImageProvider (vpp="false")
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Uninstall(Configuration.Config c)
        {
            if (!asVpp)
            {
                c.Plugins.VirtualProviderPlugins.Remove(vpp);
                return true;
            }
            else
                return false;
        }

    }
}
