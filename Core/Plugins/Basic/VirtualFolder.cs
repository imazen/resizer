using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Web;
using System.Security.Permissions;
using System.Web.Hosting;
using ImageResizer.Plugins;
using ImageResizer.Configuration;
using System.IO;
using System.Web.Caching;
using System.Collections.Specialized;
using System.Security;
using ImageResizer.Configuration.Issues;
using ImageResizer.Util;
using ImageResizer.ExtensionMethods;
using System.Threading.Tasks;

namespace ImageResizer.Plugins.Basic {
    /// <summary>
    /// Functions exactly like an IIS virtual folder, but doesn't require IIS configuration.
    /// </summary>
    public class VirtualFolder : VirtualPathProvider, IVirtualImageProviderAsync, IVirtualImageProvider, IPlugin , IMultiInstancePlugin, IIssueProvider{

        public VirtualFolder(string virtualPath, string physicalPath)
            : this(virtualPath,physicalPath,true) {
        }
        public VirtualFolder(string virtualPath, string physicalPath, bool registerAsVpp)
            : base() {
            this.VirtualPath = virtualPath;
            this.PhysicalPath = physicalPath;
            this.RegisterAsVpp = registerAsVpp;
        }

        public VirtualFolder(NameValueCollection args)
            : base() {
            this.VirtualPath = args["virtualPath"];
            this.PhysicalPath = args["physicalPath"];
            this.RegisterAsVpp = args.Get("vpp", true);
        }



        private bool _failedToRegisterVpp = false;
        /// <summary>
        /// True if the provider attempted to register itself as a VirtualPathProvider and failed due to limited security clearance.
        /// False if it did not attempt (say, due to missing IOPermission) , or if it succeeded.
        /// </summary>
        public bool FailedToRegisterVpp {
            get { return _failedToRegisterVpp; }
        }

        /// <summary>
        /// If true, the plugin will attempt to register itself as an application-wide VirtualPathProvider instead of a image resizer-specific IVirtualImageProvider.
        /// </summary>
        public bool RegisterAsVpp { get; set; }

        private string virtualPath = null;
        /// <summary>
        /// The virtual path served by the VirtualFolder
        /// </summary>
        public string VirtualPath {
            get { return virtualPath; }
            set { virtualPath = normalizeVirtualPath(value); }
        }

        private string physicalPath = null;
        /// <summary>
        /// The physical path
        /// </summary>
        public string PhysicalPath {
            get { return physicalPath; }
            set { physicalPath = resolvePhysicalPath(value); }
        }


        private bool registeredVpp = false;
        /// <summary>
        /// Registers the VirtualFolder plugin as a virtual path provider.
        /// </summary>
        /// <returns></returns>
        public IPlugin Install(Config c) {
            if (!NoIOPermission && RegisterAsVpp) {
                try {
                    HostingEnvironment.RegisterVirtualPathProvider(this);
                    registeredVpp = true;
                } catch (SecurityException) {
                    this._failedToRegisterVpp =true;
                }
            }
            c.Plugins.add_plugin(this);
            return this;
        }
        public bool Uninstall(Config c) {
            c.Plugins.remove_plugin(this);
            return !registeredVpp;//Cannot truly remove a VPP
        }

        private bool noIOPermission = false;
        /// <summary>
        /// True if the plugin has detected it doesn't have sufficient IOPermission to operate.
        /// </summary>
        public bool NoIOPermission {
            get { return noIOPermission; }
        }

        /// <summary>
        /// Converts relative and app-relative paths to domain-relative virtual paths.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        protected string normalizeVirtualPath(string path) {
            if (!path.StartsWith("/")) path = HostingEnvironment.ApplicationVirtualPath.TrimEnd('/') + '/' + (path.StartsWith("~") ? path.Substring(1) : path).TrimStart('/');
            return path;
        }
        /// <summary>
        /// Attempts to convert a phyiscal path into a collapsed rooted physical path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        protected string resolvePhysicalPath(string path) {

