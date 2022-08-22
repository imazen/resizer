// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System.Web;
using ImageResizer;

namespace SampleProject
{
    public class HandlerTest : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            /*
             * This is not a good approach to take!!! 
             * This is just to illustrate that you *can* use ImageBuilder in a lot of different ways
             * 
             * A proper implementation should:
             * Set caching headers.
             * Disk cache resizing results
             * Should be implemented as an HttpModule, not an IHttpHandler, and let the static file handler serve the resulting image from disk.
             * Implementing a HTTP handler that supports range requests, 401, 200, etc, is very complicated.
             * Read "Image Resizing Pitfalls" on nathanaeljones.com for more reasons why a handler is not a good solution (such as memory saturation)
             */
            var settings = new ResizeSettings("?maxwidth=80&maxheight=80&format=png");

            //Set the mime-type
            context.Response.ContentType =
                ImageBuilder.Current.EncoderProvider.GetEncoder(settings, "~/grass.jpg").MimeType;
            //Send result to output stream. 
            ImageBuilder.Current.Build("~/grass.jpg", context.Response.OutputStream, settings);
        }

        public bool IsReusable => false;
    }
}