using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace ImageResizer.Caching {
    public class NoCacheHandler :IHttpHandler{
        private IResponseArgs e;

        public NoCacheHandler(IResponseArgs e) {
            this.e = e;
        }

        public bool IsReusable {
            get { return false; }
        }

        public void ProcessRequest(HttpContext context) {
            context.Response.StatusCode = 200;
            e.ResponseHeaders.ApplyDuringPreSendRequestHeaders = false;
            e.ResponseHeaders.ApplyToResponse(e.ResponseHeaders, context);
            e.ResizeImageToStream(context.Response.OutputStream);
        }
    }
}