            if (!Path.IsPathRooted(path) && HostingEnvironment.ApplicationPhysicalPath != null) path = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, path);
            if (NoIOPermission) return collapsePath(path);

            try {
                return Path.GetFullPath(path);
            } catch (SecurityException) {
                noIOPermission = true;
                return collapsePath(path);
            }
        }
        /// <summary>
        /// Collapses any .. segments
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        protected string collapsePath(string path) {
            string oldPath = path;
            do {
                oldPath = path;
                path = collapseOneLevel(oldPath);
            } while (oldPath != path);
            return path;
        }

        protected string collapseOneLevel(string path) {
            int up = path.Length - 1;
            do {
                up = path.LastIndexOf(System.IO.Path.DirectorySeparatorChar + ".." + System.IO.Path.DirectorySeparatorChar, up);
                if (up < 0) return path;
                int prevSlash = path.LastIndexOf(System.IO.Path.DirectorySeparatorChar, up - 1);
                if (prevSlash < 0) return path;
                string segment = path.Substring(prevSlash + 1, up - prevSlash - 1);
                if (segment.Equals("..", StringComparison.OrdinalIgnoreCase)) {
                    //We can't combine \..\..\, just keep looking closer to the beginning of the string. We already adjusted 'up'
                } else if (segment.Equals(".", StringComparison.OrdinalIgnoreCase)) {
                    return path.Substring(0, prevSlash) + path.Substring(up); //Just remove \.\ sections
                } else {
                    return path.Substring(0, prevSlash) + path.Substring(up + 3); //If it's not \.\ or \..\, remove both it and the following \..\
                }
            } while (up > 0);
            return path;
        }

        public string VirtualToPhysical(string virtualPath)
        {
            return LocalMapPath(virtualPath);
        }
        /// <summary>
        /// Converts any virtual path in this folder to a physical path. Returns null if the virtual path is outside the folder.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public string LocalMapPath(string virtualPath) {
            virtualPath = normalizeVirtualPath(virtualPath);
            if (virtualPath.StartsWith(this.VirtualPath, StringComparison.OrdinalIgnoreCase)) {
                return Path.Combine(PhysicalPath, virtualPath.Substring(this.VirtualPath.Length).TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            }
            return null;
        }
        /// <summary>
        /// Returns true if the specified path is inside this virtual folder
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public bool IsVirtualPath(string virtualPath) {
            virtualPath = normalizeVirtualPath(virtualPath);
            return virtualPath.StartsWith(this.VirtualPath, StringComparison.OrdinalIgnoreCase);
        }
        /// <summary>
        /// Returns true if (a) we have IOPermission, (b) the path is under our prefix, (c) the 
        /// expected physical file does not exist (because we shouldn't interfere in that case), and
        /// (d) the other VPPs don't believe the file exists.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        private bool isOnlyVirtualPath(string virtualPath) {
            if (NoIOPermission) return false; //Don't act as a VPP if we don't have permission to operate.
            if (!IsVirtualPath(virtualPath)) return false;
            if (File.Exists(HostingEnvironment.MapPath(normalizeVirtualPath(virtualPath)))) return false;
            if (registeredVpp && Previous.FileExists(virtualPath)) return false;
            return true;
        }



        internal protected Stream getStream(string virtualPath) {
            if (NoIOPermission || !IsVirtualPath(virtualPath)) return null;
            return File.Open(LocalMapPath(virtualPath), FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        /// <summary>
        /// Returns the LastWriteTimeUtc value for the specified virtual file in this folder, or DateTime.MinValue if missing.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public DateTime GetDateModifiedUtc(string virtualPath) {
            string physicalPath = LocalMapPath(virtualPath);
            if (System.IO.File.Exists(physicalPath))
                return System.IO.File.GetLastWriteTimeUtc(physicalPath);
            else return DateTime.MinValue;
        }

        /// <summary>
        /// Returns true if the file exists in this virtual folder, and would not be masking an existing file.
        /// Returns false if NoIOPermission is true.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public bool FileExists(string virtualPath, NameValueCollection queryString) {
            if (NoIOPermission) return false; //Because File.Exists is always false when IOPermission is missing, anyhow.
            if (!IsVirtualPath(virtualPath)) return false; //It's not even in our area.
            if (File.Exists(LocalMapPath(virtualPath))){
                //Ok, we could serve it, but existing files take precedence.
                //Return false if we would be masking an existing file.
                return !File.Exists(HostingEnvironment.MapPath(normalizeVirtualPath(virtualPath)));
            }
            return false;
        }
        /// <summary>
        /// Unless the path is not within the virtual folder, or IO permissions are missing, will return an IVirtualFile instance for the path. 
        /// The file may or may not exist.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public IVirtualFile GetFile(string virtualPath, NameValueCollection queryString) {
            if (NoIOPermission) return null;
            if (!IsVirtualPath(virtualPath)) return null; //It's not even in our area.
            return new VirtualFolderProviderVirtualFile(virtualPath, this);
        }

        /// <summary>
        /// For internal use only by the .NET VPP system.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public override bool FileExists(string virtualPath) {
            if (isOnlyVirtualPath(virtualPath))
                return File.Exists(LocalMapPath(virtualPath));
            else
                return Previous.FileExists(virtualPath);
        }
        /// <summary>
        /// For internal use only by the .NET VPP system.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public override VirtualFile GetFile(string virtualPath) {
            if (isOnlyVirtualPath(virtualPath))
                return new VirtualFolderProviderVirtualFile(virtualPath, this);
            else
                return Previous.GetFile(virtualPath);
        }
        /// <summary>
        /// For internal use only by the .NET VPP system.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="virtualPathDependencies"></param>
        /// <param name="utcStart"></param>
        /// <returns></returns>
        public override CacheDependency GetCacheDependency(
          string virtualPath,
          System.Collections.IEnumerable virtualPathDependencies,
          DateTime utcStart) {

              if (!isOnlyVirtualPath(virtualPath)) return Previous.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);

            List<String> deps = new List<string>();
            foreach (string s in virtualPathDependencies) {
                deps.Add(s);
            }


             return new CacheDependency(new string[] { LocalMapPath(virtualPath) }, deps.ToArray(), utcStart);

        }

        public class VirtualFolderProviderVirtualFile : VirtualFile, IVirtualFileAsync, IVirtualFileWithModifiedDate, IVirtualFile, IVirtualFileSourceCacheKey{

            private VirtualFolder provider;

            private Nullable<bool> _exists = null;
            private Nullable<DateTime> _fileModifiedDate = null;

            public bool Exists {
                get {
                    if (_exists == null) _exists = provider.FileExists(this.VirtualPath);
                    return _exists.Value;
                }
            }

            public VirtualFolderProviderVirtualFile(string virtualPath, VirtualFolder provider)
                : base(virtualPath) {
                this.provider = provider;
            }

            public override Stream Open() { return provider.getStream(this.VirtualPath); }

            public DateTime ModifiedDateUTC {
                get {
                    if (_fileModifiedDate == null) _fileModifiedDate = provider.GetDateModifiedUtc(this.VirtualPath);
                    return _fileModifiedDate.Value;
                }
            }

            public string GetCacheKey(bool includeModifiedDate) {
                return VirtualPath + (includeModifiedDate ? ("_" + ModifiedDateUTC.Ticks.ToString(CultureInfo.InvariantCulture)) : "");
            }

            public Task<Stream> OpenAsync()
            {
                return Task.FromResult(provider.getStream(this.VirtualPath));
            }
        }

        public IEnumerable<IIssue> GetIssues() {
            List<IIssue> issues = new List<IIssue>();

            if (NoIOPermission) issues.Add(new Issue("The VirtualFolder plugin cannot serve files from \"" +  PhysicalPath + "\" until you increase the trust level or modify the trust configuration to permit read access to the directory.", IssueSeverity.Error));
            if (FailedToRegisterVpp) issues.Add(new Issue("The VirtualFolder plugin failed to register itself as an app-wide VirtualPathProvider. It will only work for images proccessed by the image resizer.", IssueSeverity.Error));


            return issues;
        }



        public Task<bool> FileExistsAsync(string virtualPath, NameValueCollection queryString)
        {
            if (NoIOPermission) return Task.FromResult(false); //Because File.Exists is always false when IOPermission is missing, anyhow.
            if (!IsVirtualPath(virtualPath)) return Task.FromResult(false); //It's not even in our area.
            if (File.Exists(LocalMapPath(virtualPath)))
            {
                //Ok, we could serve it, but existing files take precedence.
                //Return false if we would be masking an existing file.
                return Task.FromResult(!File.Exists(HostingEnvironment.MapPath(normalizeVirtualPath(virtualPath))));
            }
            return Task.FromResult(false);
        }

        public Task<IVirtualFileAsync> GetFileAsync(string virtualPath, NameValueCollection queryString)
        {
            if (NoIOPermission) return Task.FromResult<IVirtualFileAsync>(null);
            if (!IsVirtualPath(virtualPath)) return Task.FromResult<IVirtualFileAsync>(null); //It's not even in our area.
            return Task.FromResult<IVirtualFileAsync>(new VirtualFolderProviderVirtualFile(virtualPath, this));
        }
    }

}

