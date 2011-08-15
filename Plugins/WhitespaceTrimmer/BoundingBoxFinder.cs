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
        public Rectangle FindBoxSobel(Bitmap image, Rectangle lookInside, byte threshold) {


            bool disposeImage = false;
            try {
                if (image.PixelFormat != PixelFormat.Format24bppRgb && 
                    image.PixelFormat != PixelFormat.Format32bppArgb && 
                    image.PixelFormat != PixelFormat.Format32bppRgb) {
                        image = AForge.Imaging.Image.Clone(image, PixelFormat.Format24bppRgb);
                        disposeImage = true;
                }
                
                //We do this mess of try/finally so we never have more than 1 copy in memory at a time.
                UnmanagedImage croppedCopy = null;
                UnmanagedImage grayscaleCopy = null;
                
                Rectangle imageSize = new Rectangle(0, 0, image.Width, image.Height);
                try {
                    try {
                        // lock source bitmap data
                        BitmapData data = image.LockBits(imageSize, ImageLockMode.ReadWrite, image.PixelFormat);
                        try {

                            //Crop original image if needed.
                            if (lookInside.Equals(imageSize))
                                grayscaleCopy = Grayscale.CommonAlgorithms.Y.Apply(new UnmanagedImage(data));
                            else
                                croppedCopy = new Crop(PolygonMath.ToRectangle(lookInside)).Apply(new UnmanagedImage(data));
                        } finally {
                            image.UnlockBits(data);
                        }
                        //Convert to 8bpp grayscale
                        if (grayscaleCopy == null) grayscaleCopy = Grayscale.CommonAlgorithms.Y.Apply(croppedCopy);
                    } finally {
                        if (croppedCopy != null) croppedCopy.Dispose(); ;
                    }
                    //Apply sobel operator to grayscale image
                    new SobelEdgeDetector().ApplyInPlace(grayscaleCopy);
                    //Threshold into black and white.
                    new Threshold(threshold).ApplyInPlace(grayscaleCopy);
                    //Trim only exact black pixels
                    Rectangle result = FindBoxExact(grayscaleCopy, Color.Black);
                    return new Rectangle(lookInside.X + result.X, lookInside.Y + result.Y, result.Width, result.Height);
                } finally {
                    if (grayscaleCopy != null) grayscaleCopy.Dispose();
                }
            } finally {
                if (disposeImage) image.Dispose();
            }

        }
        /// <summary>
        /// Returns a bounding box that only excludes the specified color.
        /// </summary>
        /// <param name="sourceData"></param>
        /// <param name="colorToRemove"></param>
        /// <returns></returns>
        public Rectangle FindBoxExact(UnmanagedImage sourceData, Color colorToRemove) {

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
                byte* src = (byte*)sourceData.ImageData.ToPointer();

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
