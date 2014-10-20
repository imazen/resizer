using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Plugins;
using ImageResizer.Configuration;
using ImageResizer.Encoding;
using System.Collections.Specialized;
using Imazen.WebP;
using System.Drawing;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.WebPEncoder {
    public class WebPEncoderPlugin:IPlugin, IEncoder {
       
        public IPlugin Install(Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        public WebPEncoderPlugin() {
            Quality = 90;
            Lossless = false;
            NoAlpha = false;
        }
        public WebPEncoderPlugin(NameValueCollection args):this() {
            Lossless = args.Get<bool>("lossless", Lossless);
            Quality = args.Get<float>("quality", Quality);
            NoAlpha = args.Get<bool>("noalpha", NoAlpha);
            
        }

        public float Quality { get; set; }
        public bool Lossless { get; set; }
        /// <summary>
        /// If true, the alpha channel will be ignored, even if present.
        /// </summary>
        public bool NoAlpha { get; set; }

        public IEncoder CreateIfSuitable(ResizeSettings settings, object original) {
            if ("webp".Equals(settings.Format, StringComparison.OrdinalIgnoreCase)) {
                return new WebPEncoderPlugin(settings);
            }
            return null;
        }

        public void Write(System.Drawing.Image i, System.IO.Stream s) {
            using (var b = new Bitmap(i)){
                new SimpleEncoder().Encode(b, s, Lossless ? -1 : Quality);
           }
        }

        public bool SupportsTransparency {
            get { return true;  }
        }

        public string MimeType {
            get { return "image/webp"; }
        }

        public string Extension {
            get { return "webp";  }
        }
    }
}
