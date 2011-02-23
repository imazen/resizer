/**
 * Written by Nathanael Jones 
 * http://nathanaeljones.com
 * nathanael.jones@gmail.com
 * 
 * This file is for user extension and modification (although all the source is!)
 * No restrictions on distribution of this file.
 * 
 **/
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Drawing;
using System.Web;

namespace fbs.ImageResizer
{
    /// <summary>
    /// Provides extensibility points for drawing watermarks and even modifying resizing/image settings
    /// </summary>
    public class WatermarkSettings
    {
        public string watermarkFile = "~/image001.png";
        public SizeF watermarkSize = new SizeF(200, 73);
        public Boolean valuesPercentages = false;
        public Boolean keepAspectRatio = true;
        public SizeF topLeftPadding = new SizeF(10, 10);
        public SizeF bottomRightPadding = new SizeF(70, 5);
        public Boolean hideIfTooSmall = true;
        public System.Drawing.ContentAlignment align = ContentAlignment.BottomRight;
 
        string watermark = null;
        /// <summary>
        /// Creates a new WatermarkSettings class
        /// </summary>
        /// <param name="q"></param>
        public WatermarkSettings(NameValueCollection q)
        {
            watermark = q["watermark"];
        }

        /// <summary>
        /// Returns true if all settings are at their defaults and no proccessing is to occur.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return watermark == null; //Change this, should return false if any code is in ModifySettings or Process
            }
        }

        public Bitmap GetMemCachedBitmap(string localfile)
        {
            string key = localfile.ToLowerInvariant();
            Bitmap b = HttpContext.Current.Cache[key] as Bitmap;
            if (b != null) return b;

            b = new Bitmap(localfile);
            HttpContext.Current.Cache.Insert(key, b, new System.Web.Caching.CacheDependency(localfile));
            return b;
        }

        /// <summary>
        /// Executed prior to resizing. Permits modifications of geometry and effect settings
        /// </summary>
        /// <param name="rs"></param>
        /// <param name="opts"></param>
        public virtual void ModifySettings(ResizeSettings rs, ImageSettings opts, ImageFilter adjustments, ImageOutputSettings ios)
        {
        }

        /// <summary>
        /// Modify this to do watermarks. Executing after resizing is complete
        /// </summary>
        /// <param name="b"></param>
        /// <param name="g"></param>
        public virtual void Process(Bitmap b, Graphics g, RectangleF imageBox)
        {

            if ("true".Equals(watermark, StringComparison.OrdinalIgnoreCase))
            {
                //Load the file specified in the querystring,
                Bitmap wb = GetMemCachedBitmap(HttpContext.Current.Server.MapPath(watermarkFile));

                //If percentages, resolve to pixels
                if (valuesPercentages){
                    watermarkSize.Height *= imageBox.Height;
                    watermarkSize.Width *= imageBox.Width;
                    topLeftPadding.Height *= imageBox.Height;
                    topLeftPadding.Width *= imageBox.Width;
                    bottomRightPadding.Height *= imageBox.Height;
                    bottomRightPadding.Width *= imageBox.Width;
                }

                //Keep aspect ratio
                if (keepAspectRatio) watermarkSize = PolygonMath.ScaleInside(wb.Size, watermarkSize);
                

                //Floor all values
                watermarkSize = new SizeF((float)Math.Floor(watermarkSize.Width), (float)Math.Floor(watermarkSize.Height));
                topLeftPadding = new SizeF((float)Math.Floor(topLeftPadding.Width), (float)Math.Floor(topLeftPadding.Height));
                bottomRightPadding = new SizeF((float)Math.Floor(bottomRightPadding.Width), (float)Math.Floor(bottomRightPadding.Height));


                //Check boundingbox
                SizeF watermarkBoundingBox = new SizeF(watermarkSize.Width + topLeftPadding.Width + bottomRightPadding.Width,
                    watermarkSize.Height + topLeftPadding.Height + bottomRightPadding.Height);

                //Don't draw the watermark if it is too small.
                if (!PolygonMath.FitsInside(watermarkSize, imageBox.Size) && hideIfTooSmall) return;

                

                float innerWidth = (float)Math.Floor(imageBox.Width - topLeftPadding.Width - bottomRightPadding.Width);
                float innerHeight = (float)Math.Floor(imageBox.Height - topLeftPadding.Height - bottomRightPadding.Height);

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


            TrialWatermark(b, g);
        }

        private void TrialWatermark(Bitmap b, Graphics g)
        {
            //Only executes when built using "Trial Version" configuration
#if TRIAL
            if (new Random().Next(4) < 2)
                this.DrawString(b, g, "Unlicensed", FontFamily.GenericSansSerif, Color.FromArgb(70, Color.White));
#endif
        }

        public virtual void DrawString(Bitmap b, Graphics g, String text, FontFamily ff, Color c)
        {
            SizeF size = g.MeasureString(text, new Font(ff, 32));
            double difX = (size.Width - b.Width) / -size.Width;

            double difY = (size.Height - b.Height) / -size.Height;
            float finalFontSize = 32 + (float)(32 * Math.Min(difX, difY));
            SizeF finalSize = g.MeasureString(text, new Font(ff, finalFontSize));

            g.DrawString(text, new Font(ff, finalFontSize), new SolidBrush(c),
                new PointF((b.Width - finalSize.Width) / 2, (b.Height - finalSize.Height) / 2));
            g.Flush();
        }
    }
}
