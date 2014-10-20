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
using System.IO;

namespace ImageResizer.Plugins.Watermark {
    public class ImageLayer:Layer {
        public ImageLayer(NameValueCollection attrs, ResizeSettings defaultImageQuery, Config c)
            : base(attrs) {
            this.c = c;

            var configPath = attrs["path"];
            var configImageQuery = attrs["imageQuery"];

            if (!string.IsNullOrEmpty(configPath))
            {
                this.Path = PathUtils.RemoveQueryString(configPath);
            }

            // Combine the ResizeSettings from 'path', 'imageQuery', and any
            // 'defaultImageQuery' settings as well.  Settings from 'imageQuery'
            // take precedence over 'path', and both take precedence over the
            // 'defaultImageQuery' settings.
            var pathSettings = new ResizeSettings(configPath ?? string.Empty);
            var imageQuerySettings = new ResizeSettings(configImageQuery ?? string.Empty);
            var mergedSettings = new ResizeSettings(imageQuerySettings, pathSettings);

            this.ImageQuery = new ResizeSettings(mergedSettings, defaultImageQuery);
        }

        public ImageLayer(Config c)
        {
            this.c = c;
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
        protected Config c = null;

        public Config ConfigInstance { get { return c; } set { c = value; } }
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

        public override object[] GetHashBasis()
        {
            var b = new object[]{Path,ImageQuery};
            var o = base.GetHashBasis();
            object[] combined = new object[b.Length + o.Length];
            Array.Copy(b,combined,b.Length);
            Array.Copy(o,0,combined,b.Length,o.Length);
            return combined;
        }

        public ImageLayer Copy() {
            ImageLayer l = new ImageLayer(this.c);
            this.CopyTo(l);
            return l;
        }

        /// <summary>
        /// Loads or caches a bitmap, using asp.net's cache (when available)
        /// </summary>
        /// <param name="query"></param>
        /// <param name="virtualPath"></param>
        /// <param name="onlyLoadIfCacheExists">Whether to load the image when
        /// no cache is available.  Pass <c>true</c> for pre-fetching, and
        /// <c>false</c> if the image is needed immediately.</param>
        /// <returns>Returns the Bitmap.  If no cache is available, and
        /// <c>onlyLoadIfCacheExists</c> is <c>true</c>, returns <c>null</c>
        /// rather than loading the Bitmap.</returns>
        public Bitmap GetMemCachedBitmap(string virtualPath, ResizeSettings query, bool onlyLoadIfCacheExists = false) {
            //If not ASP.NET, don't cache.
            if (HttpContext.Current == null) return onlyLoadIfCacheExists ? null : c.CurrentImageBuilder.LoadImage(virtualPath, query);

            string key = virtualPath.ToLowerInvariant() + query.ToString();
            Bitmap b = HttpContext.Current.Cache[key] as Bitmap;
            if (b != null) return b;
            try
            {
                b = c.CurrentImageBuilder.LoadImage(virtualPath, query);
            }
            catch (FileNotFoundException fe)
            {
                throw new ImageProcessingException(500, "Failed to located watermark " + virtualPath, "Failed to located a watermarking file", fe);
            }
            //Query VPPs for cache dependency. TODO: Add support for IVirtualImageProviders to customize cache dependencies.
            CacheDependency cd = null;
            if (HostingEnvironment.VirtualPathProvider != null) cd = HostingEnvironment.VirtualPathProvider.GetCacheDependency(virtualPath, new string[] { }, DateTime.UtcNow);

            HttpContext.Current.Cache.Insert(key, b, cd);
            return b;
        }

        public void PreFetchImage() {
            if (this.ShouldLoadAsOriginalSize()) {
                GetMemCachedBitmap(this.Path, this.ImageQuery, onlyLoadIfCacheExists: true);
            }
        }

        public override void RenderTo(Resizing.ImageState s) {
            if (string.IsNullOrEmpty(Path)) return;

            s.destGraphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
            s.destGraphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            s.destGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;

            Bitmap img = null;
            if (this.ShouldLoadAsOriginalSize()) {
                img = GetMemCachedBitmap(Path, ImageQuery);
            }
           
            //Calculate the location for the bitmap
            RectangleF imgBounds = this.CalculateLayerCoordinates(s, delegate(double maxwidth, double maxheight) {
                ResizeSettings opts = new ResizeSettings(ImageQuery);
                if (Fill && string.IsNullOrEmpty(opts["scale"])) opts.Scale = ScaleMode.Both;
                if (!double.IsNaN(maxwidth)) opts.MaxWidth = (int)Math.Floor(maxwidth);
                if (!double.IsNaN(maxheight)) opts.MaxHeight = (int)Math.Floor(maxheight);

                if (img == null) img = GetMemCachedBitmap(Path, opts); //Delayed creation allows the maxwidth/maxheight to be used in gradient plugin
                lock (img) {
                    return ImageBuilder.Current.GetFinalSize(img.Size, opts);
                }
            }, true);

            lock (img) { //Only one reader from the cached bitmap at a time.
                //Skip rendering unless we have room to work with.
                if (imgBounds.Width <2 || imgBounds.Height < 2) return;


                if (ImageQuery.Keys.Count > 0 || Fill) {
                    ResizeSettings settings = new ResizeSettings(ImageQuery);
                    if (Fill && string.IsNullOrEmpty(settings["scale"])) settings.Scale = ScaleMode.Both;

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

        /// <summary>
        /// Whether the image should be loaded in its original size.  If it can,
        /// it can also be pre-fetched.
        /// </summary>
        /// <returns></returns>
        private bool ShouldLoadAsOriginalSize() {
            if (string.IsNullOrEmpty(this.Path)) return false;

            // If the image is a virtual gradient, we don't load in its "original
            // size".  Instead, we allow later-estimated width/height values to
            // be used to create it at the actual needed size.
            var file = c.Pipeline.GetFile(this.Path, this.ImageQuery);
            if (file != null && (file is ImageResizer.Plugins.Basic.Gradient.GradientVirtualFile)) {
                return false;
            }

            return true;
        }
    }
}
