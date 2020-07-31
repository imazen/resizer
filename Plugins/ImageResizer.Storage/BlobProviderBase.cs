// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
﻿using ImageResizer.Configuration.Xml;
using ImageResizer.Plugins;
using ImageResizer.Util;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImageResizer.ExtensionMethods;
using System.IO;
using System.Web.Hosting;

namespace ImageResizer.Storage
{

    public interface IBlobMetadata{
        bool? Exists { get; set; }
        DateTime? LastModifiedDateUtc { get; set; } 
    }
    public class BlobMetadata:IBlobMetadata
    {
        public bool? Exists { get; set; }
        public DateTime? LastModifiedDateUtc { get; set; } 
    }
    public abstract class BlobProviderBase : IPlugin,IVirtualImageProviderAsync, IVirtualImageProvider, IVirtualImageProviderVpp, IRedactDiagnostics, ILicensedPlugin, IPluginInfo
    {
        /// <summary>
        /// Returns the license key feature codes that are able to activate this plugins.
        /// </summary>
        public IEnumerable<string> LicenseFeatureCodes
        {
            get { yield return "R_Performance"; yield return "R4Performance"; yield return "R4BlobProviders"; }
        }

        public BlobProviderBase()
        {
            this.UntrustedData = false;
            this.RequireImageExtension = true;
            this.LazyExistenceCheck = true;
            this.CacheUnmodifiedFiles = false;
            this.ExposeAsVpp = true;
            this.CheckForModifiedFiles = false;
            this.CacheMetadata = true;
            MetadataCache = new StandardMetadataCache();
        }

        public virtual void LoadConfiguration(NameValueCollection args){
            if (!string.IsNullOrEmpty(args["prefix"]))
            {
                VirtualFilesystemPrefix = args["prefix"];
            }
            CacheMetadata = args.Get<bool>("cacheMetadata", CacheMetadata);
            LazyExistenceCheck = args.Get<bool>("lazyExistenceCheck", LazyExistenceCheck);
            ExposeAsVpp = args.Get<bool>("vpp", ExposeAsVpp);
            RequireImageExtension = args.Get("requireImageExtension", RequireImageExtension);
            UntrustedData = args.Get("untrustedData", UntrustedData);
            CacheUnmodifiedFiles = args.Get("cacheUnmodifiedFiles", CacheUnmodifiedFiles);
            CheckForModifiedFiles = args.Get("includeModifiedDate", args.Get("checkForModifiedFiles", CheckForModifiedFiles));
        }

        /// <summary>
        /// Redacts any connectionString attribute from the diagnostics page.
        /// </summary>
        /// <param name="resizer"></param>
        /// <returns></returns>
        public Configuration.Xml.Node RedactFrom(Node resizer)
        {
            return resizer?.RedactAttributes("plugins.add", new [] { "connectionString"});
        }

        /// <summary>
        /// If true, metadata (such as modified dates and existence) will be cached.
        /// </summary>
        public bool CacheMetadata { get; set; }
        /// <summary>
        /// The caching system responsible for caching metadata (like existence and modified dates)
        /// </summary>
        public IMetadataCache MetadataCache { get; set; }
        /// <summary>
        /// If true, will cause additional requests to verify the remote resource is up-to-date.
        /// </summary>
        public bool CheckForModifiedFiles {get;set;}

        /// <summary>
        /// To avoid an extra request, it is possible to 'fail late', throwing FileNotFound when Open() is called instead of earlier.
        /// Upside: faster. Downside: no other provider can handle the request if there are route conflicts.
        /// </summary>
        public bool LazyExistenceCheck { get; set; }

        /// <summary>
        /// (default: false) When true, all requests will be re-encoded before being served to the client. Invalid or malicious images will fail with an error if they cannot be read as images.
        /// This should prevent malicious files from being served to the client.
        /// </summary>
        public bool UntrustedData { get; set; }
        /// <summary>
        /// (default false). When true, files and unmodified images (i.e, no querystring) will be cached to disk (if they are requested that way) instead of only caching requests for resized images.
        /// DiskCache plugin must be installed for this to have any effect.
        /// </summary>
        public bool CacheUnmodifiedFiles { get; set; }

