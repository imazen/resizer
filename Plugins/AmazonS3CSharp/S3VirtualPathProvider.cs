/**
 * Written by Nathanael Jones 
 * http://nathanaeljones.com
 * nathanael.jones@gmail.com
 * 
 * Although I typically release my components for free, I decided to charge a 
 * 'download fee' for this one to help support my other open-source projects. 
 * Don't worry, this component is still open-source, and the license permits 
 * source redistribution as part of a larger system. However, I'm asking that 
 * people who want to integrate this component purchase the download instead 
 * of ripping it out of another open-source project. My free to non-free LOC 
 * (lines of code) ratio is still over 40 to 1, and I plan on keeping it that 
 * way. I trust this will keep everybody happy.
 * 
 * By purchasing the download, you are permitted to 
 * 
 * 1) Modify and use the component in all of your projects. 
 * 
 * 2) Redistribute the source code as part of another project, provided 
 * the component is less than 5% of the project (in lines of code), 
 * and you keep this information attached.
 * 
 * 3) If you received the source code as part of another open source project, 
 * you cannot extract it (by itself) for use in another project without purchasing a download 
 * from http://nathanaeljones.com/. If nathanaeljones.com is no longer running, and a download
 * cannot be purchased, then you may extract the code.
 * 
 * Disclaimer of warranty and limitation of liability continued at http://nathanaeljones.com/11151_Image_Resizer_License
 **/

using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Hosting;
using System.Security.Permissions;
using System.Web.Caching;
using System.IO;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Configuration;
using LitS3;
namespace fbs.ImageResizer
{
    /// <summary>
    /// Class to allow modifying the bucket and key request path
    /// </summary>
    public class S3PathEventArgs : EventArgs
    {
        protected string _path;
        /// <summary>
        /// Create a new instance of S3PathEventArgs for modifying the bucket and key of incoming requests
        /// </summary>
        /// <param name="path"></param>
        public S3PathEventArgs(string path)
        {
            _path = path;
        }
        /// <summary>
        /// Path contains the bucket and key in the form "bucket/key".
        /// Where key may contain additional forward slashes.
        /// </summary>
        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }
        /// <summary>
        /// Stops the request if it doesn't match one of the allowed buckets.
        /// Amazon S3 is case-sensitive. Thus, comparisons are case sensitive as well. To minimize headache, a different exception is thrown when only case differs.
        /// </summary>
        /// <param name="buckets"></param>
        public void AssertBucketMatches(params string[] buckets)
        {
            bool found = false;
            string b = Bucket;
            //Look for exact match
            foreach (string s in buckets) if (s.Equals(b)) { found = true; break; }
            //If no exact matches, but a case-insensitive match exists, tell the user
            if (!found) foreach (string s in buckets) if (s.Equals(b, StringComparison.InvariantCultureIgnoreCase)) { ThrowInvalidCaseException(); }
            //Throw exception if needed
            if (!found) ThrowException();
        }

        /// <summary>
        /// Throws an HTTP 403 Not Authorized exception. Call this if an invalid bucket request comes through.
        /// </summary>
        public void ThrowException()
        {
            throw new HttpException(403, "You are not authorized to access files from this amazon S3 bucket.");
        }

        /// <summary>
        /// Like ThrowException, but hints that the bucket casing is wrong.
        /// </summary>
        public void ThrowInvalidCaseException()
        {
            throw new HttpException(403, "Amazon S3 is case sensitive. Check your requested bucket name to verify correct casing.");
        }

