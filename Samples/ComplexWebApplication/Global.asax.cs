using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;

namespace ComplexWebApplication {
    public class Global : System.Web.HttpApplication {

        protected void Application_Start(object sender, EventArgs e) {
            // Code that runs on application startup

           
            //This is a URL rewrite rule. It sets the default value of '404' to '~/Sun_256.png' for all requests containing '/propertyimages/'
            ImageResizer.Configuration.Config.Current.Pipeline.RewriteDefaults += delegate(IHttpModule m, HttpContext c, ImageResizer.Configuration.IUrlEventArgs args) {
                if (args.VirtualPath.IndexOf("/propertyimages/", StringComparison.OrdinalIgnoreCase) > -1)
                    args.QueryString["404"] = "~/Sun_256.png";
            };

            //The watermark plugin has lots of options, and they're easier to configure in code.
            ImageResizer.Plugins.Watermark.WatermarkPlugin w = new ImageResizer.Plugins.Watermark.WatermarkPlugin();
            w.align = System.Drawing.ContentAlignment.BottomLeft;
            w.hideIfTooSmall = true;
            w.keepAspectRatio = true;
            w.valuesPercentages = false;
            w.watermarkDir = "~/watermarks/"; //Where the watermark plugin looks for the image specifed in the querystring ?watermark=file.png
            w.bottomRightPadding = new System.Drawing.SizeF(20, 20);
            w.topLeftPadding = new System.Drawing.SizeF(20, 20);
            w.watermarkSize = new System.Drawing.SizeF(30, 30); //The desired size of the watermark, maximum dimensions (aspect ratio maintained if keepAspectRatio = true)
            //Install the plugin
            w.Install(ImageResizer.Configuration.Config.Current);

        }

        protected void Session_Start(object sender, EventArgs e) {

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