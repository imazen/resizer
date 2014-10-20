/* 
 * Copyright (c) 2014 Imazen See license.txt for your rights 
 * 
 * This code has been *heavily* modified from its original versions
 * 
 * Derived from: http://codebetter.com/brendantompkins/2007/06/14/gif-image-color-quantizer-now-with-safe-goodness/
 * 
 * Portions of this file are under the following license:
 * 
 * THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
 * ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
 * PARTICULAR PURPOSE. 
 *
 * This is sample code and is freely distributable. 
 */

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ImageResizer.Plugins.PrettyGifs

  
{
    /// <summary>
    /// Abstract Quantizer class - handles the messy, algorithm-independent details of quantization. 
    /// Subclasses must implement InitialQuantizePixel, GetPallete(), and QuantizePixel. Not thread-safe!
    /// </summary>
    public abstract class Quantizer
    {

        private bool _fixedPalette = false;
        /// <summary>
        /// (Readonly) If true, the algorithm can do everything in QuantizePixel, and InitialQuantizePixel will not be called. Implies ResizeForFirstPass=False and FourPass=false=
        /// </summary>
        public bool FixedPalette {
            get { return _fixedPalette; }
        }


        private int _pixelSize;
        /// <summary>
        /// The number of bytes in a ARGB structure. Should be 4
        /// </summary>
        public int PixelSize {
            get { return _pixelSize; }
        }

        private bool _fullTrust = true;
        /// <summary>
        /// If true, pointer arithmetic will be used instead of GetPixel. GetPixel is much slower. If false, OmitFinalStage will be assumed true, as only palette generation is possible in low trust.
        /// Defaults to true.
        /// </summary>
        public bool FullTrust {
            get { return _fullTrust; }
            set { _fullTrust = value; }
        }


        private bool _resizeForFirstPass = false;
        /// <summary>
        /// If true, the first pass (InitialQuantizePixel) will be performed on a size-limited version of the original image to control performance. Ignored if FixedPalette=True
        /// </summary>
        public bool ResizeForFirstPass {
            get { return _resizeForFirstPass; }
            set { _resizeForFirstPass = value; }
        }

        private long _firstPassPixelCount = 256 * 256;
        /// <summary>
        /// The approximate number of pixels to use when making a scaled copy of the image for the first pass. Only used when ResizeForFirstPass=True and FirstPassPixelThreshold is exceeded.
        /// </summary>
        public long FirstPassPixelCount {
            get { return _firstPassPixelCount; }
            set { _firstPassPixelCount = value; }
        }

        private long _firstPassPixelThreshold = 512 * 512;
        /// <summary>
        /// The maximum number of pixels the original image may contain before a scaled copy is made for the first pass. 
        /// Only relevant when ResizeForFirstPass=True
        /// </summary>
        public long FirstPassPixelThreshold {
            get { return _firstPassPixelThreshold; }
            set { _firstPassPixelThreshold = value; }
        }


        private bool _fourPass = false;

        /// <summary>
        /// If true, image is re-paletted after quantization - forces 2 clones of the original image to be created. FixedPalette and OmitFinalStage should be false if this is used.
        /// </summary>
        public bool FourPass {
            get { return _fourPass; }
            set { _fourPass = value; }
        }


        private bool _omitFinalStage = false;
        /// <summary>
        /// If true, a 32-bit image with an 8-bit palette will be returned instead of an 8-bit image, which GDI can save using median-cut quantization. Much faster than our final quantization pass, although it can't do transparency.
        /// Assumed true if FullTrust is false.
        /// </summary>
        public bool OmitFinalStage {
            get { return _omitFinalStage; }
            set { _omitFinalStage = value; }
        }


        /// <summary>
        /// Construct the quantizer
        /// </summary>
        /// <param name="fixedPalette">If true, the quantization only needs to loop through the source pixels once - InitialQuantiize</param>
        /// <remarks>
        /// If you construct this class with a true value for singlePass, then the code will, when quantizing your image,
        /// only call the 'QuantizeImage' function. If two passes are required, the code will call 'InitialQuantizeImage'
        /// and then 'QuantizeImage'.
        /// </remarks>
        public Quantizer(bool fixedPalette)
        {
            _fixedPalette = fixedPalette;
            _pixelSize = Marshal.SizeOf(typeof (Color32));
        }

        /// <summary>
        /// Resets the quantizer so it can process a new image. 
        /// </summary>
        public virtual void Reset()
        {
            this.secondPassIntermediate = null;
            this.secondPassX = 0;
            this.secondPassY = 0;
          
        }

        protected virtual void ValidatePropertyValues() {
            if (!FullTrust && !OmitFinalStage) throw new Exception("If FullTrust=False, OmitFinalStage must be set to true. The final stage requires full trust.");
            if (!FullTrust && FourPass) throw new Exception("If FullTrust=False, FourPass must be false also. Four-pass quantization requires full trust.");


        }

        /// <summary>
        /// Quantize an image and return the resulting output bitmap
        /// </summary>
        /// <param name="src">The image to quantize</param>
        /// <returns>A quantized version of the image</returns>
        public Bitmap Quantize(Image src) {
            //We just set up the Bitmap copies and handle their disposal - the real work happens in 
            // QuantizeFullTrust and QuantizeLowTrust
            Bitmap firstPass = null;
            Bitmap copy = null;
            Bitmap copy2 = null;
            Bitmap tempOutput = null;
            Bitmap result = null;
            try {
                // First off take a 32bpp copy of 'source' if it's not a 32bpp Bitmap instance.
                copy = src as Bitmap;
                if (FourPass || copy == null || !src.PixelFormat.Equals(PixelFormat.Format32bppArgb)) {
                    copy = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);

                    // Now lock the bitmap into memory
                    using (Graphics g = Graphics.FromImage(copy)) {
                        g.PageUnit = GraphicsUnit.Pixel;

                        // Draw the source image onto the copy bitmap,
                        // which will effect a widening as appropriate.
                        g.DrawImage(src, new Point(0, 0));

                    }
                }

                firstPass = copy;
                //If we should make a resized version for the first pass, let's do it.
                if (!FixedPalette && ResizeForFirstPass && FirstPassPixelThreshold < copy.Width * copy.Height) {
                    double factor = FirstPassPixelCount / ((double)copy.Width * (double)copy.Height);
                    firstPass = new Bitmap((int)Math.Floor((double)copy.Width * factor), (int)Math.Floor((double)copy.Height * factor), PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(firstPass)) {
                        //Use the low-quality settings - we want the original colors of the image, nearest neighbor is better than bicubic spline here.
                        g.PageUnit = GraphicsUnit.Pixel;
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                        g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                        g.DrawImage(copy, 0, 0, firstPass.Width, firstPass.Height);
                    }
                }
                copy2 = null;
                if (FourPass) {
                    copy2 = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(copy2)) {
                        g.PageUnit = GraphicsUnit.Pixel;
                        g.DrawImage(src, new Point(0, 0));
                    }
                }
                // And make an 8-bit output image
                tempOutput = new Bitmap(OmitFinalStage ? 2 : src.Width, OmitFinalStage ? 2 : src.Height, PixelFormat.Format8bppIndexed);

                //Full trust and low trust are implemented differently. 
                if (FullTrust) {
                    result = QuantizeFullTrust(firstPass, copy, copy2, tempOutput);
                } else {
                    result = QuantizeLowTrust(firstPass, copy, copy2, tempOutput);
                }
                return result;
            } finally {
                if (firstPass != null && firstPass != copy && firstPass != src && firstPass != result) firstPass.Dispose();
                if (copy != null && copy != src && copy != result) copy.Dispose();
                if (copy2 != null && copy2 != src && copy2 != result) copy2.Dispose();
                if (tempOutput != null && tempOutput != src && tempOutput != result) tempOutput.Dispose();
            }

        }
        


        protected Bitmap QuantizeFullTrust(Bitmap firstPass, Bitmap copy, Bitmap copy2, Bitmap output) {
            Rectangle bounds = new Rectangle(0, 0, copy.Width, copy.Height);
            int width = copy.Width;
            int height = copy.Height;

            //On a fixed palette, AnalyzeImage is never called.
            if (FixedPalette) {
                if (OmitFinalStage) {
                    copy.Palette = GetPalette(output.Palette); //We have to reuse the palette structure since we can't make new ones.
                    return copy;
                } else {
                    output.Palette = GetPalette(output.Palette);
                    //Lock and quantize
                    BitmapData copyData = null;
                    try {
                        copyData = copy.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                        QuantizeImage(copyData, null, output, width, height, bounds);
                        return output;
                    } finally {
                        copy.UnlockBits(copyData);
                    }
                }
            }

            //This is our standard quantize, calling AnalyzeImage and QuantizeImage once each
            if (!FourPass) {
                BitmapData firstPassData = null;
                try { 
                    firstPassData = firstPass.LockBits(new Rectangle(0, 0, firstPass.Width, firstPass.Height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                    //Analyze image and build data structures so we can generate a palette
                    AnalyzeImage(firstPassData, firstPass.Width, firstPass.Height);
                    // Then set the color palette on the output bitmap, using the existing one since no ctor exists.
                    output.Palette = GetPalette(output.Palette);
                    if (OmitFinalStage) {
                        copy.Palette = output.Palette;
                        return copy;
                    } else if (firstPass == copy) {
                        //In case we didn't resize for the first pass, reuse data
                        QuantizeImage(firstPassData, null, output, width, height, bounds);
                        return output;
                    } else {
                        BitmapData copyData = null;
                        try {
                            copyData = copy.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                            QuantizeImage(copyData, null, output, width, height, bounds);
                            return output;
                        } finally {
                            copy.UnlockBits(copyData);
                        }
                    }
                } finally {
                    firstPass.UnlockBits(firstPassData);
                }
            }

            //With a 4-pass algorithm, we 
            if (FourPass) {
                // Define a pointer to the bitmap data
                BitmapData sourceData = null;
                BitmapData intermediateData = null;
                try {
                    // Get the source image bits and lock into memory
                    sourceData = copy.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    intermediateData = copy2.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                    //Analyze and generate an intermediate palette
                    AnalyzeImage(sourceData, width, height);
                    output.Palette = GetPalette(output.Palette);
                    //Run first pass
                    QuantizeImage(sourceData, intermediateData, output, width, height, bounds);
                    //TODO: document
                    AnalyzeImage(intermediateData, width, height);
                    output.Palette = GetPalette(output.Palette);
                    QuantizeImage(intermediateData, null, output, width, height, bounds);
                    return output;
                } finally {
                    // Ensure that the bits are unlocked
                    copy.UnlockBits(sourceData);
                    if (intermediateData != null) copy2.UnlockBits(intermediateData);
                }
            }
            return null;
        }
        protected Bitmap QuantizeLowTrust(Bitmap firstPass, Bitmap copy, Bitmap copy2, Bitmap output) {
            Rectangle bounds = new Rectangle(0, 0, copy.Width, copy.Height);
            int width = copy.Width;
            int height = copy.Height;

            //On a fixed palette, AnalyzeImage is never called.
            if (!FixedPalette) {
                //Analyze image and build data structures so we can generate a palette
                AnalyzeImageLowTrust(firstPass, firstPass.Width, firstPass.Height);
            }

            copy.Palette = GetPalette(output.Palette); //We have to reuse the palette structure since we can't make new ones.
            return copy;

        }
        /// <summary>
        /// Execute the first pass through the pixels in the image
        /// </summary>
        /// <param name="sourceData">The source data</param>
        /// <param name="width">The width in pixels of the image</param>
        /// <param name="height">The height in pixels of the image</param>
        protected  virtual void AnalyzeImage(BitmapData sourceData, int width, int height)
        {
            // Define the source data pointers. The source row is a byte to
            // keep addition of the stride value easier (as this is in bytes)              
            IntPtr pSourceRow = sourceData.Scan0;

            // Loop through each row
            for (int row = 0; row < height; row++)
            {
                // Set the source pixel to the first pixel in this row
                IntPtr pSourcePixel = pSourceRow;

                // And loop through each column
                for (int col = 0; col < width; col++)
                {            
                    InitialQuantizePixel(new Color32(pSourcePixel)); 
                    pSourcePixel = (IntPtr)((long)pSourcePixel + PixelSize); //Increment afterwards
                }	// Now I have the pixel, call the FirstPassQuantize function...

                // Add the stride to the source row
                pSourceRow = (IntPtr)((long)pSourceRow + sourceData.Stride);
            }
        }
        protected virtual void AnalyzeImageLowTrust(Bitmap b, int width, int height) {
            // Loop through each row
            for (int row = 0; row < height; row++) {
                // And loop through each column
                for (int col = 0; col < width; col++) {
                    InitialQuantizePixel(new Color32(b.GetPixel(col, row)));
                }
            }
        }

        //For dithering - 5-18-09 ndj
        private BitmapData secondPassIntermediate;
        //private Bitmap secondPassIntermediateBitmap;
        private int secondPassX;
        private int secondPassY;

        /// <summary>
        /// Execute a second pass through the bitmap. If dithering is enabled, sourceData will be modified. 
        /// </summary>
        /// <param name="sourceData">The source bitmap, locked into memory</param>
        /// <param name="intermediate">The intermediate bitmap, used for 4-pass quantization. If specified, output will not actually be modified</param>
        /// <param name="output">The output bitmap</param>
        /// <param name="width">The width in pixels of the image</param>
        /// <param name="height">The height in pixels of the image</param>
        /// <param name="bounds">The bounding rectangle</param>
        protected virtual void QuantizeImage(BitmapData sourceData, BitmapData intermediate, Bitmap output, int width, int height, Rectangle bounds)
        {
            secondPassIntermediate = (intermediate != null) ? intermediate : sourceData;// Not thread safe.... But nothing here is anyways...//For dithering - 5-18-09 ndj
            BitmapData outputData = null;
            
            try
            {
                // Lock the output bitmap into memory
                if (intermediate == null) outputData = output.LockBits(bounds, ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

                // Define the source data pointers. The source row is a byte to
                // keep addition of the stride value easier (as this is in bytes)
                IntPtr pSourceRow = sourceData.Scan0;
                IntPtr pSourcePixel = pSourceRow;
                IntPtr pPreviousPixel = pSourcePixel;

                // Now define the destination data pointers
                IntPtr pDestinationRow = IntPtr.Zero;
                if (intermediate == null) pDestinationRow = outputData.Scan0;
                IntPtr pDestinationPixel = pDestinationRow;

                // And convert the first pixel, so that I have values going into the loop

                byte pixelValue = QuantizePixel(new Color32(pSourcePixel));

                // Assign the value of the first pixel
                if (intermediate == null) Marshal.WriteByte(pDestinationPixel, pixelValue);

                // Loop through each row
                for (int row = 0; row < height; row++)
                {
                    secondPassY = row;  //For dithering - 5-18-09 ndj
                    // Set the source pixel to the first pixel in this row
                    pSourcePixel = pSourceRow;

                    // And set the destination pixel pointer to the first pixel in the row
                    if (intermediate == null) pDestinationPixel = pDestinationRow;

                    // Loop through each pixel on this scan line
                    for (int col = 0; col < width; col++)
                    {
                        secondPassX = col; //For dithering - 5-18-09 ndj
                        // Check if this is the same as the last pixel. If so use that value
                        // rather than calculating it again. This is an inexpensive optimisation.
                        // Nathanael: 2-11-09 changed from ReadByte to ReadInt32 on both.
                        // Otherwise this comparison may return true if only the blue component is the 
                        // same in 2 subsequent pixels.
                        if (Marshal.ReadInt32(pPreviousPixel) != Marshal.ReadInt32(pSourcePixel) || (intermediate != null))
                        {
                            // Quantize the pixel
                            pixelValue = QuantizePixel(new Color32(pSourcePixel));

                            // And setup the previous pointer
                            pPreviousPixel = pSourcePixel;
                        }

                        // And set the pixel in the output
                        if (intermediate == null) Marshal.WriteByte(pDestinationPixel, pixelValue);

                        pSourcePixel = (IntPtr)((long)pSourcePixel + PixelSize);
                        if (intermediate == null) pDestinationPixel = (IntPtr)((long)pDestinationPixel + 1);

                    }

                    // Add the stride to the source row
                    pSourceRow = (IntPtr)((long)pSourceRow + sourceData.Stride);

                    // And to the destination row
                    if (intermediate == null) pDestinationRow = (IntPtr)((long)pDestinationRow + outputData.Stride);
                }
            }
            finally
            {
                // Ensure that I unlock the output bits
                if (intermediate == null) output.UnlockBits(outputData);
                secondPassIntermediate = null;
            }
        }

        /// <summary>
        /// Can only be called from QuantizePixel...
        ///This is how dithering is done... 
        ///5-18-09 ndj
        /// </summary>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        /// <param name="deltaR"></param>
        /// <param name="deltaG"></param>
        /// <param name="deltaB"></param>
        /// <param name="deltaA"></param>
        protected void AdjustNeighborSource(int offsetX, int offsetY, int deltaR, int deltaG, int deltaB, int deltaA)
        {
            if (secondPassIntermediate == null) return;
            int x = secondPassX + offsetX;
            int y = secondPassY + offsetY;
            if (x < 0 || x >= secondPassIntermediate.Width) return; //do nothing;
            if (y < 0 || y >= secondPassIntermediate.Height) return; //do nothing
            IntPtr p = (IntPtr)((long)secondPassIntermediate.Scan0 + ((long)y * (long)secondPassIntermediate.Stride) + (PixelSize * x));
            //Read the original color
            Color32 c = new Color32(p);

            c.Red = ToByte((int)c.Red + deltaR);
            c.Green = ToByte((int)c.Green + deltaG);
            c.Blue = ToByte((int)c.Blue + deltaB);
            c.Alpha = ToByte((int)c.Alpha + deltaA);
            
            Marshal.StructureToPtr(c, p, true); //False to not dispose old block. Since no reference to it exists (I believe PtrToStructure from Color32 copies, not references), this should be safe
  
        }
        //Truncates an int to a byte. 5-18-09 ndj
        protected byte ToByte(int i) {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return (byte)i;
        }
        //protected void AdjustNeighborSourceLowTrust(int offsetX, int offsetY, int deltaR, int deltaG, int deltaB, int deltaA) {
        //    if (secondPassIntermediateBitmap == null) return;
        //    int x = secondPassX + offsetX;
        //    int y = secondPassY + offsetY;
        //    if (x < 0 || x >= secondPassIntermediateBitmap.Width) return; //do nothing;
        //    if (y < 0 || y >= secondPassIntermediateBitmap.Height) return; //do nothing
        //    Color c = secondPassIntermediateBitmap.GetPixel(x, y);

        //    secondPassIntermediateBitmap.SetPixel(x, y, Color.FromArgb(ClampToByte(c.A + deltaA),
        //        ClampToByte(c.R + deltaR), ClampToByte(c.G + deltaG), ClampToByte(c.B + deltaB)));

        //}
        //protected int ClampToByte(int i) {
        //    if (i < 0) return 0;
        //    if (i > 255) return 255;
        //    return i;
        //}
        

        /// <summary>
        /// Override this to process the pixel in the first pass of the algorithm
        /// </summary>
        /// <param name="pixel">The pixel to quantize</param>
        /// <remarks>
        /// This function need only be overridden if your quantize algorithm needs two passes,
        /// such as an Octree quantizer.
        /// </remarks>
        protected virtual void InitialQuantizePixel(Color32 pixel)
        {
        }

        /// <summary>
        /// Override this to process the pixel in the second pass of the algorithm
        /// </summary>
        /// <param name="pixel">The pixel to quantize</param>
        /// <returns>The quantized value</returns>
        protected abstract byte QuantizePixel(Color32 pixel);

        /// <summary>
        /// Retrieve the palette for the quantized image
        /// </summary>
        /// <param name="original">Any old palette, this is overrwritten</param>
        /// <returns>The new color palette</returns>
        protected abstract ColorPalette GetPalette(ColorPalette original);



     

        /// <summary>
        /// Struct that defines a 32 bpp colour
        /// </summary>
        /// <remarks>
        /// This struct is used to read data from a 32 bits per pixel image
        /// in memory, and is ordered in this manner as this is the way that
        /// the data is layed out in memory
        /// </remarks>
        [StructLayout(LayoutKind.Explicit)]
        public struct Color32
        {
            public Color32(Color c){
                this.ARGB = c.ToArgb();
                Blue = c.B;
                Green = c.G;
                Red = c.R;
                Alpha = c.A;
            }
            public Color32(IntPtr pSourcePixel)
            {
              this = (Color32) Marshal.PtrToStructure(pSourcePixel, typeof(Color32));
                          
            }

            /// <summary>
            /// Holds the blue component of the colour
            /// </summary>
            [FieldOffset(0)]
            public byte Blue;
            /// <summary>
            /// Holds the green component of the colour
            /// </summary>
            [FieldOffset(1)]
            public byte Green;
            /// <summary>
            /// Holds the red component of the colour
            /// </summary>
            [FieldOffset(2)]
            public byte Red;
            /// <summary>
            /// Holds the alpha component of the colour
            /// </summary>
            [FieldOffset(3)]
            public byte Alpha;

            /// <summary>
            /// Permits the color32 to be treated as an int32
            /// </summary>
            [FieldOffset(0)]
            public int ARGB;

            /// <summary>
            /// Return the color for this Color32 object
            /// </summary>
            public Color Color
            {
                get { return Color.FromArgb(Alpha, Red, Green, Blue); }
            }
        }
    }
}
