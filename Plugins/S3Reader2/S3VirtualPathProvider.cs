/* Copyright (c) 2011 Nathanael Jones. See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Hosting;
using System.Security.Permissions;
using System.Web.Caching;
using System.IO;
using System.Configuration;
using ImageResizer.Util;
using Amazon.S3;
namespace ImageResizer.Plugins.S3Reader2
{
    
    /// <summary>
    /// Allows clients to request objects located on another amazon S3 server through this server. Allows URL rewriting.
    /// </summary>
    public class S3VirtualPathProvider : VirtualPathProvider, IVirtualImageProvider
    {

        public delegate void RewriteBucketAndKeyPath(S3VirtualPathProvider sender, S3PathEventArgs e);

        /// <summary>
        /// Important! You should handle this event and throw an exception if a bucket that you do not own is requested. Otherwise other people's buckets could be accessed using your server.
        /// </summary>
        public event RewriteBucketAndKeyPath PreS3RequestFilter;

        /// <summary>
        /// Execites the PreS3RequestFilter event and returns the result.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public string FilterPath(string path)
        {
            S3PathEventArgs e = new S3PathEventArgs(path);
            if (PreS3RequestFilter != null) PreS3RequestFilter(this,e);
            return e.Path;
        }

        private string _virtualFilesystemPrefix = "~/s3/";
        /// <summary>
        /// Requests starting with this path will be handled by this virtual path provider. Should be in app-relative form: "~/s3/". Will be converted to root-relative form upon assigment. Trailing slash required, auto-added.
        /// </summary>
        public string VirtualFilesystemPrefix
        {
            get { return _virtualFilesystemPrefix; }
            set { if (!value.EndsWith("/")) value += "/";  _virtualFilesystemPrefix = PathUtils.ResolveAppRelativeAssumeAppRelative(value); }
        }
        private TimeSpan _metadataAbsoluteExpiration = TimeSpan.MaxValue;
        /// <summary>
        /// Existence and modified date metadata about files is cached for, at longest, this amount of time after it is first stored.
        /// </summary>
        public TimeSpan MetadataAbsoluteExpiration
        {
            get
            {
                return _metadataAbsoluteExpiration;//1 hr
            }
            set
            {
                if (!(value == TimeSpan.MaxValue || MetadataSlidingExpiration == TimeSpan.Zero)) throw new ArgumentException("MetadataAbsoluteExpiration must be DateTime.MaxValue or MetadataSlidingExpiration must be timeSpan.Zero.");
                _metadataAbsoluteExpiration = value;
            }
        }
        private TimeSpan _metadataSlidingExpiration = new TimeSpan(0, 1, 0, 0); //one hour
        /// <summary>
        /// Existence and modified date metadata about files is cached for this long after it is last accessed.
        /// </summary>
        public TimeSpan MetadataSlidingExpiration
        {
            get
            {
                return _metadataSlidingExpiration;//1 hr
            }
            set
            {
                if (!(MetadataAbsoluteExpiration == TimeSpan.MaxValue || value == TimeSpan.Zero)) throw new ArgumentException("MetadataAbsoluteExpiration must be DateTime.MaxValue or MetadataSlidingExpiration must be timeSpan.Zero.");
                _metadataSlidingExpiration = value;
            }

        }

   
        private AmazonS3Client s3Client = null;
        /// <summary>
        /// Gets and sets the AmazonS3Client object that specifies connection details such as authentication, encryption, etc.
        /// </summary>
        public AmazonS3Client S3Client
        {
            get { return s3Client; }
            set { s3Client = value; }
        }

        private bool _fastMode = true;
        /// <summary>
        /// If true, existence of bucket and key is assumed as long as prefix is present.
        /// Defaults to true. Also, no modified date information is provided, so the cache never gets updated. Requires 1 request instead of 2 to download the image.
        /// </summary>
        public bool FastMode { get { return _fastMode; } set { _fastMode = value; } }
        /// <summary>
        /// Create and configure a virtual path provider. 
        /// </summary>
        /// <param name="s3client">Provide the authentication and ecryption settings. For public buckets, no settings are required. Use AmazonS3Config.CommunicationProtocol=HTTP for best performance. </param>
        /// <param name="virtualFilesystemPrefix">The virtual folder to allow client access of s3 from.</param>
        /// <param name="absoluteMetadataExpiration"></param>
        /// <param name="slidingMetadataExpiration"></param>
        /// <param name="bucketFilterCallback">You should validate that the requested bucket is your own. If you only want one bucket, just prefix your bucket to the path.</param>
        /// <param name="fastMode">If true, existence of bucket and key is assumed as long as prefix is present. Also, no modified date information is provided to the image resizer, so the cache never gets updated. Requires 1 request instead of 2 to download the image.</param>
        public S3VirtualPathProvider(AmazonS3Client s3client, String virtualFilesystemPrefix, TimeSpan absoluteMetadataExpiration, TimeSpan slidingMetadataExpiration, RewriteBucketAndKeyPath bucketFilterCallback, Boolean fastMode)
            : base()
        {
            this.s3Client = s3client;
            this.VirtualFilesystemPrefix = virtualFilesystemPrefix;
            this.MetadataAbsoluteExpiration = absoluteMetadataExpiration;
            this.MetadataSlidingExpiration = slidingMetadataExpiration;
            this.PreS3RequestFilter += bucketFilterCallback;
            this.FastMode = fastMode;
        }

        /// <summary>
        /// Default settings: CommunicationProtocol=HTTP, VirtualFileSystemPrefix = "~/s3", SlidingExpiration = 1h, AbsoluteExpiration = maxvalue
        /// No bucket filtering is performed, so any amazon-hosted bucket can be accessed through this provider unless you add a bucket filter.
        /// </summary>
        /// <param name="bucketFilterCallback">You should validate that the requested bucket is your own. If you only want one bucket, just prefix your bucket to the path.</param>
        /// <param name="fastMode">If true, existence of bucket and key is assumed as long as prefix is present. Also, no modified date information is provided to the image resizer, so the cache never gets updated. Requires 1 request instead of 2 to download the image.</param>
        public S3VirtualPathProvider(RewriteBucketAndKeyPath bucketFilterCallback, Boolean fastMode)
            : base()
        {
            this.s3Client = new AmazonS3Client(null,new AmazonS3Config() { UseHttp = true });
            this.PreS3RequestFilter += bucketFilterCallback;
            this.FastMode = fastMode;
        }
  
        protected override void Initialize()
        {

        }

        /// <summary>
        ///   Determines whether a specified virtual path is within
        ///   the virtual file system.
        /// </summary>
        /// <param name="virtualPath">An absolute virtual path.</param>
        /// <returns>
        ///   true if the virtual path is within the 
        ///   virtual file sytem; otherwise, false.
        /// </returns>
        public bool IsPathVirtual(string virtualPath)
        {
            return virtualPath.StartsWith(VirtualFilesystemPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public override bool FileExists(string virtualPath)
        {
            if (IsPathVirtual(virtualPath))
            {
                return new S3File(virtualPath, this).Exists;
            }
            else
                return Previous.FileExists(virtualPath);
        }


        public override VirtualFile GetFile(string virtualPath)
        {
            if (IsPathVirtual(virtualPath))

                return new S3File(virtualPath, this);
            else
                return Previous.GetFile(virtualPath);
        }

        /**
         * Since we can't watch S3. If you want to invalidate the cache through code or sql, modify this class. For the ASP.NET cache, not the image resizer disk cache.
         */
        private class EmptyCacheDependency : CacheDependency
        {
            public EmptyCacheDependency()
            {
            }
        }
        
        public override CacheDependency GetCacheDependency(
          string virtualPath,
          System.Collections.IEnumerable virtualPathDependencies,
          DateTime utcStart)
        {
            if (IsPathVirtual(virtualPath))
                return new EmptyCacheDependency();
            else
                return Previous.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
        }




        public bool FileExists(string virtualPath, System.Collections.Specialized.NameValueCollection queryString) {
            return IsPathVirtual(virtualPath) && new S3File(virtualPath, this).Exists;
        }

        public IVirtualFile GetFile(string virtualPath, System.Collections.Specialized.NameValueCollection queryString) {
            return (IsPathVirtual(virtualPath)) ? new S3File(virtualPath, this) : null;
        }
    }


    
}