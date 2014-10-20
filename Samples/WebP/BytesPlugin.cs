using System;
using System.Collections.Generic;
using System.Web;
using ImageResizer.Plugins;
using ImageResizer.Configuration;
using System.IO;
using ImageResizer.Caching;
using System.Drawing;
using System.Drawing.Imaging;

namespace WebP {
    public class BytesPlugin:IPlugin {

        public BytesPlugin() { }

        public IPlugin Install(Config c) {
            c.Plugins.add_plugin(this); 
            c.Pipeline.PreHandleImage += Pipeline_PreHandleImage;
            return this;
        }

        void Pipeline_PreHandleImage(IHttpModule sender, HttpContext context, ImageResizer.Caching.IResponseArgs e) {
            if (!ImageResizer.ExtensionMethods.NameValueCollectionExtensions.Get<bool>(e.RewrittenQuerystring, "showbytes", false)) return;
            var old = e.ResizeImageToStream;
            ((ResponseArgs)e).ResizeImageToStream = delegate(Stream s) {
                MemoryStream ms = new MemoryStream(8096);
                old(ms);
                WriteTextInPng(ms.Length.ToString("N") + " bytes", s);
            };
        }

        public bool Uninstall(ImageResizer.Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            c.Pipeline.PreHandleImage -= Pipeline_PreHandleImage;
            return true;
        }

        public void WriteTextInPng(string text, Stream s) {

            using (Bitmap b = new Bitmap(120, 25)) {
                
                using (Graphics g = Graphics.FromImage(b)) {
                    g.Clear(Color.White);
                    using (Font f = new Font(FontFamily.GenericSansSerif,12)){
                        g.DrawString(text, f, Brushes.Black, new PointF(0, 0));
                    }
                }

                if (!s.CanSeek) {
                    var ms = new MemoryStream();
                    b.Save(ms, ImageFormat.Png);
                    ImageResizer.ExtensionMethods.StreamExtensions.CopyToStream(ms, s, true);
                } else b.Save(s, ImageFormat.Png);
            }
        }
    }
}