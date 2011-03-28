using System;
using System.Collections.Generic;
using System.Text;

namespace fbs.ImageResizer.Caching {
    public class NoCache :ICache {
        public void Process(System.Web.HttpContext current, CacheEventArgs e) {
            current.Response.ContentType = e.ContentType;
            e.ResizeImageToStream(current.Response.OutputStream);
            
        }
    }
}
