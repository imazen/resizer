using ImageResizer.Configuration;
using ImageResizer.Plugins;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Web.Caching;
using System.Web.Hosting;

namespace ImageResizer.Plugins
{

    /// <summary>
    /// Allows IVirtualImageProviders to expose access through the ASP.NET VirtualPathProvider system.
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

        protected IVirtualImageProviderVpp GetVIP(string virtualPath)
        {
            var qs = new NameValueCollection();
            foreach (IVirtualImageProviderVpp p in c.Plugins.GetAll<IVirtualImageProviderVpp>())
            {
                if (p.VppExposeFile(virtualPath) && p.FileExists(virtualPath, qs)) return p;
            }
            return null;
        }

        public override bool FileExists(string virtualPath)
        {
            var p = GetVIP(virtualPath);
            return p != null || Previous.FileExists(virtualPath);
        }


        public override VirtualFile GetFile(string virtualPath)
        {
            var p = GetVIP(virtualPath);
            if (p != null)
            {
                var f = p.GetFile(virtualPath, new NameValueCollection());
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
            var p = GetVIP(virtualPath) as IVirtualImageProviderVppCaching;
        
            var hash = p != null ? p.VppGetFileHash(virtualPath, virtualPathDependencies) : null;
            return hash ?? base.GetFileHash(virtualPath, virtualPathDependencies);
        }

        public override CacheDependency GetCacheDependency(
          string virtualPath,
          IEnumerable virtualPathDependencies,
          DateTime utcStart)
        {
            var p = GetVIP(virtualPath);
            if (p is IVirtualImageProviderVppCaching){
                var pc = p as IVirtualImageProviderVppCaching;
                var dep = pc == null ? null : pc.VppGetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
                return dep ?? new EmptyCacheDependency();
            }else{
                return Previous.GetCacheDependency(virtualPath, virtualPathDependencies, utcStart);
            }
        }


    }
}