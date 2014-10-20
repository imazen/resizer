using System;
using System.Web;
using ImageResizer;
using System.Drawing;
using ImageResizer.Configuration;
using ImageResizer.Plugins.RemoteReader;

namespace App {
    public partial class Global : System.Web.HttpApplication {

        protected void Application_Start(object sender, EventArgs e) {
                //The watermark plugin has lots of options, and they're easier to configure in code.
              /*ImageResizer.Plugins.Watermark.WatermarkPlugin w = new ImageResizer.Plugins.Watermark.WatermarkPlugin();
              w.align = System.Drawing.ContentAlignment.BottomLeft;
              w.hideIfTooSmall = true;
              w.keepAspectRatio = true;
              w.valuesPercentages = false;
              w.watermarkDir = "~/watermarks/"; //Where the watermark plugin looks for the image specifed in the querystring ?watermark=file.png
              w.bottomRightPadding = new System.Drawing.SizeF(20, 20);
              w.topLeftPadding = new System.Drawing.SizeF(20, 20);
              w.watermarkSize = new System.Drawing.SizeF(30, 30); //The desired size of the watermark, maximum dimensions (aspect ratio maintained if keepAspectRatio = true)
              //Install the plugin
              w.Install(Config.Current);
                        */
                        
            RemoteReaderPlugin.Current.AllowRemoteRequest += delegate(object sender2, RemoteRequestEventArgs args) {
                //Allow all images from this trusted domain
                if (args.RemoteUrl.StartsWith("http://www.build.com", StringComparison.OrdinalIgnoreCase)) args.DenyRequest = false;
            };
        }

    }
}