using System;
using System.Collections.Generic;
using System.Web;


namespace WatermarkWebTest {
    public class Global : System.Web.HttpApplication {

        protected void Application_Start(object sender, EventArgs e) {
            ImageResizer.Plugins.Watermark.WatermarkPlugin w = new ImageResizer.Plugins.Watermark.WatermarkPlugin();
            w.align = System.Drawing.ContentAlignment.BottomLeft;

            w.hideIfTooSmall = false;
            w.keepAspectRatio = true;
            w.valuesPercentages = true;
            w.watermarkDir = "~/"; 
            w.bottomRightPadding = new System.Drawing.SizeF(0, 0);
            w.topLeftPadding = new System.Drawing.SizeF(0, 0);
            w.watermarkSize = new System.Drawing.SizeF(0.5f, 0.5f);

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