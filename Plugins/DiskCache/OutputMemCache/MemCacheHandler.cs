using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using ImageResizer.Caching;

namespace ImageResizer.Plugins.MemCache {
        /// <summary>
    /// Implements IHttpHandler, serves content for the NoCache plugin
    /// </summary>
    public class MemCacheHandler : IHttpHandler {
        private IResponseArgs e;
        private byte[] data;

        public MemCacheHandler(IResponseArgs e, byte[] data) {
            this.e = e;
            this.data = data;
        }

        public bool IsReusable {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context) {
            context.Response.StatusCode = 200;
            context.Response.BufferOutput = false; 
            e.ResponseHeaders.ApplyDuringPreSendRequestHeaders = false;
            e.ResponseHeaders.ApplyToResponse(e.ResponseHeaders, context);
            for (int i = 0; i < data.Length; i += 4096) {
                context.Response.OutputStream.Write(this.data, i, Math.Min(4096, data.Length - i));
            }

        }
    }
}
