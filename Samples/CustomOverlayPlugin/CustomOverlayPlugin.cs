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

namespace ImageResizer.Plugins.CustomOverlay {
    /// <summary>
    /// This plugin applies image overlays onto a base image using data provided by the specified IOverlayProvider. 
    /// It requires a base image path and therefore cannot work except within the HttpModule.
    /// </summary>
    public class CustomOverlayPlugin:BuilderExtension, IPlugin {
        IOverlayProvider provider;
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
            //If it's our request, lookup the data from the provider and add the hash so we can allow the overlay data to change without clearing diskcache
            if (string.IsNullOrEmpty(e.QueryString["customoverlay"])) return;

            var o = provider.GetOverlayInfo(e.VirtualPath, e.QueryString);
            if (o == null) return;
            e.QueryString["customoverlay.hash"] = o.GetDataHashCode().ToString();

            //And save the overlay object so we don't have to look it up twice
            context.Items["customoverlay"] = o; 
        }



        protected override RequestedAction RenderOverlays(ImageState s) {
            Overlay o = (HttpContext.Current != null && HttpContext.Current.Items["customoverlay"] != null) ? HttpContext.Current.Items["customoverlay"] as Overlay: null;
            if (o == null || string.IsNullOrEmpty(o.OverlayPath)) return RequestedAction.None; //skip town if there's no overlay object cached.


            Graphics g = s.destGraphics;
            s.destGraphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            s.destGraphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            s.destGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;


            //Get the memcached overlay (assuming SourceFileCache is installed) 
            using (Bitmap b = c.CurrentImageBuilder.LoadImage(o.OverlayPath,new ResizeSettings("memcache=true")))
            using (ImageAttributes ia = new ImageAttributes()){
                ia.SetWrapMode( System.Drawing.Drawing2D.WrapMode.TileFlipXY);

                g.DrawImage(b, TranslatePoints(new LayoutEngine().Layout(o, s, b.Size), s), new Rectangle(0, 0, b.Width, b.Height), GraphicsUnit.Pixel, ia);
            }

            return RequestedAction.None;
        }

        protected double GetAngleFromXAxis(PointF a, PointF b) {
            return -Math.Atan2(b.Y - a.Y, b.X - a.X) ;
        }

        /// <summary>
        /// Translates the specified points from the source bitmap coordinate space to the destination image coordinate space.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        protected PointF[] TranslatePoints(PointF[] points, ImageState s) {
            PointF[] moved = new PointF[points.Length];
            PointF[] dest = s.layout["image"];
            double newWidth = PolygonMath.Dist(dest[0], dest[1]);
            double newHeight = PolygonMath.Dist(dest[1], dest[2]);
            double xScale = newWidth / s.copyRect.Width;
            double yScale = newHeight / s.copyRect.Height;

            double angle = GetAngleFromXAxis(dest[0], dest[1]);

            for (int i = 0; i < points.Length; i++) {
                PointF p = points[i];
                p.X -= s.copyRect.X; //Translate
                p.Y -= s.copyRect.Y;
                p.X = (float)(p.X * xScale); //Scale
                p.Y = (float)(p.Y * yScale);
                p = PolygonMath.RotateVector(p, angle, dest[0]); //Rotate
                moved[i] = p;
            }
            return moved;
        }

        public bool Uninstall(Config c) {
            c.Pipeline.Rewrite -= Pipeline_Rewrite;
            c.Plugins.remove_plugin(this);
            return true;
        }
    }
}
