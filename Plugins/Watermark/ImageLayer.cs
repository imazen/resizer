using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Drawing;
using ImageResizer.Util;
using System.Web;
using System.Web.Hosting;
using System.Web.Caching;
using ImageResizer.Configuration;

namespace ImageResizer.Plugins.Watermark {
    public class ImageLayer:Layer {
        public ImageLayer(NameValueCollection attrs, Config c)
            : base(attrs) {
                Path = attrs["path"];
                this.c = c;
                if (!string.IsNullOrEmpty(attrs["imageQuery"])) ImageQuery = new ResizeSettings(attrs["imageQuery"]);
        }

        public ImageLayer() {
        }
        protected string _path = null;
        /// <summary>
        /// The virtual path to the watermark image
        /// </summary>
        public string Path {
            get { return _path; }
            set { _path = value; }
        }

        protected ResizeSettings _imageQuery = new ResizeSettings();
        protected Config c;
        /// <summary>
        /// Settings to apply to the watermark before overlaying it on the image. 
        /// </summary>
        public ResizeSettings ImageQuery {
            get { return _imageQuery; }
            set { _imageQuery = value; }
        }

        public void CopyTo(ImageLayer other) {
            base.CopyTo(other);
            other.Path = Path;
            other.ImageQuery = new ResizeSettings(ImageQuery);
        }
        public ImageLayer Copy() {
            ImageLayer l = new ImageLayer();
            this.CopyTo(l);
            return l;
        }
        /// <summary>
        /// Loads a bitmap, cached using asp.net's cache
        /// </summary>
        /// <param name="localfile"></param>
        /// <returns></returns>
        public Bitmap GetMemCachedBitmap(string virtualPath) {
            //If not ASP.NET, don't cache.
            if (HttpContext.Current == null) return c.CurrentImageBuilder.LoadImage(virtualPath, new ResizeSettings());

            string key = virtualPath.ToLowerInvariant();
            Bitmap b = HttpContext.Current.Cache[key] as Bitmap;
            if (b != null) return b;

            b = c.CurrentImageBuilder.LoadImage(virtualPath, new ResizeSettings());
            //Query VPPs for cache dependency. TODO: Add support for IVirtualImageProviders to customize cache dependencies.
            CacheDependency cd = null;
            if (HostingEnvironment.VirtualPathProvider != null) cd = HostingEnvironment.VirtualPathProvider.GetCacheDependency(virtualPath, new string[] { }, DateTime.UtcNow);

            HttpContext.Current.Cache.Insert(key, b, cd);
            return b;
        }


        public Bitmap GetBitmap() {
            return GetMemCachedBitmap(Path);
        }


        public override void RenderTo(Resizing.ImageState s) {
            if (string.IsNullOrEmpty(Path)) return;

            s.destGraphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            s.destGraphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            s.destGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            Bitmap img = GetBitmap();
            lock (img){ //Only one reader from the cached bitmap at a time.
                //Calculate the location for the bitmap
                RectangleF imgBounds = this.CalculateLayerCoordinates(s, delegate(double maxwidth, double maxheight) {
                    ResizeSettings opts = new ResizeSettings(ImageQuery);
                    if (!double.IsNaN(maxwidth)) opts.MaxWidth = (int)maxwidth;
                    if (!double.IsNaN(maxheight)) opts.MaxHeight = (int)maxheight;

                    return ImageBuilder.Current.GetFinalSize(img.Size, opts);
                }, true);

                //Skip rendering unless we have room to work with.
                if (imgBounds.Width <2 || imgBounds.Height < 2) return;


                if (ImageQuery.Keys.Count > 0) {
                    ResizeSettings settings = new ResizeSettings(ImageQuery);

                    settings.MaxWidth = (int)Math.Floor(imgBounds.Width);
                    settings.MaxHeight = (int)Math.Floor(imgBounds.Height);

                    using (Bitmap final = ImageBuilder.Current.Build(img, settings,false)) {
                        s.destGraphics.DrawImage(final, PolygonMath.ToRectangle(PolygonMath.CenterInside(PolygonMath.DownScaleInside(final.Size,imgBounds.Size), imgBounds)));
                    }
                } else {

                    s.destGraphics.DrawImage(img, PolygonMath.ToRectangle(PolygonMath.CenterInside(PolygonMath.DownScaleInside(img.Size,imgBounds.Size), imgBounds)));
                }

            }
        }
    }
}
