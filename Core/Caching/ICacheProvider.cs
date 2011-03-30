using System;
using System.Collections.Generic;
using System.Text;

namespace fbs.ImageResizer.Caching {
    public interface ICacheProvider {
        ICache GetCachingSystem(System.Web.HttpContext context, IResponseArgs args);
    }
}
