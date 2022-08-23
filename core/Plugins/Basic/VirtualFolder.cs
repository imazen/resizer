// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Security;
using System.Threading.Tasks;
using System.Web.Caching;
using System.Web.Hosting;
using ImageResizer.Configuration;
using Imazen.Common.Issues;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.Basic
{
    /// <summary>
    ///     Functions exactly like an IIS virtual folder, but doesn't require IIS configuration.
    /// </summary>
    public class VirtualFolder : VirtualPathProvider, IVirtualImageProviderAsync, IVirtualImageProvider, IPlugin,
        IMultiInstancePlugin, IIssueProvider
    {
        public VirtualFolder(string virtualPath, string physicalPath)
            : this(virtualPath, physicalPath, true)
        {
        }

        public VirtualFolder(string virtualPath, string physicalPath, bool registerAsVpp)
            : base()
        {
            VirtualPath = virtualPath;
            PhysicalPath = physicalPath;
            RegisterAsVpp = registerAsVpp;
        }

        public VirtualFolder(NameValueCollection args)
            : base()
        {
            if (string.IsNullOrWhiteSpace(args["virtualPath"]))
                throw new ArgumentException("missing virtualPath attribute on <add name='VirtualPath' ... element");
            if (string.IsNullOrWhiteSpace(args["physicalPath"]))
                throw new ArgumentException("missing physicalPath attribute on <add name='VirtualPath' ... element");
            VirtualPath = args["virtualPath"];
            PhysicalPath = args["physicalPath"];
            RegisterAsVpp = args.Get("vpp", true);
        }


        private bool _failedToRegisterVpp = false;

        /// <summary>
        ///     True if the provider attempted to register itself as a VirtualPathProvider and failed due to limited security
        ///     clearance.
        ///     False if it did not attempt (say, due to missing IOPermission) , or if it succeeded.
        /// </summary>
        public bool FailedToRegisterVpp => _failedToRegisterVpp;

        /// <summary>
        ///     If true, the plugin will attempt to register itself as an application-wide VirtualPathProvider instead of a image
        ///     resizer-specific IVirtualImageProvider.
        /// </summary>
        public bool RegisterAsVpp { get; set; }

        private string virtualPath = null;

        /// <summary>
        ///     The virtual path served by the VirtualFolder
        /// </summary>
        public string VirtualPath
        {
            get => virtualPath;
            set => virtualPath = normalizeVirtualPath(value);
        }

        private string physicalPath = null;

        /// <summary>
        ///     The physical path
        /// </summary>
        public string PhysicalPath
        {
            get => physicalPath;
            set => physicalPath = resolvePhysicalPath(value);
        }


        private bool registeredVpp = false;

        /// <summary>
        ///     Registers the VirtualFolder plugin as a virtual path provider.
        /// </summary>
        /// <returns></returns>
        public IPlugin Install(Config c)
        {
            if (!NoIOPermission && RegisterAsVpp)
                try
                {
                    HostingEnvironment.RegisterVirtualPathProvider(this);
                    registeredVpp = true;
                }
                catch (SecurityException)
                {
                    _failedToRegisterVpp = true;
                }

            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            return !registeredVpp; //Cannot truly remove a VPP
        }

        private bool noIOPermission = false;

        /// <summary>
        ///     True if the plugin has detected it doesn't have sufficient IOPermission to operate.
        /// </summary>
        public bool NoIOPermission => noIOPermission;

        /// <summary>
        ///     Converts relative and app-relative paths to domain-relative virtual paths.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        protected string normalizeVirtualPath(string path)
        {
            if (!path.StartsWith("/"))
                path = HostingEnvironment.ApplicationVirtualPath.TrimEnd('/') + '/' +
                       (path.StartsWith("~") ? path.Substring(1) : path).TrimStart('/');
            return path;
        }

        /// <summary>
        ///     Attempts to convert a physical path into a collapsed rooted physical path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        protected string resolvePhysicalPath(string path)
        {
            if (!Path.IsPathRooted(path) && HostingEnvironment.ApplicationPhysicalPath != null)
                path = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, path);
            if (NoIOPermission) return collapsePath(path);

            try
            {
                return Path.GetFullPath(path);
            }
            catch (SecurityException)
            {
                noIOPermission = true;
                return collapsePath(path);
            }
        }

        /// <summary>
        ///     Collapses any .. segments
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        protected string collapsePath(string path)
        {
            var oldPath = path;
            do
            {
                oldPath = path;
                path = collapseOneLevel(oldPath);
            } while (oldPath != path);

            return path;
        }

        protected string collapseOneLevel(string path)
        {
            var up = path.Length - 1;
            do
            {
                up = path.LastIndexOf(Path.DirectorySeparatorChar + ".." + Path.DirectorySeparatorChar, up);
                if (up < 0) return path;
                var prevSlash = path.LastIndexOf(Path.DirectorySeparatorChar, up - 1);
                if (prevSlash < 0) return path;
                var segment = path.Substring(prevSlash + 1, up - prevSlash - 1);
                if (segment.Equals("..", StringComparison.OrdinalIgnoreCase))
                {
                    //We can't combine \..\..\, just keep looking closer to the beginning of the string. We already adjusted 'up'
                }
                else if (segment.Equals(".", StringComparison.OrdinalIgnoreCase))
                {
                    return path.Substring(0, prevSlash) + path.Substring(up); //Just remove \.\ sections
                }
                else
                {
                    return path.Substring(0, prevSlash) +
                           path.Substring(up + 3); //If it's not \.\ or \..\, remove both it and the following \..\
                }
            } while (up > 0);

            return path;
        }

        public string VirtualToPhysical(string virtualPath)
        {
            return LocalMapPath(virtualPath);
        }

        /// <summary>
        ///     Converts any virtual path in this folder to a physical path. Returns null if the virtual path is outside the
        ///     folder.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public string LocalMapPath(string virtualPath)
        {
            virtualPath = normalizeVirtualPath(virtualPath);
            if (virtualPath.StartsWith(VirtualPath, StringComparison.OrdinalIgnoreCase))
                return Path.Combine(PhysicalPath,
                    virtualPath.Substring(VirtualPath.Length).TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            return null;
        }

        /// <summary>
        ///     Returns true if the specified path is inside this virtual folder
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public bool IsVirtualPath(string virtualPath)
        {
            virtualPath = normalizeVirtualPath(virtualPath);
            return virtualPath.StartsWith(VirtualPath, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        ///     Returns true if (a) we have IOPermission, (b) the path is under our prefix, (c) the
        ///     expected physical file does not exist (because we shouldn't interfere in that case), and
        ///     (d) the other VPPs don't believe the file exists.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        private bool isOnlyVirtualPath(string virtualPath)
        {
            if (NoIOPermission) return false; //Don't act as a VPP if we don't have permission to operate.
            if (!IsVirtualPath(virtualPath)) return false;
            if (File.Exists(HostingEnvironment.MapPath(normalizeVirtualPath(virtualPath)))) return false;
            if (registeredVpp && Previous.FileExists(virtualPath)) return false;
            return true;
        }


        protected internal Stream getStream(string virtualPath)
        {
            if (NoIOPermission || !IsVirtualPath(virtualPath)) return null;
            return File.Open(LocalMapPath(virtualPath), FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        /// <summary>
        ///     Returns the LastWriteTimeUtc value for the specified virtual file in this folder, or DateTime.MinValue if missing.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public DateTime GetDateModifiedUtc(string virtualPath)
        {
            var physicalPath = LocalMapPath(virtualPath);
            if (File.Exists(physicalPath))
                return File.GetLastWriteTimeUtc(physicalPath);
            else return DateTime.MinValue;
        }

        /// <summary>
        ///     Returns true if the file exists in this virtual folder, and would not be masking an existing file.
        ///     Returns false if NoIOPermission is true.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public bool FileExists(string virtualPath, NameValueCollection queryString)
        {
            if (NoIOPermission)
                return false; //Because File.Exists is always false when IOPermission is missing, anyhow.
            if (!IsVirtualPath(virtualPath)) return false; //It's not even in our area.
            if (File.Exists(LocalMapPath(virtualPath)))
                //Ok, we could serve it, but existing files take precedence.
                //Return false if we would be masking an existing file.
                return !File.Exists(HostingEnvironment.MapPath(normalizeVirtualPath(virtualPath)));
            return false;
        }

        /// <summary>
        ///     Unless the path is not within the virtual folder, or IO permissions are missing, will return an IVirtualFile
        ///     instance for the path.
        ///     The file may or may not exist.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="queryString"></param>
        /// <returns></returns>
        public IVirtualFile GetFile(string virtualPath, NameValueCollection queryString)
        {
            if (NoIOPermission) return null;
            if (!IsVirtualPath(virtualPath)) return null; //It's not even in our area.
            return new VirtualFolderProviderVirtualFile(virtualPath, this);
        }

        /// <summary>
        ///     For internal use only by the .NET VPP system.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public override bool FileExists(string virtualPath)
        {
            if (isOnlyVirtualPath(virtualPath))
                return File.Exists(LocalMapPath(virtualPath));
            else
                return Previous.FileExists(virtualPath);
        }

        /// <summary>
        ///     For internal use only by the .NET VPP system.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <returns></returns>
        public override VirtualFile GetFile(string virtualPath)
        {
            if (isOnlyVirtualPath(virtualPath))
                return new VirtualFolderProviderVirtualFile(virtualPath, this);
            else
                return Previous.GetFile(virtualPath);
        }

        /// <summary>
        ///     For internal use only by the .NET VPP system.
        /// </summary>
        /// <param name="virtualPath"></param>
        /// <param name="virtualPathDependencies"></param>
        /// <param name="utcStart"></param>
        /// <returns></returns>
        public override CacheDependency GetCacheDependency(
            string virtualPath,
            IEnumerable virtualPathDependencies,
            DateTime utcStart)
        {
            if (!isOnlyVirtualPath(virtualPath))
                return Previous.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);

            var deps = new List<string>();
            foreach (string s in virtualPathDependencies) deps.Add(s);


            return new CacheDependency(new[] { LocalMapPath(virtualPath) }, deps.ToArray(), utcStart);
        }

        public class VirtualFolderProviderVirtualFile : VirtualFile, IVirtualFileAsync, IVirtualFileWithModifiedDate,
            IVirtualFile, IVirtualFileSourceCacheKey
        {
            private VirtualFolder provider;

            private Nullable<bool> _exists = null;
            private Nullable<DateTime> _fileModifiedDate = null;

            public bool Exists
            {
                get
                {
                    if (_exists == null) _exists = provider.FileExists(VirtualPath);
                    return _exists.Value;
                }
            }

            public VirtualFolderProviderVirtualFile(string virtualPath, VirtualFolder provider)
                : base(virtualPath)
            {
                this.provider = provider;
            }

            public override Stream Open()
            {
                return provider.getStream(VirtualPath);
            }

            public DateTime ModifiedDateUTC
            {
                get
                {
                    if (_fileModifiedDate == null) _fileModifiedDate = provider.GetDateModifiedUtc(VirtualPath);
                    return _fileModifiedDate.Value;
                }
            }

            public string GetCacheKey(bool includeModifiedDate)
            {
                return VirtualPath + (includeModifiedDate
                    ? "_" + ModifiedDateUTC.Ticks.ToString(CultureInfo.InvariantCulture)
                    : "");
            }

            public Task<Stream> OpenAsync()
            {
                return Task.FromResult(provider.getStream(VirtualPath));
            }
        }

        public IEnumerable<IIssue> GetIssues()
        {
            var issues = new List<IIssue>();

            if (NoIOPermission)
                issues.Add(new Issue(
                    "The VirtualFolder plugin cannot serve files from \"" + PhysicalPath +
                    "\" until you increase the trust level or modify the trust configuration to permit read access to the directory.",
                    IssueSeverity.Error));
            if (FailedToRegisterVpp)
                issues.Add(new Issue(
                    "The VirtualFolder plugin failed to register itself as an app-wide VirtualPathProvider. It will only work for images processed by the image resizer.",
                    IssueSeverity.Error));


            return issues;
        }


        public Task<bool> FileExistsAsync(string virtualPath, NameValueCollection queryString)
        {
            if (NoIOPermission)
                return
                    Task.FromResult(false); //Because File.Exists is always false when IOPermission is missing, anyhow.
            if (!IsVirtualPath(virtualPath)) return Task.FromResult(false); //It's not even in our area.
            if (File.Exists(LocalMapPath(virtualPath)))
                //Ok, we could serve it, but existing files take precedence.
                //Return false if we would be masking an existing file.
                return Task.FromResult(!File.Exists(HostingEnvironment.MapPath(normalizeVirtualPath(virtualPath))));
            return Task.FromResult(false);
        }

        public Task<IVirtualFileAsync> GetFileAsync(string virtualPath, NameValueCollection queryString)
        {
            if (NoIOPermission) return Task.FromResult<IVirtualFileAsync>(null);
            if (!IsVirtualPath(virtualPath))
                return Task.FromResult<IVirtualFileAsync>(null); //It's not even in our area.
            return Task.FromResult<IVirtualFileAsync>(new VirtualFolderProviderVirtualFile(virtualPath, this));
        }
    }
}