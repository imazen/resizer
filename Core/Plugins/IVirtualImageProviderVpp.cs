using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Caching;

namespace ImageResizer.Plugins
{
    public interface IVirtualImageProviderVpp : IVirtualImageProvider
    {
        bool VppExposeFile(string virtualPath);

    }
}