        /// <summary>
        /// Prefixes the specified bucket to the requested path.
        /// </summary>
        /// <param name="bucket"></param>
        public void PrefixBucket(String bucket)
        {
            bucket = bucket.Trim('\\', '/');
            Path = bucket + "/" + Path.TrimStart('/');
        }
        /// <summary>
        /// Parses the bucket from Path
        /// </summary>
        /// <returns></returns>
        public string Bucket
        {
            get {
                //strip leading slashes
                string path = Path.TrimStart(new char[] { '/', '\\' });

                int keyStartsAt = path.IndexOf('/');
                if (keyStartsAt < 0) return path; //No key present
                //Get bucket
                return path.Substring(0, keyStartsAt);
            }
        }
        /// <summary>
        /// Parses the key from Path
        /// </summary>
        /// <returns></returns>
        public string Key
        {
            get
            {   
                //strip leading slashes
                string path = Path.TrimStart(new char[] { '/', '\\' });

                int keyStartsAt = path.IndexOf('/');
                if (keyStartsAt < 0) return null; //no key

                //Get key
                return path.Substring(keyStartsAt + 1).TrimStart(new char[] { '/', '\\' });
            }
        }
    }

    /// <summary>
    /// Allows clients to request objects located on another amazon S3 server through this server. Allows URL rewriting.
    /// </summary>
    [AspNetHostingPermission(SecurityAction.Demand, Level = AspNetHostingPermissionLevel.Medium)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.High)]
    public class S3VirtualPathProvider : VirtualPathProvider
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
        /// Requests starting with this path will be handled by this virtual path provider. Must be in app-relative form: "~/s3/"
        /// </summary>
        public string VirtualFilesystemPrefix
        {
            get { return _virtualFilesystemPrefix; }
            set { _virtualFilesystemPrefix = value; }
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

   
        private LitS3.S3Service service = null;
        /// <summary>
        /// The S3Service object that specifies connection details such as authentication, encryption, etc.
        /// </summary>
        public S3Service Service
        {
            get { return service; }
            set { service = value; }
        }

        private bool _fastMode = true;
        /// <summary>
        /// If true, existence of bucket and key is assumed as long as prefix is present. Also, no modified date information is provided to the image resizer, so the cache never gets updated. Requires 1 request instead of 2 to download the image.
        /// </summary>
        public bool FastMode { get { return _fastMode; } set { _fastMode = value; } }
        /// <summary>
        /// Create and configure a virtual path provider. 
        /// </summary>
        /// <param name="service">Provide the authentication and ecryption settings. For public buckets, no settings are required. UseSSL should be false for best performance. </param>
        /// <param name="virtualFilesystemPrefix">The virtual folder to allow client access of s3 from.</param>
        /// <param name="absoluteMetadataExpiration"></param>
        /// <param name="slidingMetadataExpiration"></param>
        /// <param name="bucketFilterCallback">You should validate that the requested bucket is your own. If you only want one bucket, just prefix your bucket to the path.</param>
        /// <param name="fastMode">If true, existence of bucket and key is assumed as long as prefix is present. Also, no modified date information is provided to the image resizer, so the cache never gets updated. Requires 1 request instead of 2 to download the image.</param>
        public S3VirtualPathProvider(S3Service service, String virtualFilesystemPrefix, TimeSpan absoluteMetadataExpiration, TimeSpan slidingMetadataExpiration, RewriteBucketAndKeyPath bucketFilterCallback, Boolean fastMode)
            : base()
        {
            this.service = service;
            this.VirtualFilesystemPrefix = virtualFilesystemPrefix;
            this.MetadataAbsoluteExpiration = absoluteMetadataExpiration;
            this.MetadataSlidingExpiration = slidingMetadataExpiration;
            this.PreS3RequestFilter += bucketFilterCallback;
            this.FastMode = fastMode;
        }

        /// <summary>
        /// Default settings: UseSSL=false, VirtualFileSystemPrefix = "~/s3", SlidingExpiration = 1h, AbsoluteExpiration = maxvalue
        /// No bucket filtering is performed, so any amazon-hosted bucket can be accessed through this provider unless you add a bucket filter.
        /// </summary>
        /// <param name="bucketFilterCallback">You should validate that the requested bucket is your own. If you only want one bucket, just prefix your bucket to the path.</param>
        /// <param name="fastMode">If true, existence of bucket and key is assumed as long as prefix is present. Also, no modified date information is provided to the image resizer, so the cache never gets updated. Requires 1 request instead of 2 to download the image.</param>
        public S3VirtualPathProvider(RewriteBucketAndKeyPath bucketFilterCallback, Boolean fastMode)
            : base()
        {
            this.service = new S3Service();
            this.service.UseSsl = false;
            this.PreS3RequestFilter += bucketFilterCallback;
            this.FastMode = fastMode;
        }
  
        protected override void Initialize()
        {

        }
        /// <summary>
        /// Adds a hook to the image resizer (an event handler for RewriteDefaults) that sets useresizingpipeline=true for all requests that would hit this VPP
        /// </summary>
        /// <returns></returns>
        public S3VirtualPathProvider AddDiskCachingHook()
        {
            S3VirtualPathProvider me = this;
            //Force all s3 files to go through the resizing pipeline and get cached. The VirtualPathProvider sends a bitmap to the resizer - very little overhead doing it this way.
            fbs.ImageResizer.InterceptModule.RewriteDefaults += delegate(fbs.ImageResizer.InterceptModule s, fbs.ImageResizer.UrlEventArgs ev)
            {
                if (me.IsPathVirtual(ev.VirtualPath)) ev.QueryString["useresizingpipeline"] = "true";
            };
            return this;
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
            return (VirtualPathUtility.ToAppRelative(virtualPath).StartsWith(VirtualFilesystemPrefix, StringComparison.InvariantCultureIgnoreCase));
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



    }


    [AspNetHostingPermission(SecurityAction.Demand, Level = AspNetHostingPermissionLevel.Minimal)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
    
    public class S3File : VirtualFile , fbs.ImageResizer.IVirtualFileWithModifiedDate 
    {
        private string bucket;
        private string key;
        private S3VirtualPathProvider provider;

        private Nullable<bool> _exists = null;
        private Nullable<DateTime> _fileModifiedDate = null;

        /// <summary>
        /// Updates the exists and modified date
        /// </summary>
        public void UpdateMetadata()
        {
            //Try to pull it out of the .net cache
            string ckey = provider.VirtualFilesystemPrefix + "/" + bucket + "/" + key; 
            Cache c = HttpContext.Current.Cache;
            object o = c.Get(ckey);
            if (o is DateTime)
            {
                _exists = true;
                _fileModifiedDate = (Nullable<DateTime>)o;
            }
            else if (o is bool)
            {
                _exists = false;
                _fileModifiedDate = null;
            }
            else
            {
                //Looks like we have to execute a head request
                var request = new GetObjectRequest(provider.Service, bucket, key,true);
                try
                {
                    using (GetObjectResponse response = request.GetResponse()){
                        //Exists.
                        _exists = true;
                        _fileModifiedDate = response.LastModified;
                    }
                }
                catch (System.Net.WebException exception)
                {
                    var response = exception.Response as System.Net.HttpWebResponse;
                    if (response != null && response.StatusCode == System.Net.HttpStatusCode.NotFound){
                        //Doesn't exist
                        _exists = false;
                        _fileModifiedDate = null;
                    }else
                        throw;
                }
                //Now, save to the .net cache
                object obj = (_fileModifiedDate == null) ? (object)false : (object)_fileModifiedDate.Value;
                //If MetadataAbsoluteExpiration is MaxValue, use DateTime.MaxValue.
                c.Insert(ckey, obj,null, provider.MetadataAbsoluteExpiration == TimeSpan.MaxValue ?  DateTime.MaxValue : DateTime.UtcNow.Add(provider.MetadataAbsoluteExpiration), provider.MetadataSlidingExpiration);

            }
        }

        /// <summary>
        /// Returns true if the row exists. 
        /// </summary>
        public bool Exists
        {
            get {
                if (_exists == null && provider.FastMode) return true; //Assume it exists in fast mode.
                if (_exists == null) UpdateMetadata();
                return _exists.Value;
            }
        }

        public S3File(string virtualPath, S3VirtualPathProvider provider)
            : base(virtualPath)
        {
            this.provider = provider;
            //Must be inside virtual filesystem folder
            if (!provider.IsPathVirtual(virtualPath)) throw new ArgumentException("S3 file path must be located within " + provider.VirtualFilesystemPrefix);
            
            /////////// Parse path into bucket and key

            //Strip prefix
            String path = VirtualPathUtility.ToAppRelative(VirtualPath).Substring(provider.VirtualFilesystemPrefix.Length);

            //strip leading slashes
            path = path.TrimStart(new char[] { '/', '\\' });

            //Now execute filter!
            path = provider.FilterPath(path);

            //strip leading slashes again
            path = path.TrimStart(new char[] { '/', '\\' });
            

            int keyStartsAt = path.IndexOf('/');
            if (keyStartsAt < 0) throw new ArgumentException("S3 file path must specify a bucket" + path);
            //Get bucket
            bucket = path.Substring(0, keyStartsAt);
            //Get key
            key = path.Substring(keyStartsAt + 1).TrimStart(new char[] { '/', '\\' });
        }



        /// <summary>
        /// Returns a stream to the database blob associated with the id
        /// </summary>
        /// <returns></returns>
        public override Stream Open() { 
            MemoryStream ms = new MemoryStream(4096); //4kb is a good starting point.
            //Synchronously download
            try
            {
                provider.Service.GetObject(bucket, key, ms);
            }catch (S3Exception se){
                if (se.ErrorCode == S3ErrorCode.NoSuchKey) throw new HttpException(404,"S3 File not found",se);
                else if (se.ErrorCode == S3ErrorCode.AccessDenied) throw new HttpException(403,"Access Denied - file may not exist",se);
            }
            ms.Seek(0, SeekOrigin.Begin); //Reset to beginning
			//TODO: handle file not found error, throw cachable exception
            return ms;
        }

        /// <summary>
        /// Returns the last modified date of the row. Cached for performance.
        /// </summary>
        public DateTime ModifiedDateUTC{
            get{
                if (_fileModifiedDate == null && provider.FastMode) return DateTime.MinValue; //In fast mode, Return flag value that means no info available.
                if (_fileModifiedDate == null) UpdateMetadata();
                return _fileModifiedDate.Value;
            }
        }
      
    }
}