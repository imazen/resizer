using System;
using System.Collections.Generic;
using System.Text;
using AForge.Imaging.Filters;
using System.Drawing.Imaging;
using AForge.Imaging;
using System.Drawing;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.AdvancedFilters {
    /// <summary>
    /// Allows for feathered edges.
    /// </summary>
    public class FeatherEdge : BaseInPlacePartialFilter {
        /// <summary>
        /// Declares the edges of the feathered image.
        /// </summary>
        /// <param name="outerAlpha"></param>
        /// <param name="innerAlpha"></param>
        /// <param name="width"></param>
        public FeatherEdge(double outerAlpha, double innerAlpha, double width ):this() {
            this.OuterAlpha = outerAlpha;
            this.InnerAlpha = innerAlpha;
            this.Width = width;

        }
        /// <summary>
        /// Outside limit of the feathered edge.
        /// </summary>
        public double OuterAlpha { get; set; }
        /// <summary>
        /// Inside limit of the feathered edge.
        /// </summary>
        public double InnerAlpha { get; set; }
        /// <summary>
        /// Width of the feathered edge.
        /// </summary>
        public double Width { get; set; }

        private Dictionary<PixelFormat, PixelFormat> formatTranslations = new Dictionary<PixelFormat, PixelFormat>();
        public override Dictionary<PixelFormat, PixelFormat> FormatTranslations {
            get { return formatTranslations; }
        }


        /// <summary>
        /// Creates a new instance of FeatherEdge.
        /// </summary>
        public FeatherEdge() {
            formatTranslations[PixelFormat.Format32bppArgb] = PixelFormat.Format32bppArgb;
        }





        protected override unsafe void ProcessFilter(UnmanagedImage image, Rectangle rect) {
            int pixelSize = (image.PixelFormat == PixelFormat.Format8bppIndexed) ? 1 :
                (image.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;

            if (pixelSize != 4) throw new InvalidImagePropertiesException();

            int startX = rect.Left;
            int startY = rect.Top;
            int stopX = startX + rect.Width;
            int stopY = startY + rect.Height;
            int stride = image.Stride;
            int offset = stride - rect.Width * pixelSize;

            int numberOfPixels = (stopX - startX) * (stopY - startY);

            // color image
            byte* ptr = (byte*)image.ImageData.ToPointer();
            // allign pointer to the first pixel to process
            ptr += (startY * stride + startX * pixelSize);


            double width = Width;
            double inner = InnerAlpha;
            double outer = OuterAlpha;
            double diff =  OuterAlpha - InnerAlpha;
            const short a = RGB.A;
            int w = (int)Math.Round(width);


            for (int y = startY; y < stopY; y++) {
                int ydist = Math.Max(0, Math.Max(startY + w - y, y - (stopY - 1 - w)));

                for (int x = startX; x < stopX; x++, ptr += pixelSize) {
                    int xdist = Math.Max(0, Math.Max(startX + w - x, x - (stopX - 1 - w)));
                    double dist = xdist > 0 && ydist > 0 ? Math.Round(Math.Sqrt(xdist * xdist + ydist * ydist)): Math.Max(xdist,ydist);

                    if (dist <= 0 || w == 0) {
                        ptr[a] = (byte)Math.Round((double)ptr[a] * inner);
                    } else if (dist > w){
                        ptr[a] = (byte)Math.Round((double)ptr[a] * outer);
                    } else {
                        double t = dist / width;
                        //t = Math.Sin(Math.PI * t / 2);
                        t = 3 * t * t - 2 * t * t * t;
                        //t = 6 * Math.Pow(t, 5) - 15 * Math.Pow(t, 4) + 10 * Math.Pow(t, 3);
                        ptr[a] = (byte)Math.Round((double)ptr[a] * (inner + diff * t));
                    }
                }
                ptr += offset;
            }
        }

    }
}
