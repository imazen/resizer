using System.Drawing;
using AForge.Imaging;
using System.Drawing.Imaging;
using System;
using ImageResizer.Plugins.RedEye;
using AForge.Imaging.Filters;


namespace ImageResizer.Plugins.RedEye {
    /// <summary>
    /// Looks for the 'best' pixel within a given radius from a specified point, where 'best' is the brightest pixel after applying a red-eye filter. Uses weighted evaluation.
    /// </summary>
    public class ManualSearcher {

        /// <summary>
        /// Looks for the brightest pixel after applying a redness filter. Narrows search first using a resampled copy of the image to eliminate edge dots. 
        /// Expects an image that is already cropped to the interested area for faster processing.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="mouse"></param>
        /// <param name="maxDistanceFromMouse"></param>
        /// <returns></returns>
        public unsafe Point FindMaxPixel(UnmanagedImage img, PointF mouse, float maxDistanceFromMouse) {
            int width = 15;
            int height = (int)Math.Ceiling((double)img.Height / (double)img.Width * width);

            if (width <= img.Width && height <= img.Height + 1) {
                width = img.Width;
                height = img.Height;
            }

            double scale = (double)img.Width / (double)width;

            UnmanagedImage lowRed = null;
            try {
                if (width != img.Width && height != img.Height) {
                    using (Bitmap reduced = new Bitmap(width, height, PixelFormat.Format24bppRgb))
                    using (Graphics g = Graphics.FromImage(reduced))
                    using (ImageAttributes ia = new ImageAttributes()) {
                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                        g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        ia.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                        g.DrawImage(img.ToManagedImage(false), new Rectangle(0, 0, width, height), 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, ia);
                        //TODO: Not sure if ToManagedImage will stick around after the underying image is disposed. I know that the bitmap data will be gone, guess that's most of it.
                        using (UnmanagedImage rui = UnmanagedImage.FromManagedImage(reduced)) {
                            lowRed = new RedEyeFilter(2).Apply(rui); // Make an copy using the red eye filter
                        }
                    }
                } else {
                    //Don't resample unless needed
                    lowRed = new RedEyeFilter(2).Apply(img);
                }


                Point max = GetMax(lowRed, new PointF(mouse.X / (float)scale, mouse.Y / (float)scale), maxDistanceFromMouse / scale);

                //We weren't scaling things? OK, cool...
                if (scale == 0) return max;

                //Otherwise, let's get the unscaled pixel. 
                //Calculate the rectangle surrounding the selected pixel, but in source coordinates.
                int tinySize = (int)Math.Ceiling(scale) + 1;
                Rectangle tinyArea = new Rectangle((int)Math.Floor(scale * (double)max.X), (int)Math.Floor(scale * (double)max.Y), tinySize, tinySize);
                if (tinyArea.Right >= img.Width) tinyArea.Width -= img.Width - tinyArea.Right + 1;
                if (tinyArea.Bottom >= img.Height) tinyArea.Height -= img.Height - tinyArea.Bottom + 1;
                //Filter it and look
                using (UnmanagedImage tiny = new Crop(tinyArea).Apply(img)) {
                    using (UnmanagedImage tinyRed = new RedEyeFilter(2).Apply(tiny)) {
                        max = GetMax(tinyRed);
                        max.X += tinyArea.X;
                        max.Y += tinyArea.Y;
                    }
                }
                return max;
            } finally {
                if (lowRed != null) lowRed.Dispose();
            }

        }
        /// <summary>
        /// Searches for the brightest pixel in an 8-bit image. Optionally weights the pixel values using a cosine curve based on the ratio of the (pixel's distance from 'weight') to 'maxDistance'.
        /// </summary>
        /// <param name="red"></param>
        /// <param name="weight"></param>
        /// <param name="maxDistance"></param>
        /// <returns></returns>
        private unsafe Point GetMax(UnmanagedImage red, PointF weight = default(PointF), double maxDistance = -1) {
            int width = red.Width;
            int height = red.Height;
            Point max = new Point(0, 0);
            byte maxVal = 0;
            double dist;
            //Now, let's find the max point. 
            for (int y = 0; y < height; y++) {
                byte* px = (byte*)red.ImageData + y * red.Stride;
                for (int x = 0; x < width; x++) {
                    if (maxDistance > 0) {
                        dist = Math.Sqrt((weight.X - x) * (weight.X - x) + (weight.Y - y) * (weight.Y - y));
                        if (dist < maxDistance) {
                            byte val = (byte)((double)*px * Math.Cos(dist / maxDistance * Math.PI / 2));
                            if (val > maxVal) {
                                maxVal = val;
                                max = new Point(x, y);
                            }
                        }
                    } else {

                        if (*px > maxVal) {
                            maxVal = *px;
                            max = new Point(x, y);
                        }
                    }
                    px++;
                }
            }
            return max;
        }
    }
}