        /// <summary>
        /// (default true) When false, all requests inside the VirtualFilesystemPrefix folder will be handled by this plugin.
        /// You should still use image extensions, otherwise we don't know what content type to send with the response, and browsers will choke. 
        /// It's  also the cleanest way to tell the image resizer what kind of file type you'd like back when you request resizing.
        /// This setting is designed to support non-image file serving
        /// It will also cause conflicts if VirtualFilesystemPrefix overlaps with a folder name used for something else.
        /// </summary>
        public bool RequireImageExtension { get; set; }

        /// <summary>
        /// If true, the blob provide will be accessible through the ASP.NET VirtualPathProvider system.
        /// </summary>
        public bool ExposeAsVpp { get; set; }

        /// <summary>
        /// Returns true if the request  is within the VirtualFilesystemPrefix. Override to provide more granular heuristics
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public virtual bool Belongs(string virtualPath)
        {
            return virtualPath.StartsWith(VirtualFilesystemPrefix, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Should perform an immediate (uncached) query of blob metadata (such as existence and modified date information)
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public abstract Task<IBlobMetadata> FetchMetadataAsync(string virtualPath, NameValueCollection queryString);
        public abstract Task<Stream> OpenAsync(string virtualPath, NameValueCollection queryString);

        /// <summary>
        /// Cached access to FetchMetadata. Only provides caching when CacheMetadata=true && MetadataCache != null.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public async Task<IBlobMetadata> FetchMetadataCachedAsync(string virtualPath, NameValueCollection queryString)
        {
            if (CacheMetadata && MetadataCache != null)
            {
                var key = DeriveMetadataCacheKey(virtualPath, queryString);
                var o = MetadataCache.Get(key) as IBlobMetadata;
                if (o == null)
                {
                    o = await FetchMetadataAsync(virtualPath, queryString);
                    MetadataCache.Put(key, o);
                }
                return o;
            }else{
                return await FetchMetadataAsync(virtualPath, queryString);
            }
        }

        protected virtual string DeriveMetadataCacheKey(string virtualPath, NameValueCollection q)
        {
            return virtualPath;
        }

        private string _virtualFilesystemPrefix = null;

        public string VirtualFilesystemPrefix
        {
            get
            {
                return _virtualFilesystemPrefix;
            }
            set
            {
                if (!value.EndsWith("/")) value += "/";
                _virtualFilesystemPrefix = value != null && HostingEnvironment.ApplicationVirtualPath != null ? PathUtils.ResolveAppRelativeAssumeAppRelative(value) : value;

            }
        }

        protected string StripPrefix(string virtualPath)
        {
            if (!virtualPath.StartsWith(VirtualFilesystemPrefix, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException();
            return virtualPath.Substring(VirtualFilesystemPrefix.Length);
        }

        /// <summary>
        /// If LazyExistenceCheck = true, same as Belongs(virtualPath). Otherwise also performs actual BlobExists() call.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public bool FileExists(string virtualPath, NameValueCollection queryString)
        {
            var belongs = Belongs(virtualPath);
            if (LazyExistenceCheck)
                return belongs;
            else
                return belongs && AsyncUtils.RunSync<bool>( () => BlobExistsAsync(virtualPath, queryString));
        }

        public IVirtualFile GetFile(string virtualPath, NameValueCollection queryString)
        {
            if (!FileExists(virtualPath, queryString)) return null;
            return new Blob(this, virtualPath, queryString);
        }

        public async Task<bool> FileExistsAsync(string virtualPath, NameValueCollection queryString)
        {
            var belongs = Belongs(virtualPath);
            if (LazyExistenceCheck)
                return belongs;
            else
                return belongs && await BlobExistsAsync(virtualPath, queryString);
        }

        public async Task<IVirtualFileAsync> GetFileAsync(string virtualPath, NameValueCollection queryString)
        {
            if (!await FileExistsAsync(virtualPath, queryString)) return null;
            return new Blob(this, virtualPath, queryString);
        }

        /// <summary>
        /// Performs a cached existence check to verify the blob actually exists.
        /// </summary>
        /// <param name="subPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public async Task<bool> BlobExistsAsync(string virtualPath, NameValueCollection queryString)
        {
            var m = await FetchMetadataCachedAsync(virtualPath, queryString);
            return m.Exists.Value;
        }
        /// <summary>
        /// Performs a cached metadata query to get the last modified date (UTC). 
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        public async Task<DateTime?> GetModifiedDateUtcAsync(string virtualPath, NameValueCollection queryString)
        {
            var m = await FetchMetadataCachedAsync(virtualPath, queryString);
            return m.LastModifiedDateUtc;
        }

        /// <summary>
        /// Returns true if the given file should be made available to the VirtualPathProvider system.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public bool VppExposeFile(string virtualPath)
        {
            return ExposeAsVpp;
        }

        protected Configuration.Config c;
        public virtual IPlugin Install(Configuration.Config c)
        {
            this.c = c;
            c.Plugins.add_plugin(this);

            c.Pipeline.PostAuthorizeRequestStart +=Pipeline_PostAuthorizeRequestStart; 
            c.Pipeline.RewriteDefaults +=Pipeline_RewriteDefaults;
            c.Pipeline.PostRewrite +=Pipeline_PostRewrite;
            c.Plugins.GetOrInstall<ImageResizer.Plugins.LicenseVerifier.LicenseEnforcer<BlobProviderBase>>();


            if (HostingEnvironment.IsHosted)  EnsureShimRegistered(c); //TODO; we should only install the shim for the singleton, but instead we are installing for every config. We can't access Config.Current here without creating recursion
            return this;
        }

        private static object lockShim = new object();
        private static VirtualPathProviderShim shim = null;
        private static void EnsureShimRegistered(Configuration.Config c)
        {
            lock (lockShim)
            {
                if (shim == null)
                {
                    var s = new VirtualPathProviderShim(c);
                    HostingEnvironment.RegisterVirtualPathProvider(s);
                    shim = s;
                }
            }
        } 

        void Pipeline_PostRewrite(System.Web.IHttpModule sender, System.Web.HttpContext context, Configuration.IUrlEventArgs e)
        {
            //Only work with database images
            //If the data is untrusted, always re-encode each file.
            if (UntrustedData && Belongs(e.VirtualPath))
                e.QueryString["process"] = ImageResizer.ProcessWhen.Always.ToString();

        }

        void Pipeline_RewriteDefaults(System.Web.IHttpModule sender, System.Web.HttpContext context, Configuration.IUrlEventArgs e)
        {
            //Only works with blob provided files
            //Non-images will be served as-is
            //Cache all file types, whether they are processed or not.
            if (CacheUnmodifiedFiles && Belongs(e.VirtualPath))
                e.QueryString["cache"] = ServerCacheMode.Always.ToString();

        }

        void Pipeline_PostAuthorizeRequestStart(System.Web.IHttpModule sender, System.Web.HttpContext context)
        {
            //Only work with blob images
            //This allows us to resize database images without putting ".jpg" after the ID in the path.
            if ((!RequireImageExtension || UntrustedData) && Belongs(c.Pipeline.PreRewritePath))
                c.Pipeline.SkipFileTypeCheck = true; //Skip the file extension check. FakeExtensions will still be stripped.
         
        }

        public virtual bool Uninstall(Configuration.Config c)
        {
            c.Pipeline.PostAuthorizeRequestStart -= Pipeline_PostAuthorizeRequestStart;
            c.Pipeline.RewriteDefaults -= Pipeline_RewriteDefaults;
            c.Pipeline.PostRewrite -= Pipeline_PostRewrite;

            c.Plugins.remove_plugin(this);
            
            return true;
        }

        protected void ReportReadTicks(long ticks, long bytes)
        {
            Configuration.Performance.GlobalPerf.BlobRead(this.c, ticks,  bytes);
        }

        public IEnumerable<KeyValuePair<string, string>> GetInfoPairs()
        {
            return new KeyValuePair<string, string>[]
            {
                new KeyValuePair<string, string>("provider_prefix", VirtualFilesystemPrefix),
                new KeyValuePair<string, string>("provider_flags", 
                  string.Join(",", new [] {ExposeAsVpp, CacheUnmodifiedFiles, UntrustedData, CheckForModifiedFiles, RequireImageExtension, LazyExistenceCheck, CacheMetadata}
                  .Select(b => b ? "1" : "0")))
            };
        }
    }
}
