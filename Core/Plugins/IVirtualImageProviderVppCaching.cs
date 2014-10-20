using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Caching;

namespace ImageResizer.Plugins
{
    public interface IVirtualImageProviderVppCaching:IVirtualImageProviderVpp
    {
        CacheDependency VppGetCacheDependency(string virtualPath,
          IEnumerable virtualPathDependencies,
          DateTime utcStart);
        string VppGetFileHash(string virtualPath, IEnumerable virtualPathDependencies);
    }
}
