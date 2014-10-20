using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using ImageResizer.Caching;
using ImageResizer.Util;
using System.Threading.Tasks;
using System.IO;

namespace ImageResizer.Plugins.MemCache {
        /// <summary>
    /// Implements IHttpHandler, serves content for the NoCache plugin
    /// </summary>
    public class MemCacheHandler : AsyncUtils.AsyncHttpHandlerBase {
        private IResponseArgs e;
        private IAsyncResponsePlan p;
        private byte[] data;

        public MemCacheHandler(IResponseArgs e, byte[] data) {
            this.e = e;
            this.data = data;
        }
        public MemCacheHandler(IAsyncResponsePlan p, byte[] data)
        {
            this.p = p;
            this.data = data;
        }

        public override Task ProcessRequestAsync(HttpContext context)
        {
            context.Response.StatusCode = 200;
            context.Response.BufferOutput = false;
            if (e != null)
            {
                e.ResponseHeaders.ApplyDuringPreSendRequestHeaders = false;
                e.ResponseHeaders.ApplyToResponse(e.ResponseHeaders, context);
            }
            if (p != null)
            {
                context.Response.ContentType = p.EstimatedContentType;
            }
            var ms = new MemoryStream(data, 0, data.Length, false, true);
            return ms.CopyToAsync(context.Response.OutputStream);
        }
    }
}
