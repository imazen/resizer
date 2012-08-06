using System;
using System.Collections.Generic;
using System.Text;
using AForge.Imaging.Filters;
using System.Drawing.Imaging;
using AForge.Imaging;
using System.Drawing;

namespace ImageResizer.Plugins.AdvancedFilters {
    public class AutoWhiteBalance : BaseInPlacePartialFilter {

        private Dictionary<PixelFormat, PixelFormat> formatTranslations = new Dictionary<PixelFormat, PixelFormat>();
        public override Dictionary<PixelFormat, PixelFormat> FormatTranslations {
            get { return formatTranslations; }
        }

        public AutoWhiteBalance() {
            formatTranslations[PixelFormat.Format8bppIndexed] = PixelFormat.Format8bppIndexed;
            formatTranslations[PixelFormat.Format24bppRgb] = PixelFormat.Format24bppRgb;
            formatTranslations[PixelFormat.Format32bppRgb] = PixelFormat.Format32bppRgb;
            formatTranslations[PixelFormat.Format32bppArgb] = PixelFormat.Format32bppArgb;
        }


        protected override unsafe void ProcessFilter(UnmanagedImage image, Rectangle rect) {
            int pixelSize = (image.PixelFormat == PixelFormat.Format8bppIndexed) ? 1 :
                (image.PixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;

            int startX = rect.Left;
            int startY = rect.Top;
            int stopX = startX + rect.Width;
            int stopY = startY + rect.Height;
            int stride = image.Stride;
            int offset = stride - rect.Width * pixelSize;

            int numberOfPixels = (stopX - startX) * (stopY - startY);

            // check image format
            if (image.PixelFormat == PixelFormat.Format8bppIndexed) {
                // grayscale image
                byte* ptr = (byte*)image.ImageData.ToPointer();
                // allign pointer to the first pixel to process
                ptr += (startY * stride + startX);

                // calculate histogram
                int[] histogram = new int[256];
                for (int y = startY; y < stopY; y++) {
                    for (int x = startX; x < stopX; x++, ptr++) {
                        histogram[*ptr]++;
                    }
                    ptr += offset;
                }

                // calculate new intensity levels
                byte[] equalizedHistogram = Equalize(histogram, numberOfPixels);

                // update pixels' intensities
                ptr = (byte*)image.ImageData.ToPointer();
                // allign pointer to the first pixel to process
                ptr += (startY * stride + startX);

                for (int y = startY; y < stopY; y++) {
                    for (int x = startX; x < stopX; x++, ptr++) {
                        *ptr = equalizedHistogram[*ptr];
                    }
                    ptr += offset;
                }
            } else {
                // color image
                byte* ptr = (byte*)image.ImageData.ToPointer();
                // allign pointer to the first pixel to process
                ptr += (startY * stride + startX * pixelSize);

                // calculate histogram
                int[] histogramR = new int[256];
                int[] histogramG = new int[256];
                int[] histogramB = new int[256];

                for (int y = startY; y < stopY; y++) {
                    for (int x = startX; x < stopX; x++, ptr += pixelSize) {
                        histogramR[ptr[RGB.R]]++;
                        histogramG[ptr[RGB.G]]++;
                        histogramB[ptr[RGB.B]]++;
                    }
                    ptr += offset;
                }

                // calculate new intensity levels
                byte[] equalizedHistogramR = Equalize(histogramR, numberOfPixels);
                byte[] equalizedHistogramG = Equalize(histogramG, numberOfPixels);
                byte[] equalizedHistogramB = Equalize(histogramB, numberOfPixels);

                // update pixels' intensities
                ptr = (byte*)image.ImageData.ToPointer();
                // allign pointer to the first pixel to process
                ptr += (startY * stride + startX * pixelSize);

                for (int y = startY; y < stopY; y++) {
                    for (int x = startX; x < stopX; x++, ptr += pixelSize) {
                        ptr[RGB.R] = equalizedHistogramR[ptr[RGB.R]];
                        ptr[RGB.G] = equalizedHistogramG[ptr[RGB.G]];
                        ptr[RGB.B] = equalizedHistogramB[ptr[RGB.B]];
                    }
                    ptr += offset;
                }
            }
        }

        private double truncateLow = 0.0005f;
        private double truncateHigh = 0.0005f;

        // Histogram 
        private byte[] Equalize(int[] histogram, long numPixel) {
            
            //Low and high indexes to stretch
            int low = 0; int high = 255;

            double totalPixels = (double)numPixel;

            for (int i = 0; i < 256; i++) {
                if ((double)histogram[i] / numPixel > truncateLow) {
                    low = i;
                    break;
                }
            }
            //Find high
            for (int i = 255; i >= 0; i--) {
                if ((double)histogram[i] / totalPixels > truncateHigh) {
                    high = i;
                    break;
                }
            }

            //Calculate scale factor
            double scale = 255.0 / (double)(high - low);

            //Create the new, scaled mapping
            byte[] equalizedHistogram = new byte[256];
            for (int i = 0; i < 256; i++) {
                equalizedHistogram[i] = (byte)Math.Max(0,Math.Min(255,Math.Round(((double)i * scale) - low )));
            }

            return equalizedHistogram;
        }
    }
}
