using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace ImageResizer.Plugins
{
    public interface IAsyncTyrantCache
    {
        bool CanProcess(HttpContext current, IAsyncResponsePlan e);
        Task ProcessAsync(HttpContext current, IAsyncResponsePlan e);
    }
}
