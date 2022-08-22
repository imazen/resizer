// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Web.Caching;
using System.Web.Hosting;
using ImageResizer.Configuration;

namespace ImageResizer.Plugins
{
    /// <summary>
    ///     Allows IVirtualImageProviders to expose access through the ASP.NET VirtualPathProvider system.
    /// </summary>
    public class VirtualPathProviderShim : VirtualPathProvider
    {
        private Config c;

        public VirtualPathProviderShim(Config c)
            : base()
        {
            this.c = c;
        }


        protected override void Initialize()
        {
        }

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

        protected IVirtualImageProviderVpp GetVIP(string virtualPath)
        {
            var path = normalizeVirtualPath(
                virtualPath); //Because *sometimes* ASP.NET likes to send us app-relative paths, just for the heck of it.
            var qs = new NameValueCollection();
            foreach (var p in c.Plugins.GetAll<IVirtualImageProviderVpp>())
                if (p.VppExposeFile(path) && p.FileExists(path, qs))
                    return p;
            return null;
        }

        public override bool FileExists(string virtualPath)
        {
            var p = GetVIP(virtualPath);
            return p != null || Previous.FileExists(virtualPath);
        }


        public override VirtualFile GetFile(string virtualPath)
        {
            var path = normalizeVirtualPath(
                virtualPath); //Because *sometimes* ASP.NET likes to send us app-relative paths, just for the heck of it.
            var p = GetVIP(path);
            if (p != null)
            {
                var f = p.GetFile(path, new NameValueCollection());
                if (f != null) return new VirtualFileShim(f);
            }

            return Previous.GetFile(virtualPath);
        }


        private class EmptyCacheDependency : CacheDependency
        {
            public EmptyCacheDependency()
            {
            }
        }

        public override string GetFileHash(string virtualPath, IEnumerable virtualPathDependencies)
        {
            var path = normalizeVirtualPath(
                virtualPath); //Because *sometimes* ASP.NET likes to send us app-relative paths, just for the heck of it.

            var p = GetVIP(path) as IVirtualImageProviderVppCaching;

            var hash = p != null ? p.VppGetFileHash(path, virtualPathDependencies) : null;
            return hash ?? base.GetFileHash(path, virtualPathDependencies);
        }

        public override CacheDependency GetCacheDependency(
            string virtualPath,
            IEnumerable virtualPathDependencies,
            DateTime utcStart)
        {
            var path = normalizeVirtualPath(
                virtualPath); //Because *sometimes* ASP.NET likes to send us app-relative paths, just for the heck of it.
            var p = GetVIP(path);
            if (p is IVirtualImageProviderVppCaching)
            {
                var pc = p as IVirtualImageProviderVppCaching;
                var dep = pc == null ? null : pc.VppGetCacheDependency(path, virtualPathDependencies, utcStart);
                return dep ?? new EmptyCacheDependency();
            }
            else
            {
                return Previous.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
            }
        }
    }
}