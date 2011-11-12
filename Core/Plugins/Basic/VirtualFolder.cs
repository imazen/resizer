using System;
using System.Collections.Generic;
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

namespace ImageResizer.Plugins.Basic {
    /// <summary>
    /// Functions exactly like an IIS virtual folder, but doesn't require IIS configuration.
    /// </summary>
    [AspNetHostingPermission(SecurityAction.Demand, Level = AspNetHostingPermissionLevel.Medium)]
    [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Medium)]
    public class VirtualFolder : VirtualPathProvider, IPlugin , IMultiInstancePlugin{

        public VirtualFolder(string virtualPath, string physicalPath)
            : base() {
            this.VirtualPath = virtualPath;
            this.PhysicalPath = physicalPath;
        }

        public VirtualFolder(NameValueCollection args)
            : base() {
            this.VirtualPath = args["virtualPath"];
            this.PhysicalPath = args["physicalPath"];
        }



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


        /// <summary>
        /// Registers the VirtualFolder plugin as a virtual path provider.
        /// </summary>
        /// <returns></returns>
        public IPlugin Install(Config c) {
            HostingEnvironment.RegisterVirtualPathProvider(this);
            c.Plugins.add_plugin(this);
            return this;
        }
        public bool Uninstall(Config c) {
            c.Plugins.remove_plugin(this);
            return false;//Cannot truly remove a VPP
        }





        protected string normalizeVirtualPath(string path) {
            if (!path.StartsWith("/")) path = HostingEnvironment.ApplicationVirtualPath.TrimEnd('/') + '/' + path.Substring(1).TrimStart('/');
            return path;
        }

        protected string resolvePhysicalPath(string path) {
            
            if (!Path.IsPathRooted(path)) path = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, path);
            try {
                return Path.GetFullPath(path);
            } catch (SecurityException) { //TODO: provide alterante implementation that is medium-trust friendly, or maybe just throw the error that we don't have permissions to call File.Open anyway? 
                return collapsePath(path);
            }
        }

        protected string collapsePath(string path){
            string oldPath = path;
            do{
                oldPath = path;
                path = collapseOneLevel(oldPath);
            }while(oldPath != path);
            return path;
        }

        protected string collapseOneLevel(string path) {
            int up = path.IndexOf(Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar);
            if (up < 0) return path;
            int prevSlash = path.LastIndexOf(Path.DirectorySeparatorChar, up - 1);
            if (prevSlash > -1) {
                return path.Substring(0, prevSlash) + path.Substring(up + 3);
            }
            return path;
        }

        public string virtualToPhysical(string virtualPath) {
            virtualPath = normalizeVirtualPath(virtualPath);
            if (virtualPath.StartsWith(this.VirtualPath, StringComparison.OrdinalIgnoreCase)) {
                return Path.Combine(PhysicalPath, virtualPath.Substring(this.VirtualPath.Length).TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            }
            return null;
        }

        public bool isVirtualPath(string virtualPath) {
            virtualPath = normalizeVirtualPath(virtualPath);
            return virtualPath.StartsWith(this.VirtualPath, StringComparison.OrdinalIgnoreCase);
        }
        public bool isOnlyVirtualPath(string virtualPath) {
            if (Previous.FileExists(virtualPath)) return false;
            return isVirtualPath(virtualPath);
        }



        public Stream getStream(string virtualPath) {
            if (isOnlyVirtualPath(virtualPath))
                return File.Open(virtualToPhysical(virtualPath), FileMode.Open, FileAccess.Read, FileShare.Read);
            return null;
        }

        public DateTime getDateModifiedUtc(string virtualPath) {
            string physicalPath = virtualToPhysical(virtualPath);
            if (System.IO.File.Exists(physicalPath))
                return System.IO.File.GetLastWriteTimeUtc(physicalPath);
            else return DateTime.MinValue;
        }



        public override bool FileExists(string virtualPath) {
            if (isOnlyVirtualPath(virtualPath))
                return File.Exists(virtualToPhysical(virtualPath));
            else
                return Previous.FileExists(virtualPath);
        }

        public override VirtualFile GetFile(string virtualPath) {
            if (isOnlyVirtualPath(virtualPath))
                return new VirtualFolderProviderVirtualFile(virtualPath, this);
            else
                return Previous.GetFile(virtualPath);
        }

        public override CacheDependency GetCacheDependency(
          string virtualPath,
          System.Collections.IEnumerable virtualPathDependencies,
          DateTime utcStart) {

              if (!isOnlyVirtualPath(virtualPath)) return Previous.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);

            List<String> deps = new List<string>();
            foreach (string s in virtualPathDependencies) {
                deps.Add(s);
            }


             return new CacheDependency(new string[] { virtualToPhysical(virtualPath) }, deps.ToArray(), utcStart);

        }

        [AspNetHostingPermission(SecurityAction.Demand, Level = AspNetHostingPermissionLevel.Minimal)]
        [AspNetHostingPermission(SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)]
        public class VirtualFolderProviderVirtualFile : VirtualFile, IVirtualFileWithModifiedDate, IVirtualFile {

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
                    if (_fileModifiedDate == null) _fileModifiedDate = provider.getDateModifiedUtc(this.VirtualPath);
                    return _fileModifiedDate.Value;
                }
            }

        }
    }

}

