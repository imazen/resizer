using ImageResizer.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ImageResizer.Plugins.HybridCache
{
    public class HybridCachePlugin : IAsyncTyrantCache, IPlugin
    {
        public bool CanProcess(HttpContext current, IAsyncResponsePlan e)
        {
            throw new NotImplementedException();
        }

        public IPlugin Install(Config c)
        {
            throw new NotImplementedException();
        }

        public Task ProcessAsync(HttpContext current, IAsyncResponsePlan e)
        {
            throw new NotImplementedException();
        }

        public bool Uninstall(Config c)
        {
            throw new NotImplementedException();
        }
    }
}
