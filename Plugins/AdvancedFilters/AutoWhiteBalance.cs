using System;
using System.Collections.Generic;
using System.Text;
using AForge.Imaging.Filters;
using System.Drawing.Imaging;
using AForge.Imaging;
using System.Drawing;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.AdvancedFilters {

    public enum HistogramThresholdAlgorithm {
        /// <summary>
        /// Simple upper and lower usage thresholds are applied to the values in each channel's histogram to determine the input start/stop points for each individual channel. The start/stop points are used to calcualte the scale factor and offset for the channel.
        /// </summary>
        Simple,
        /// <summary>
        /// Threshold is applied based on cumulative area at the lower and upper ends of the histogram. Much larger thresholds are required for this than SimpleThreshold.
        /// </summary>
        [EnumString("true")]
        Area,
        /// <summary>
        /// Threshold is applied based on strangely skewed cumulative area, identical to the process used by GIMP
        /// </summary>
        GIMP
    }

    /// <summary>
    /// In-place auto white balance filter with adjustable thresholds and 3 strategies.
    /// </summary>
    public class AutoWhiteBalance : BaseInPlacePartialFilter {

        /// <summary>
        /// Creates an in-place auto-white-balance filter using the Area strategy and a threshold of 0.006 (0.6 percent)
        /// </summary>
        public AutoWhiteBalance()
            : this(HistogramThresholdAlgorithm.Area) {
        }

        /// <summary>
        /// Creates an in-place auto-white-balance filter, using the given strategy. If Simple is used, a threshold of 0.0006 is used; otherwise 0.006 is used.
        /// </summary>
        /// <param name="algorithm"></param>
        public AutoWhiteBalance(HistogramThresholdAlgorithm algorithm)
            : this(algorithm, algorithm == HistogramThresholdAlgorithm.Simple ? 0.0006 : 0.006) {
        }

        /// <summary>
        /// Creates an in-place auto-white-balance filter, using the given strategy and (0..1) percent threshold.
        /// </summary>
        /// <param name="algorithm"></param>
        /// <param name="threshold"></param>
        public AutoWhiteBalance(HistogramThresholdAlgorithm algorithm, double threshold) {
            formatTranslations[PixelFormat.Format8bppIndexed] = PixelFormat.Format8bppIndexed;
            formatTranslations[PixelFormat.Format24bppRgb] = PixelFormat.Format24bppRgb;
            formatTranslations[PixelFormat.Format32bppRgb] = PixelFormat.Format32bppRgb;
            formatTranslations[PixelFormat.Format32bppArgb] = PixelFormat.Format32bppArgb;

            this.Algorithm = algorithm;
            LowThreshold = HighThreshold = threshold;
        }
        /// <summary>
        /// The algorithm to use when determining the histogram sections to discard
        /// </summary>
        public HistogramThresholdAlgorithm Algorithm { get; set; }
        /// <summary>
        /// The usage threshold to use for lower channel values. Normally around 0.006 for area algorithms, or 0.0006 for iterative algorithms.
        /// </summary>
        public double LowThreshold { get; set; }
        /// <summary>
        /// The usage threshold to use for higher channel values. Normally around 0.006 for area algorithms, or 0.0006 for iterative algorithms.
        /// </summary>
        public double HighThreshold { get; set; }

        private Dictionary<PixelFormat, PixelFormat> formatTranslations = new Dictionary<PixelFormat, PixelFormat>();

 
        /// <summary>
        /// Defines which pixel formats are supported for source images
        /// and which pixel format will be used for resulting image.
        /// See AForge.Imaging.Filters.IFilterInformation.FormatTranslations for more info.
        /// </summary>
        public override Dictionary<PixelFormat, PixelFormat> FormatTranslations {
            get { return formatTranslations;}
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="rect"></param>
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

        private byte[] Equalize(int[] histogram, long totalPixels) {
            
            //Low and high indexes to stretch
            int low = 0; int high = 255;

            if (Algorithm == HistogramThresholdAlgorithm.Area)
                AreaThreshold(histogram, (double)totalPixels, LowThreshold, HighThreshold, out low, out high);
            else if (Algorithm == HistogramThresholdAlgorithm.GIMP)
                GIMPThreshold(histogram, (double)totalPixels, LowThreshold, HighThreshold, out low, out high);
            else
                SimpleThreshold(histogram, (double)totalPixels, LowThreshold, HighThreshold, out low, out high);

            //Calculate scale factor
            double scale = 255.0 / (double)(high - low);

            //Create the new, scaled mapping
            byte[] equalizedHistogram = new byte[256];
            for (int i = 0; i < 256; i++) {
                equalizedHistogram[i] = (byte)Math.Max(0,Math.Min(255,Math.Round(((double)i -low)* scale)));
            }

            return equalizedHistogram;
        }

        /// <summary>
        /// Simple iterative thresholding
        /// </summary>
        /// <param name="histogram"></param>
        /// <param name="totalPixels"></param>
        /// <param name="lowThreshold"></param>
        /// <param name="highThreshold"></param>
        /// <param name="low"></param>
        /// <param name="high"></param>
        private void SimpleThreshold(int[] histogram, double totalPixels, double lowThreshold, double highThreshold, out int low, out int high) {
            low = 0; high = 255;
            for (int i = 0; i < 256; i++) {
                if ((double)histogram[i] / totalPixels >= lowThreshold) {
                    low = i;
                    break;
                }
            }
            //Find high
            for (int i = 255; i >= 0; i--) {
                if ((double)histogram[i] / totalPixels >= highThreshold) {
                    high = i;
                    break;
                }
            }
        }

        /// <summary>
        /// Simple histogram area thresholding
        /// </summary>
        /// <param name="histogram"></param>
        /// <param name="totalPixels"></param>
        /// <param name="lowThreshold"></param>
        /// <param name="highThreshold"></param>
        /// <param name="low"></param>
        /// <param name="high"></param>
        private void AreaThreshold(int[] histogram, double totalPixels, double lowThreshold, double highThreshold, out int low, out int high) {
            low = 0; high = 255;
            double area = 0;
            for (int i = 0; i < 256; i++) {
                area += histogram[i];

                if (area / totalPixels > lowThreshold) {
                    low = i;
                    break;
                }
            }
            area = 0;
            for (int i = 255; i >= 0; i--) {
                area += histogram[i];
                if (area / totalPixels > highThreshold) {
                    high = i;
                    break;
                }
            }
        }

        /// <summary>
        /// Matches the GIMP white balance algorithm, if provided 0.006 and 0.006 for the thresholds
        /// </summary>
        /// <param name="histogram"></param>
        /// <param name="totalPixels"></param>
        /// <param name="lowThreshold"></param>
        /// <param name="highThreshold"></param>
        /// <param name="low"></param>
        /// <param name="high"></param>
        private void GIMPThreshold(int[] histogram, double totalPixels, double lowThreshold, double highThreshold, out int low, out int high) {
            low = 0; high = 255;
            double area = 0;

            double pct;
            double next_pct;

            for (int i = 0; i < 255; i++) {
                area += histogram[i];
                pct = area / totalPixels;
                next_pct = (area + histogram[i + 1]) / totalPixels;


                if (Math.Abs(pct - lowThreshold) < Math.Abs(next_pct - lowThreshold)) {
                    low = i + 1;
                    break;
                }
            }
            area = 0;

            //Find high
            for (int i = 255; i > 0; i--) {
                area += histogram[i];
                pct = area / totalPixels;
                next_pct = (area + histogram[i - 1]) / totalPixels;


                if (Math.Abs(pct - highThreshold) < Math.Abs(next_pct - highThreshold)) {
                    high = i - 1;
                    break;
                }
            }
        }

     


    }
}
