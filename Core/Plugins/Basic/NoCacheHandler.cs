// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.Licensed under the Apache License, Version 2.0.

using System.IO;
using System.Web;
using ImageResizer.Caching;

namespace ImageResizer.Plugins.Basic
{
    /// <summary>
    ///     Implements IHttpHandler, serves content for the NoCache plugin
    /// </summary>
    public class NoCacheHandler : IHttpHandler
    {
        private IResponseArgs e;

        public NoCacheHandler(IResponseArgs e)
        {
            this.e = e;
        }

        public bool IsReusable => false;

        public void ProcessRequest(HttpContext context)
        {
            context.Response.StatusCode = 200;
            context.Response.BufferOutput = true; //Same as .Buffer. Allows bitmaps to be disposed quicker.

            //Generally, it's OK to send the source file's last-modified date. But that causes problems with watermarks and other dependencies. 
            //DateTime lastModified = e.GetModifiedDateUTC();
            //if (lastModified != DateTime.MinValue && e.ResponseHeaders.LastModified == DateTime.MinValue) e.ResponseHeaders.LastModified = lastModified;

            e.ResponseHeaders.ApplyDuringPreSendRequestHeaders = false;
            e.ResponseHeaders.ApplyToResponse(e.ResponseHeaders, context);
            var ms = new MemoryStream();
            e.ResizeImageToStream(ms);
            ms.Seek(0, SeekOrigin.Begin);
            ms.CopyTo(context.Response.OutputStream);
        }
    }
}