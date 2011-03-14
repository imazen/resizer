/* 
  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF 
  ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO 
  THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A 
  PARTICULAR PURPOSE. 
  
    This is sample code and is freely distributable. 
 * Heavily modified by Nathanael Jones - I've left it under the same license
*/

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace fbs.ImageResizer.Plugins.PrettyGifs
{
    /// <summary>
    /// Summary description for Class1.
    /// </summary>
    public abstract class Quantizer
    {

        /// <summary>
        /// Construct the quantizer
        /// </summary>
        /// <param name="singlePass">If true, the quantization only needs to loop through the source pixels once</param>
        /// <remarks>
        /// If you construct this class with a true value for singlePass, then the code will, when quantizing your image,
        /// only call the 'QuantizeImage' function. If two passes are required, the code will call 'InitialQuantizeImage'
        /// and then 'QuantizeImage'.
        /// </remarks>
        public Quantizer(bool singlePass)
        {
            _singlePass = singlePass;
            _pixelSize = Marshal.SizeOf(typeof (Color32));
        }

        public virtual void Reset()
        {
            this.secondPassIntermediate = null;
            this.secondPassX = 0;
            this.secondPassY = 0;
          
        }
        /// <summary>
        /// If true, image is re-paletted after quantization, and dithering occurs on a separate frame from the source.
        /// </summary>
        public bool fourPass = false;
        /// <summary>
        /// Quantize an image and return the resulting output bitmap
        /// </summary>
        /// <param name="source">The image to quantize</param>
        /// <returns>A quantized version of the image</returns>
        public Bitmap Quantize(Image source)
        {
            // Get the size of the source image
            int height = source.Height;
            int width = source.Width;

            // And construct a rectangle from these dimensions
            Rectangle bounds = new Rectangle(0, 0, width, height);

            // First off take a 32bpp copy of the image
            Bitmap copy = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            // And construct an 8bpp version
            Bitmap output = new Bitmap(width, height, PixelFormat.Format8bppIndexed);

            // Now lock the bitmap into memory
            using (Graphics g = Graphics.FromImage(copy))
            {
                g.PageUnit = GraphicsUnit.Pixel;

                // Draw the source image onto the copy bitmap,
                // which will effect a widening as appropriate.
                g.DrawImage(source, bounds);

            }

            Bitmap copy2 = null;
            if (fourPass)
            {
                copy2 = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(copy2))
                {
                    g.PageUnit = GraphicsUnit.Pixel;
                    g.DrawImage(source, bounds);
                }
            }



            // Define a pointer to the bitmap data
            BitmapData sourceData = null;
            BitmapData intermediateData = null;
            try
            {
                // Get the source image bits and lock into memory
                sourceData = copy.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);



                // Call the FirstPass function if not a single pass algorithm.
                // For something like an octree quantizer, this will run through
                // all image pixels, build a data structure, and create a palette.
                if (!_singlePass)
                    FirstPass(sourceData, width, height);

                // Then set the color palette on the output bitmap. I'm passing in the current palette 
                // as there's no way to construct a new, empty palette.
                output.Palette = GetPalette(output.Palette);

                if (!fourPass)
                {
                    // Then call the second pass which actually does the conversion
                    SecondPass(sourceData, null, output, width, height, bounds);
                }
                else
                {
                    intermediateData = copy2.LockBits(bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                    SecondPass(sourceData, intermediateData, output, width, height, bounds);

                    //NDJ- trying quad pass for better results (adjusts for dithering)
                    FirstPass(intermediateData, width, height);
                    output.Palette = GetPalette(output.Palette);
                    SecondPass(intermediateData, null,output, width, height, bounds);

                }
                /*
                Reset();
                
                */
            }
            finally
            {
                // Ensure that the bits are unlocked
                copy.UnlockBits(sourceData);
                if (intermediateData != null) copy2.UnlockBits(intermediateData);
            }

            // Last but not least, return the output bitmap
            return output;
        }

        /// <summary>
        /// Execute the first pass through the pixels in the image
        /// </summary>
        /// <param name="sourceData">The source data</param>
        /// <param name="width">The width in pixels of the image</param>
        /// <param name="height">The height in pixels of the image</param>
        protected  virtual void FirstPass(BitmapData sourceData, int width, int height)
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
                    pSourcePixel = (IntPtr)((Int32)pSourcePixel + _pixelSize);
                }	// Now I have the pixel, call the FirstPassQuantize function...

                // Add the stride to the source row
                pSourceRow = (IntPtr)((long)pSourceRow + sourceData.Stride);
            }
        }

        //For dithering - 5-18-09 ndj
        private BitmapData secondPassIntermediate;
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
        protected virtual void SecondPass(BitmapData sourceData, BitmapData intermediate, Bitmap output, int width, int height, Rectangle bounds)
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

                        pSourcePixel = (IntPtr)((long)pSourcePixel + _pixelSize);
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
       
        //Can only be called from QuantizePixel... Expects sourceData to be locked.
        //This is how dithering is done... 
        //5-18-09 ndj
        protected void AdjustNeighborSource(int offsetX, int offsetY, int deltaR, int deltaG, int deltaB, int deltaA)
        {
            if (secondPassIntermediate == null) return;
            int x = secondPassX + offsetX;
            int y = secondPassY + offsetY;
            if (x < 0 || x >= secondPassIntermediate.Width) return; //do nothing;
            if (y < 0 || y >= secondPassIntermediate.Height) return; //do nothing
            IntPtr p = (IntPtr)((long)secondPassIntermediate.Scan0 + ((long)y * (long)secondPassIntermediate.Stride) + (_pixelSize * x));
            //Read the original color
            Color32 c = new Color32(p);

            c.Red = ToByte((int)c.Red + deltaR);
            c.Green = ToByte((int)c.Green + deltaG);
            c.Blue = ToByte((int)c.Blue + deltaB);
            c.Alpha = ToByte((int)c.Alpha + deltaA);
            
            Marshal.StructureToPtr(c, p, true); //False to not dispose old block. Since no reference to it exists (I believe PtrToStructure from Color32 copies, not references), this should be safe
  
        }
        //Truncates an int to a byte. 5-18-09 ndj
        protected byte ToByte(int i)
        {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return (byte)i;
        }
        

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
        /// Flag used to indicate whether a single pass or two passes are needed for quantization.
        /// </summary>
        private bool _singlePass;
        private int _pixelSize;

     

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
