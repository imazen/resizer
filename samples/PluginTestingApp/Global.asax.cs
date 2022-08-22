// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the Apache License, Version 2.0.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Web;
using ImageResizer;
using ImageResizer.Configuration;

namespace ComplexWebApplication
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup

            var sw = Stopwatch.StartNew();

            //This is a URL rewrite rule. It sets the default value of '404' to '~/Sun_256.png' for all requests containing '/propertyimages/'
            Config.Current.Pipeline.RewriteDefaults += delegate(IHttpModule m, HttpContext c, IUrlEventArgs args)
            {
                if (args.VirtualPath.IndexOf("/propertyimages/", StringComparison.OrdinalIgnoreCase) > -1)
                    args.QueryString["404"] = "~/Sun_256.png";
            };

            Config.Current.Pipeline.PostRewrite += delegate(IHttpModule sender2, HttpContext context, IUrlEventArgs ev)
            {
                //Check folder
                var folder = VirtualPathUtility.ToAbsolute("~/folder");
                if (ev.VirtualPath.StartsWith(folder, StringComparison.OrdinalIgnoreCase))
                {
                    //Estimate final image size, based on the original image being 600x600. Only useful for rough checking, as aspect ratio differences will affect results
                    var estimatedSize =
                        ImageBuilder.Current.GetFinalSize(new Size(600, 600), new ResizeSettings(ev.QueryString));
                    if (estimatedSize.Width > 100 || estimatedSize.Height > 100)
                        //It's over 100px, apply watermark
                        ev.QueryString["watermark"] = "Sun_256.png";
                }
            };
            sw.Stop();
            Debug.Write("ImageResizer loaded in " + sw.ElapsedMilliseconds.ToString() + "ms");
        }


        protected void Application_BeginRequest(object sender, EventArgs e)
        {
        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {
        }

        protected void Application_Error(object sender, EventArgs e)
        {
        }

        protected void Session_End(object sender, EventArgs e)
        {
        }

        protected void Application_End(object sender, EventArgs e)
        {
        }
    }
}