/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using ImageResizer.Caching;
using System.Threading.Tasks;
using System.Threading;
using ImageResizer.Util;

namespace ImageResizer.Plugins.Basic
{
    /// <summary>
    /// Implements IHttpHandler, serves content for the NoCache plugin
    /// </summary>
    public class NoCacheAsyncHandler : AsyncUtils.AsyncHttpHandlerBase
    {
        private IAsyncResponsePlan e;

        public NoCacheAsyncHandler(IAsyncResponsePlan e)
        {
            this.e = e;
        }

         public override Task ProcessRequestAsync(HttpContext context){
            context.Response.StatusCode = 200;
            context.Response.BufferOutput = true; //Same as .Buffer. Allows bitmaps to be disposed quicker.
            context.Response.ContentType = e.EstimatedContentType;
            return  e.CreateAndWriteResultAsync(context.Response.OutputStream, e);
        }

    }
}
