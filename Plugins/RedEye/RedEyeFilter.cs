using System;
using System.Collections.Generic;
using System.Text;
using AForge.Imaging.Filters;
using AForge.Imaging;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageResizer.Plugins.RedEye {

    
    /// <summary>
    /// Turns color images into grayscale images using a specialized filter that makes the red eyes stand out brightly against their background..
    /// </summary>
    public class RedEyeFilter : BaseFilter {

        // private format translation dictionary
        private Dictionary<PixelFormat, PixelFormat> formatTranslations = new Dictionary<PixelFormat, PixelFormat>();

        /// <summary>
        /// Format translations dictionary.
        /// </summary>
        public override Dictionary<PixelFormat, PixelFormat> FormatTranslations  {
            get { return formatTranslations; }
        }

        public short Algorithm { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedEyeFilter"/> class.
        /// </summary>
        public RedEyeFilter() {
            // initialize format translation dictionary
            formatTranslations[PixelFormat.Format24bppRgb] = PixelFormat.Format8bppIndexed;
            formatTranslations[PixelFormat.Format32bppRgb] = PixelFormat.Format8bppIndexed;
            formatTranslations[PixelFormat.Format32bppArgb] = PixelFormat.Format8bppIndexed;
            Algorithm = 2;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RedEyeFilter"/> class.
        /// </summary>
        /// <param name="alg">The algorithm to use for filtering the image.</param>
        public RedEyeFilter(short alg)
            : this() {
            this.Algorithm = alg;
        }

        /// <summary>
        /// Process the filter on the specified image.
        /// </summary>
        /// <param name="sourceData">Source image data.</param>
        /// <param name="destinationData">Destination image data.</param>
        protected override unsafe void ProcessFilter(UnmanagedImage sourceData, UnmanagedImage destinationData) {
            // get width and height
            int width = sourceData.Width;
            int height = sourceData.Height;

            int pixelSize =   System.Drawing.Image.GetPixelFormatSize(sourceData.PixelFormat) / 8;
            int sum;

            var algorithm = Algorithm;

            if (pixelSize <= 4) {
                int srcOffset = sourceData.Stride - width * pixelSize;
                int dstOffset = destinationData.Stride - width;

                // do the job
                byte* src = (byte*)sourceData.ImageData.ToPointer();
                byte* dst = (byte*)destinationData.ImageData.ToPointer();

                for (int y = 0; y < height; y++) {
                    for (int x = 0; x < width; x++, src += pixelSize, dst++) {
                        if (src[RGB.R] == 0) continue;
                        if (algorithm == 0) {
                            //held
                            *dst = (byte)Math.Max(src[RGB.R] - Math.Min(src[RGB.G], src[RGB.B]), 0);
                        } else if (algorithm == 1) {
                            //normalized r channel
                            sum = (src[RGB.R] + src[RGB.G] + src[RGB.B]);
                            *dst = (sum != 0) ? (byte)(255 * src[RGB.R] / sum) : (byte)0;
                        } else if (algorithm == 2) {
                            //Smolka
                            *dst = src[RGB.R] == 0 ? (byte)0 : (byte)Math.Min(255, Math.Max(0, ((float)(src[RGB.R] - Math.Max(src[RGB.G], src[RGB.B])) * 255.0F / (float)src[RGB.R])));
                        } else if (algorithm == 3) {
                            //GS
                            *dst = (byte)Math.Pow((Math.Max(0, (src[RGB.R] * 2 - src[RGB.G] - src[RGB.B]) / src[RGB.R])), 2);

                        } else if (algorithm == 4) {
                            //Gabautz
                            *dst = (byte)Math.Min(255, (src[RGB.R] * src[RGB.R] / (src[RGB.G] * src[RGB.G] + src[RGB.B] * src[RGB.B] + 14)));
                        }
                    }
                    src += srcOffset;
                    dst += dstOffset;
                }
            } else throw new NotImplementedException();
        }
    }
    
}
