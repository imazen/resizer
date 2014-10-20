/* Copyright (c) 2014 Imazen See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Drawing;
using System.Web;
using ImageResizer.Util;
using ImageResizer.Resizing;
using System.Web.Hosting;
using ImageResizer.Configuration;
using System.Web.Caching;

namespace ImageResizer.Plugins.Watermark {
    /// <summary>
    /// Provides extensibility points for drawing watermarks and even modifying resizing/image settings
    /// </summary>
    public class LegacyWatermarkFeatures : BuilderExtension {
        [Obsolete("Use .OtherImages.Path instead")]
        public string watermarkDir = null;
        [Obsolete("Use. OtherImages.Width/Height or a named preset instead")]
        public SizeF watermarkSize = new SizeF(1, 1);
        [Obsolete("Use .OtherImages instead - it permits percentage and pixel values to be mixed.")]
        public Boolean valuesPercentages = true;
        
        public Boolean keepAspectRatio = true;
        [Obsolete("Use .OtherImages.Top/Left or an xml preset instead. ")]
        public SizeF topLeftPadding = new SizeF(0, 0);
        [Obsolete("Use .OtherImages.Bottom/Right or an xml preset instead.")]
        public SizeF bottomRightPadding = new SizeF(0, 0);
        [Obsolete("New behavior defaults to true")]
        public Boolean hideIfTooSmall = false;
        [Obsolete("Use .OtherImages.Align or an xml preset instead.")]
        public System.Drawing.ContentAlignment align = ContentAlignment.MiddleCenter;


        protected Config c;
        

        /// <summary>
        /// Loads or caches a bitmap, using asp.net's cache (when available)
        /// </summary>
        /// <param name="virtualPath">The virtual path to the file to load.</param>
        /// <param name="onlyLoadIfCacheExists">Whether to load the image when
        /// no cache is available.  Pass <c>true</c> for pre-fetching, and
        /// <c>false</c> if the image is needed immediately.</param>
        /// <returns>Returns the Bitmap.  If no cache is available, and
        /// <c>onlyLoadIfCacheExists</c> is <c>true</c>, returns <c>null</c>
        /// rather than loading the Bitmap.</returns>
        public Bitmap GetMemCachedBitmap(string virtualPath, bool onlyLoadIfCacheExists = false) {
            //If not ASP.NET, don't cache.
            if (HttpContext.Current == null) return onlyLoadIfCacheExists ? null : c.CurrentImageBuilder.LoadImage(virtualPath, new ResizeSettings());

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

        protected void LegacyPreFetchWatermark(ResizeSettings settings) {
            string watermark;
            if (this.LegacyParseWatermark(settings, out watermark)) {
                this.GetMemCachedBitmap(watermark, onlyLoadIfCacheExists: true);
            }
        }

        protected bool LegacyDrawWatermark(ImageState s) {
            Graphics g = s.destGraphics;
            if (g == null) return false;

            string watermark;
            if (!LegacyParseWatermark(s.settings, out watermark)) return false;

            RectangleF imageBox = PolygonMath.GetBoundingBox(s.layout["image"]);
            //Floor and ceiling values to prevent fractional placement.
            imageBox.Width = (float)Math.Floor(imageBox.Width);
            imageBox.Height = (float)Math.Floor(imageBox.Height);
            imageBox.X = (float)Math.Ceiling(imageBox.X);
            imageBox.Y = (float)Math.Ceiling(imageBox.Y);

            SizeF watermarkSize = this.watermarkSize;
            SizeF topLeftPadding = this.topLeftPadding;
            SizeF bottomRightPadding = this.bottomRightPadding;

            //Load the file specified in the querystring,
            Bitmap wb = GetMemCachedBitmap(watermark);
            lock (wb) {
                //If percentages, resolve to pixels
                if (valuesPercentages) {
                    //Force into 0..1 range, inclusive.
                    watermarkSize.Height = Math.Max(0, Math.Min(1, watermarkSize.Height));
                    watermarkSize.Width = Math.Max(0, Math.Min(1, watermarkSize.Width));
                    topLeftPadding.Height = Math.Max(0, Math.Min(1, topLeftPadding.Height));
                    topLeftPadding.Width = Math.Max(0, Math.Min(1, topLeftPadding.Width));
                    bottomRightPadding.Height = Math.Max(0, Math.Min(1, bottomRightPadding.Height));
                    bottomRightPadding.Width = Math.Max(0, Math.Min(1, bottomRightPadding.Width));

                    //Make sure everything adds up to 1
                    double totalWidth = watermarkSize.Width + topLeftPadding.Width + bottomRightPadding.Width;
                    if (totalWidth > 1) {
                        totalWidth = 1 / totalWidth; //Turn it into the factor we have to multiple by to make everything fit.
                        watermarkSize.Width *= (float)totalWidth;
                        topLeftPadding.Width *= (float)totalWidth;
                        bottomRightPadding.Width *= (float)totalWidth;
                    }
                    double totalHeight = watermarkSize.Height + topLeftPadding.Height + bottomRightPadding.Height;
                    if (totalHeight > 1) {
                        totalHeight = 1 / totalHeight; //Turn it into the factor we have to multiply by to make everything fit.
                        watermarkSize.Height *= (float)totalHeight;
                        topLeftPadding.Height *= (float)totalHeight;
                        bottomRightPadding.Height *= (float)totalHeight;
                    }

                    //Now, we can resolve the percentages to pixels.
                    watermarkSize.Height *= imageBox.Height;
                    watermarkSize.Width *= imageBox.Width;
                    topLeftPadding.Height *= imageBox.Height;
                    topLeftPadding.Width *= imageBox.Width;
                    bottomRightPadding.Height *= imageBox.Height;
                    bottomRightPadding.Width *= imageBox.Width;
                }

                //Keep aspect ratio, shrinking further if needed.
                if (keepAspectRatio) watermarkSize = PolygonMath.DownScaleInside(wb.Size, watermarkSize);


                //Floor all values to avoid rounding errors and blurry lines.
                watermarkSize = new SizeF((float)Math.Floor(watermarkSize.Width), (float)Math.Floor(watermarkSize.Height));
                topLeftPadding = new SizeF((float)Math.Floor(topLeftPadding.Width), (float)Math.Floor(topLeftPadding.Height));
                bottomRightPadding = new SizeF((float)Math.Floor(bottomRightPadding.Width), (float)Math.Floor(bottomRightPadding.Height));


                //Check boundingbox
                SizeF watermarkBoundingBox = new SizeF(watermarkSize.Width + topLeftPadding.Width + bottomRightPadding.Width,
                    watermarkSize.Height + topLeftPadding.Height + bottomRightPadding.Height);

                //Don't draw the watermark if it is too small.
                if (!PolygonMath.FitsInside(watermarkBoundingBox, imageBox.Size)) {
                    if (hideIfTooSmall) return true;
                    else {
                        SizeF oldSize = watermarkBoundingBox;
                        watermarkBoundingBox = PolygonMath.DownScaleInside(watermarkBoundingBox, imageBox.Size);
                        watermarkSize.Width -= (oldSize.Width - watermarkBoundingBox.Width);
                        watermarkSize.Height -= (oldSize.Height - watermarkBoundingBox.Height);
                    }
                }
                //Floor all values again
                watermarkSize = new SizeF((float)Math.Floor(watermarkSize.Width), (float)Math.Floor(watermarkSize.Height));
                topLeftPadding = new SizeF((float)Math.Floor(topLeftPadding.Width), (float)Math.Floor(topLeftPadding.Height));
                bottomRightPadding = new SizeF((float)Math.Floor(bottomRightPadding.Width), (float)Math.Floor(bottomRightPadding.Height));



                float innerWidth = (float)Math.Floor(imageBox.Width - Math.Abs(topLeftPadding.Width) - Math.Abs(bottomRightPadding.Width));
                float innerHeight = (float)Math.Floor(imageBox.Height - Math.Abs(topLeftPadding.Height) - Math.Abs(bottomRightPadding.Height));

                float x = 0;
                float y = 0;

                if (align == ContentAlignment.BottomCenter || align == ContentAlignment.BottomLeft || align == ContentAlignment.BottomRight)
                    y = (innerHeight - watermarkSize.Height) + topLeftPadding.Height;

                if (align == ContentAlignment.MiddleCenter || align == ContentAlignment.MiddleLeft || align == ContentAlignment.MiddleRight)
                    y = (innerHeight - watermarkSize.Height) / 2 + topLeftPadding.Height;

                if (align == ContentAlignment.TopCenter || align == ContentAlignment.TopLeft || align == ContentAlignment.TopRight)
                    y = topLeftPadding.Height;


                if (align == ContentAlignment.BottomRight || align == ContentAlignment.MiddleRight || align == ContentAlignment.TopRight)
                    x = (innerWidth - watermarkSize.Width) + topLeftPadding.Width;

                if (align == ContentAlignment.BottomCenter || align == ContentAlignment.MiddleCenter || align == ContentAlignment.TopCenter)
                    x = (innerWidth - watermarkSize.Width) / 2 + topLeftPadding.Width;

                if (align == ContentAlignment.BottomLeft || align == ContentAlignment.MiddleLeft || align == ContentAlignment.TopLeft)
                    x = topLeftPadding.Width;

                //Draw watermark
                g.DrawImage(wb, new Rectangle((int)(x + imageBox.X), (int)(y + imageBox.Y), (int)watermarkSize.Width, (int)watermarkSize.Height));

            }
            return true;
        }

        protected bool LegacyParseWatermark(ResizeSettings settings, out string watermark)
        {
            watermark = settings["watermark"]; //from the querystring
            if (string.IsNullOrEmpty(watermark) || watermarkDir == null) return false;

            if (watermark.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) > -1 ||
                watermark.IndexOfAny(new char[] { '\\', '/' }) > -1)
                return false;

            //Combine the directory with the base dir

            //Is watermarkDir a physical path?
            char slash = watermarkDir.Contains("/") ? '/' : '\\';

            watermark = watermarkDir.TrimEnd(slash) + slash + watermark.TrimStart(slash);

            //Verify the file exists if we're in ASP.NET. If the watermark doesn't exist, skip watermarking.
            if (!c.Pipeline.FileExists(watermark, settings) && slash == '/') return false;

            return true;
        }
    }
}
