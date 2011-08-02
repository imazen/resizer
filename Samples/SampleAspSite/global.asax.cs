using System;
using System.Web;
using ImageResizer;
using System.Drawing;
using ImageResizer.Configuration;
using ImageResizer.Plugins.RemoteReader;

namespace App {
    public partial class Global : System.Web.HttpApplication {

        protected void Application_Start(object sender, EventArgs e) {
            RemoteReaderPlugin.Current.AllowRemoteRequest += delegate(object sender2, RemoteRequestEventArgs args) {
                //Allow all images from this trusted domain
                if (args.RemoteUrl.StartsWith("http://www.build.com", StringComparison.OrdinalIgnoreCase)) args.DenyRequest = false;
            };
        }

    }
}