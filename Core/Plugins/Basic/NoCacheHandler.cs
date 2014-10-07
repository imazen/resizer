/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using ImageResizer.Caching;
using System.IO;

namespace ImageResizer.Plugins.Basic {
    /// <summary>
    /// Implements IHttpHandler, serves content for the NoCache plugin
    /// </summary>
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
            context.Response.BufferOutput = true; //Same as .Buffer. Allows bitmaps to be disposed quicker.

            //Generally, it's OK to send the source file's last-modified date. But that causes problems with watermarks and other dependencies. 
            //DateTime lastModified = e.GetModifiedDateUTC();
            //if (lastModified != DateTime.MinValue && e.ResponseHeaders.LastModified == DateTime.MinValue) e.ResponseHeaders.LastModified = lastModified;

            e.ResponseHeaders.ApplyDuringPreSendRequestHeaders = false;
            e.ResponseHeaders.ApplyToResponse(e.ResponseHeaders, context);
            MemoryStream ms = new MemoryStream();
            e.ResizeImageToStream(ms);
            ms.Seek(0, SeekOrigin.Begin);
            ms.CopyTo(context.Response.OutputStream);
        }
    }
}
