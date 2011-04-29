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