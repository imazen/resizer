using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using ImageResizer.Configuration;
using System.Web;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using ImageResizer.Util;
using System.Collections.Specialized;
using System.Configuration;

namespace ImageResizer.Plugins.CustomOverlay {
    /// <summary>
    /// This plugin applies image overlays onto a base image using data provided by the specified IOverlayProvider. 
    /// It requires a base image path and therefore cannot work except within the HttpModule.
    /// </summary>
    public class CustomOverlayPlugin:BuilderExtension, IPlugin, IMultiInstancePlugin {
        IOverlayProvider provider;

        public CustomOverlayPlugin(NameValueCollection args) {
            string providerName = args["provider"];
            Type providerType = typeof(CachedOverlayProvider);

            if (!string.IsNullOrEmpty(providerName)){
                providerType = Type.GetType(providerName, false, true);
                if (providerType == null) throw new ConfigurationException("CustomOverlay: The specified provider '" + providerName + "' cannot be found. Ensure you are using the fully qualified type name, including assembly: MyNamespace.SampleProvider,MyAssembly");
            }
            this.provider = Activator.CreateInstance(providerType, args) as IOverlayProvider;
            if (this.provider == null) throw new ConfigurationException("CustomOverlay: The specified provider '" + providerName + "' was found, but does not implement IOverlayProvider.");
            //Use a unique key so we don't confuse other instances
            CustomOverlaysKey += new Random().Next().ToString();

        }

        public CustomOverlayPlugin(IOverlayProvider provider) {
            this.provider = provider;
        }

        Config c;
        public IPlugin Install(Config c) {
            this.c = c;
            c.Plugins.add_plugin(this);
            c.Pipeline.Rewrite += Pipeline_Rewrite;
            return this;
        }

        void Pipeline_Rewrite(System.Web.IHttpModule sender, System.Web.HttpContext context, IUrlEventArgs e) {
            var os = provider.GetOverlays(e.VirtualPath, e.QueryString);
            if (os == null) return;

            long hash = 0xab224895;
            int offset = 0;
            foreach (Overlay o in os) {
                hash ^= o.GetDataHashCode() << (offset % 50);
                offset += 31;
            }
            //Store a hash of all the overlays, so the disk cache updates when an overlay changes
            e.QueryString["customoverlay.hash"] = hash.ToString();

            //And save the overlays for later
            RequestOverlays = os; 
        }

        private string CustomOverlaysKey = "customoverlays_";

        /// <summary>
        /// Gets or sets the Overlay instances for the current request. Only accessible during an HttpRequest. 
        /// </summary>
        public IEnumerable<Overlay> RequestOverlays {
            get {
                return (HttpContext.Current != null && HttpContext.Current.Items[CustomOverlaysKey] != null) ? HttpContext.Current.Items[CustomOverlaysKey] as IEnumerable<Overlay> : null;
            }
            set {
                if (HttpContext.Current == null) throw new InvalidOperationException("HttpContext not present");
                HttpContext.Current.Items[CustomOverlaysKey] = value;
            }
        }

        protected override RequestedAction RenderOverlays(ImageState s) {
            IEnumerable<Overlay> overlays = RequestOverlays;
            if (overlays == null) return RequestedAction.None; //skip town if there's no overlay object cached.


            Graphics g = s.destGraphics;
            s.destGraphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            s.destGraphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            s.destGraphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            s.destGraphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            s.destGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            foreach (Overlay o in overlays) {
                if (string.IsNullOrEmpty(o.OverlayPath)) continue;//Skip overlays without a path

                try {
                    //Get the memcached overlay (assuming SourceFileCache is installed) 
                    using (Bitmap b = c.CurrentImageBuilder.LoadImage(o.OverlayPath, new ResizeSettings("memcache=true")))
                    using (ImageAttributes ia = new ImageAttributes()) {
                        ia.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);

                        g.DrawImage(b, PolygonMath.getParallelogram(new LayoutEngine().GetOverlayParalellogram(o, b.Size, s)), new Rectangle(0, 0, b.Width, b.Height), GraphicsUnit.Pixel, ia);
                        g.Flush(System.Drawing.Drawing2D.FlushIntention.Sync);
                    }
                } catch (FileNotFoundException fe) {
                    throw new ImageMissingException("The overlay image " + o.OverlayPath + " could not be found.");
                }
            }

            return RequestedAction.None;
        }


        public bool Uninstall(Config c) {
            c.Pipeline.Rewrite -= Pipeline_Rewrite;
            c.Plugins.remove_plugin(this);
            return true;
        }
    }
}
