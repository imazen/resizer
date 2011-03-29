using System;
using System.Collections.Generic;
using System.Text;

namespace fbs.ImageResizer.Caching {
    public class NoCache :ICache {
        public void Process(System.Web.HttpContext context, CacheEventArgs e) {
            e.ResponseHeaders.ApplyToResponse(e.ResponseHeaders, context);
            
            e.ResizeImageToStream(context.Response.OutputStream);
            
        }
    }
}
