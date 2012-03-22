using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using AForge.Imaging;
using ImageResizer.Util;
using AForge.Imaging.Filters;

namespace ImageResizer.Plugins.WhitespaceTrimmer {
    public class BoundingBoxFinder {

        /// <summary>
        /// Returns a rectangle inside 'lookInside' that bounds any energy greater than 'threshold'. 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="lookInside">A rectangle of 'image' to look inside. </param>
        /// <param name="threshold">1-255, the energy threshold to detect activity. 80-150 is a good range.</param>
        /// <returns></returns>
        public Rectangle FindBoxSobel(Bitmap originalImage, Rectangle lookInside, byte threshold) {

            Bitmap image = originalImage;
            try {
                //Convert if needed (makes an extra copy)
                if (image.PixelFormat != PixelFormat.Format24bppRgb &&
                    image.PixelFormat != PixelFormat.Format32bppArgb &&
                    image.PixelFormat != PixelFormat.Format32bppRgb) {
                    image = AForge.Imaging.Image.Clone(image, PixelFormat.Format24bppRgb);
                }

                //Crop if needed (makes an extra copy unless we converted too, then only 1 extra copy)
                if (!lookInside.Equals(new Rectangle(0, 0, image.Width, image.Height))) {
                    Bitmap oldImage = image;
                    try {
                        image = new Crop(PolygonMath.ToRectangle(lookInside)).Apply(image);
                    } finally {
                        if (oldImage != originalImage) oldImage.Dispose(); //Dispose the cloned 
                    }
                }


                //Makes 1 more copy at 1/3rd the size, in grayscale
                Rectangle result = FindBoxSobel(image, threshold);
                return new Rectangle(lookInside.X + result.X, lookInside.Y + result.Y, result.Width, result.Height);


            } finally {
                if (image != originalImage) image.Dispose();
            }

        }

        public Rectangle FindBoxSobel(Bitmap rgb, byte threshold) {
            using (Bitmap gray = Grayscale.CommonAlgorithms.Y.Apply(rgb)) {

                //Apply sobel operator to grayscale image
                new SobelEdgeDetector().ApplyInPlace(gray);
                //Threshold into black and white.
                new Threshold(threshold).ApplyInPlace(gray);
                //Trim only exact black pixels
                // lock source bitmap data
                BitmapData data = gray.LockBits(new Rectangle(0, 0, gray.Width, gray.Height), ImageLockMode.ReadOnly, gray.PixelFormat);
                try {
                    return FindBoxExact(data, Color.Black);
                } finally {
                    gray.UnlockBits(data);
                }
            }
        }
        /// <summary>
        /// Returns a bounding box that only excludes the specified color.
        /// </summary>
        /// <param name="sourceData"></param>
        /// <param name="colorToRemove"></param>
        /// <returns></returns>
        public Rectangle FindBoxExact(BitmapData sourceData, Color colorToRemove) {
            
            // get source image size
            int width = sourceData.Width;
            int height = sourceData.Height;
            int offset = sourceData.Stride -
                ((sourceData.PixelFormat == PixelFormat.Format8bppIndexed) ? width : width * 3);

            // color to remove
            byte r = colorToRemove.R;
            byte g = colorToRemove.G;
            byte b = colorToRemove.B;

            int minX = width;
            int minY = height;
            int maxX = 0;
            int maxY = 0;

            // find rectangle which contains something except color to remove
            unsafe {
                byte* src = (byte*)sourceData.Scan0;

                if (sourceData.PixelFormat == PixelFormat.Format8bppIndexed) {
                    // grayscale
                    for (int y = 0; y < height; y++) {
                        for (int x = 0; x < width; x++, src++) {
                            if (*src != g) {
                                if (x < minX)
                                    minX = x;
                                if (x > maxX)
                                    maxX = x;
                                if (y < minY)
                                    minY = y;
                                if (y > maxY)
                                    maxY = y;
                            }
                        }
                        src += offset;
                    }
                } else {
                    // RGB
                    for (int y = 0; y < height; y++) {
                        for (int x = 0; x < width; x++, src += 3) {
                            if (
                                (src[RGB.R] != r) ||
                                (src[RGB.G] != g) ||
                                (src[RGB.B] != b)) {
                                if (x < minX)
                                    minX = x;
                                if (x > maxX)
                                    maxX = x;
                                if (y < minY)
                                    minY = y;
                                if (y > maxY)
                                    maxY = y;
                            }
                        }
                        src += offset;
                    }
                }
            }

            // check
            if ((minX == width) && (minY == height) && (maxX == 0) && (maxY == 0)) {
                minX = minY = 0;
            }

            return new Rectangle(minX,minY,maxX - minX + 1, maxY - minY + 1);
        }
    }
}
