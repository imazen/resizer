using System;
using System.Collections.Generic;
using System.Web;
using ImageResizer;
using System.Drawing;
using ImageResizer.Configuration;
using ImageResizer.Plugins.RemoteReader;
using System.Diagnostics;

namespace ComplexWebApplication {
    public class Global : System.Web.HttpApplication {

        protected void Application_Start(object sender, EventArgs e) {
            // Code that runs on application startup

            var sw = Stopwatch.StartNew();
            
            //This is a URL rewrite rule. It sets the default value of '404' to '~/Sun_256.png' for all requests containing '/propertyimages/'
            Config.Current.Pipeline.RewriteDefaults += delegate(IHttpModule m, HttpContext c, ImageResizer.Configuration.IUrlEventArgs args) {
                if (args.VirtualPath.IndexOf("/propertyimages/", StringComparison.OrdinalIgnoreCase) > -1)
                    args.QueryString["404"] = "~/Sun_256.png";
            };

            Config.Current.Pipeline.PostRewrite += delegate(IHttpModule sender2, HttpContext context, IUrlEventArgs ev) {
                //Check folder
                string folder = VirtualPathUtility.ToAbsolute("~/folder");
                if (ev.VirtualPath.StartsWith(folder, StringComparison.OrdinalIgnoreCase)) {
                    //Estimate final image size, based on the original image being 600x600. Only useful for rough checking, as aspect ratio differences will affect results
                    Size estimatedSize = ImageBuilder.Current.GetFinalSize(new System.Drawing.Size(600,600),new ResizeSettings(ev.QueryString));
                    if (estimatedSize.Width > 100 || estimatedSize.Height > 100){
                        //It's over 100px, apply watermark
                        ev.QueryString["watermark"] = "Sun_256.png";
                    }
                }
            };
            sw.Stop();
            Debug.Write("ImageResizer loaded in " + sw.ElapsedMilliseconds.ToString() + "ms");

        }


        

        protected void Application_BeginRequest(object sender, EventArgs e) {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e) {

        }

        protected void Application_Error(object sender, EventArgs e) {

        }

        protected void Session_End(object sender, EventArgs e) {

        }

        protected void Application_End(object sender, EventArgs e) {

        }
    }
}