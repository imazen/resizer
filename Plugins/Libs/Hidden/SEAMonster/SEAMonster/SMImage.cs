using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

using SEAMonster.EnergyFunctions;
using SEAMonster.SeamFunctions;

namespace SEAMonster
{
    // Represents a SEAMonster image
    public class SMImage
    {
        private Bitmap bitmap = null;
        private BitmapData bmd = null;
        private int modifiedWidth = 0;
        private int modifiedHeight = 0;
        private double totalTime = 0;
        private Bitmap energyMapBitmap = null;
        private Bitmap energyBiasBitmap = null;
        private EnergyFunction energyFunction = null;           // Energy function used to calculate energy map
        private SeamFunction seamFunction = null;               // Seam function used to calculate and select seams
        private EnergyBias energyBias = null;                   // Energy bias map

        public event EventHandler ImageChanged;

        public SMImage(Bitmap bitmap, SeamFunction seamFunction, EnergyFunction energyFunction)
        {
            this.energyFunction = energyFunction;

            // Configure energy bias
            this.energyBias = new EnergyBias(new Size(bitmap.Width, bitmap.Height));
            this.energyBiasBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb);
            
            // Configure seam function
            this.seamFunction = seamFunction;
            this.seamFunction.EnergyFunction = this.EnergyFunction;
            this.seamFunction.EnergyBias = this.energyBias;

            this.Bitmap = bitmap;
        }

        public Size Size
        {
            get { return new Size(modifiedWidth, modifiedHeight); }
        }

        public Bitmap Bitmap
        {
            get { return bitmap; }
            set
            {
                bitmap = value;

                modifiedWidth = bitmap.Width;
                modifiedHeight = bitmap.Height;
                totalTime = 0;

                RecomputeEntireImage();
            }
        }

        public Bitmap CarvedBitmap
        {
            get
            {
                //Create a new bitmap object based on the input
                Bitmap newBmp = new Bitmap(modifiedWidth, modifiedHeight, PixelFormat.Format24bppRgb);

                //Create a graphics object and attach it to the bitmap
                Graphics newBmpGraphics = Graphics.FromImage(newBmp);

                //Draw the portion of the input image in the crop rectangle
                //in the graphics object
                newBmpGraphics.DrawImage(bitmap,
                    new Rectangle(0, 0, modifiedWidth, modifiedHeight),
                    new Rectangle(0, 0, modifiedWidth, modifiedHeight),
                    GraphicsUnit.Pixel);

                //Return the bitmap
                newBmpGraphics.Dispose();

                //newBmp will have a RawFormat of MemoryBmp because it was created
                //from scratch instead of being based on inputBmp. 
                //Since it it inconvenient
                //for the returned version of a bitmap to be 
                //of a different format, now convert
                //the scaled bitmap to the format of the source bitmap
                
                // TODO: Not sure conversion is important, but do it for now
                return ConvertBitmap(newBmp, bitmap.RawFormat);
            }
        }

        private Bitmap ConvertBitmap(Bitmap inputBmp, System.Drawing.Imaging.ImageFormat destFormat)
        {
            //Create an in-memory stream which will be used to save
            //the converted image
            System.IO.Stream imgStream = new System.IO.MemoryStream();
            //Save the bitmap out to the memory stream, 
            //using the format indicated by the caller
            inputBmp.Save(imgStream, destFormat);

            //At this point, imgStream contains the binary form of the
            //bitmap in the target format.  All that remains is to load it
            //into a new bitmap object
            Bitmap destBitmap = new Bitmap(imgStream);

            return destBitmap;
        }

        private void RecomputeEntireImage()
        {
            // Compute the energy map for this bitmap
            bmd = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite, bitmap.PixelFormat);

            energyFunction.Recompute(bmd, Size);

            // Unlock the bits.
            bitmap.UnlockBits(bmd);

            // Compute all seams
            seamFunction.ComputeAllSeams(Direction.Vertical, Size);
            seamFunction.ComputeAllSeams(Direction.Horizontal, Size);
        }

        public EnergyFunction EnergyFunction
        {
            get { return energyFunction; }
            set
            { 
                energyFunction = value;

                // Need to recompute both energy and seams
                RecomputeEntireImage();
            }
        }

        public SeamFunction SeamFunction
        {
            get { return seamFunction; }
            set
            {
                seamFunction = value;
                seamFunction.EnergyFunction = this.EnergyFunction;
                seamFunction.EnergyBias = this.energyBias;

                // Compute all seams
                seamFunction.ComputeAllSeams(Direction.Vertical, Size);
                seamFunction.ComputeAllSeams(Direction.Horizontal, Size);
            }
        }

        public Bitmap EnergyMapBitmap
        {
            get
            {
                GenerateEnergyMapBitmap();

                return energyMapBitmap;
            }
        }

        public Bitmap EnergyBiasBitmap
        {
            get
            {
                GenerateEnergyBiasBitmap();

                return energyBiasBitmap;
            }

            set
            {
                energyBiasBitmap = value;

                ParseEnergyBiasBitmap();

                // Need to recompute everything based on new biases
                RecomputeEntireImage();
            }
        }

        // Let subscribers know that the image has changed (ex. for repainting)
        protected virtual void OnImageChanged()
        {
            if (ImageChanged != null)
            {
                ImageChanged(this, EventArgs.Empty);
            }
        }

        private void SetPixel(BitmapData bmd, int x, int y, byte red, byte green, byte blue)
        {
            unsafe
            {
                byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride) + (x * 3);
                row[0] = blue;
                row[1] = green;
                row[2] = red;
            }
        }

        private void SetPixel32(BitmapData bmd, int x, int y, byte alpha, byte red, byte green, byte blue)
        {
            unsafe
            {
                byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride) + (x * 4);
                row[0] = blue;
                row[1] = green;
                row[2] = red;
                row[3] = alpha;
            }
        }

        private void GetPixel(BitmapData bmd, int x, int y, ref byte red, ref byte green, ref byte blue)
        {
            unsafe
            {
                byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride) + (x * 3);
                blue = row[0];
                green = row[1];
                red = row[2];
            }
        }

        private void GetPixel32(BitmapData bmd, int x, int y, ref byte alpha, ref byte red, ref byte green, ref byte blue)
        {
            unsafe
            {
                byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride) + (x * 4);
                blue = row[0];
                green = row[1];
                red = row[2];
                alpha = row[3];
            }
        }

        private void ShiftPixels(BitmapData bmd, Direction direction, int x, int y)
        {
            int dstIndex = 0;
            int srcIndex = 0;
            int maxIndex = 0;

            int offset = 0;
            int fromIndex = 0;
            int toIndex = 0;

            // TODO: Seems like this could be factored
            if (direction == Direction.Vertical)
            {
                unsafe
                {
                    byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride) + (x * 3);

                    dstIndex = 0;
                    srcIndex = 3;
                    maxIndex = (modifiedWidth - x - 1) * 3;

                    for (dstIndex = 0; dstIndex < maxIndex; dstIndex++)
                    {
                        row[dstIndex] = row[srcIndex];
                        srcIndex++;
                    }

                    row[dstIndex] = 0;
                    row[dstIndex + 1] = 0;
                    row[dstIndex + 2] = 0;
                }

                // Set for array shifts
                offset = y;
                fromIndex = x;
                toIndex = modifiedWidth;
            }
            else if (direction == Direction.Horizontal)
            {
                // Vertical shift
                // TODO: Can the two of these be factored into one?

                unsafe
                {
                    dstIndex = y;
                    srcIndex = dstIndex + 1;
                    maxIndex = modifiedHeight - 1;
                    byte* row = null;
                    byte* row2 = null;

                    for (dstIndex = y; dstIndex < maxIndex; dstIndex++)
                    {
                        row = (byte*)bmd.Scan0 + (dstIndex * bmd.Stride) + (x * 3);
                        row2 = (byte*)bmd.Scan0 + (srcIndex * bmd.Stride) + (x * 3);

                        row[0] = row2[0];
                        row[1] = row2[1];
                        row[2] = row2[2];
                        srcIndex++;
                    }

                    row = (byte*)bmd.Scan0 + (dstIndex * bmd.Stride) + (x * 3);
                    row[0] = 0;
                    row[1] = 0;
                    row[2] = 0;
                }

                // Set for array shifts
                offset = x;
                fromIndex = y;
                toIndex = modifiedHeight;
            }

            // Update seam energy values
            seamFunction.UpdateSeamEnergy(direction, x, y);

            // Shift the energy map and the bias map
            Common.ShiftArray(energyFunction.EnergyMap, direction, offset, fromIndex, toIndex, byte.MaxValue);
            Common.ShiftArray(energyBias.EnergyBiasMap, direction, offset, fromIndex, toIndex, 0);
        }
        
        // Carve the index at the specified seam
        private void CarveSeam(Seam seam)
        {
            // Iterate through seam
            foreach (Point p in seam.SeamPixels(Size))
            {
                // Shift pixels
                ShiftPixels(bmd, seam.direction, p.X, p.Y);
            }

            // Since we reduced the length of each row by 1, update the modified width
            if (seam.direction == Direction.Vertical)
            {
                modifiedWidth--;
            }
            else
            {
                modifiedHeight--;
            }
        }

        // Paint the seam at the specified index
        private void PaintSeam(Seam seam)
        {
            // Iterate through seam
            foreach (Point p in seam.SeamPixels(Size))
            {
                // Shift pixels
                SetPixel(bmd, p.X, p.Y, 255, 0, 0);
            }
        }

        public void Carve(Direction direction, ComparisonMethod comparisonMethod, Size minimumSize)
        {
            if (modifiedWidth <= 0 || modifiedHeight <= 0)
            {
                return;
            }

            // Time this carve
            HiPerfTimer timer = new HiPerfTimer();
            timer.Start();

            bmd = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadWrite, bitmap.PixelFormat);

            while (modifiedWidth > 0 && modifiedWidth > minimumSize.Width &&
                   modifiedHeight > 0 && modifiedHeight > minimumSize.Height)
            {
                // Find the lowest energy seam
                Seam lowestEnergySeam;
                if (direction == Direction.Vertical || direction == Direction.Horizontal)
                {
                    lowestEnergySeam = seamFunction.FindLowestEnergy(direction, comparisonMethod, Size);
                }
                else
                {
                    // Find lowest energy across both horizontal and vertical directions
                    Seam isLowest1 = seamFunction.FindLowestEnergy(Direction.Vertical, comparisonMethod, Size);
                    Seam isLowest2 = seamFunction.FindLowestEnergy(Direction.Horizontal, comparisonMethod, Size);

                    // Use compareValue, since it's been run through the seam comparison method
                    if (isLowest1.compareValue < isLowest2.compareValue)
                    {
                        lowestEnergySeam = isLowest1;
                    }
                    else
                    {
                        lowestEnergySeam = isLowest2;
                    }
                }

                //// Paint the seam
                //PaintSeam(lowestEnergySeam);
                //// Unlock the bits.
                //bitmap.UnlockBits(bmd);

                //// Fire event that image has changed
                //OnImageChanged();

                //bmd = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                //    ImageLockMode.ReadWrite, bitmap.PixelFormat);

                // Carve the seam
                CarveSeam(lowestEnergySeam);

                // Recompute energy map altered by seam
                energyFunction.RecomputeSeam(bmd, Size, lowestEnergySeam);

                // Re-compute seams
                seamFunction.RecomputeSeams(lowestEnergySeam, Size);
            }

            // Unlock the bits.
            bitmap.UnlockBits(bmd);

            // Stop the timer
            timer.Stop();
            double time = timer.Duration;

            // Update average run time
            totalTime += time;

            // Fire event that image has changed
            OnImageChanged();
        }

        private void GenerateEnergyMapBitmap()
        {
            energyMapBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb);

            BitmapData energyMapBmd = energyMapBitmap.LockBits(new Rectangle(0, 0, energyMapBitmap.Width, energyMapBitmap.Height),
                ImageLockMode.ReadWrite, energyMapBitmap.PixelFormat);

            unsafe
            {
                for (int y = 0; y < energyMapBmd.Height; y++)
                {
                    for (int x = 0; x < energyMapBmd.Width; x++)
                    {
                        byte grayValue = energyFunction.EnergyMap[x, y];
                        SetPixel(energyMapBmd, x, y, grayValue, grayValue, grayValue);
                    }
                }
            }

            // Unlock the bits.
            energyMapBitmap.UnlockBits(energyMapBmd);
        }

        private void GenerateEnergyBiasBitmap()
        {
            energyBiasBitmap = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);

            BitmapData energyBiasBmd = energyBiasBitmap.LockBits(new Rectangle(0, 0, energyBiasBitmap.Width, energyBiasBitmap.Height),
                ImageLockMode.ReadWrite, energyBiasBitmap.PixelFormat);

            unsafe
            {
                for (int y = 0; y < energyBiasBmd.Height; y++)
                {
                    for (int x = 0; x < energyBiasBmd.Width; x++)
                    {
                        if (energyBias.EnergyBiasMap[x, y] > 0)
                        {
                            SetPixel32(energyBiasBmd, x, y, 127, 0, 255, 0);
                        }
                        else if (energyBias.EnergyBiasMap[x, y] < 0)
                        {
                            SetPixel32(energyBiasBmd, x, y, 127, 255, 0, 0);
                        }
                    }
                }
            }

            // Unlock the bits.
            energyBiasBitmap.UnlockBits(energyBiasBmd);
        }

        private void ParseEnergyBiasBitmap()
        {
            BitmapData energyBiasBmd = energyBiasBitmap.LockBits(new Rectangle(0, 0, energyBiasBitmap.Width, energyBiasBitmap.Height),
                ImageLockMode.ReadOnly, energyBiasBitmap.PixelFormat);

            byte alpha = 0, red = 0, green = 0, blue = 0;

            unsafe
            {
                for (int y = 0; y < energyBiasBmd.Height; y++)
                {
                    for (int x = 0; x < energyBiasBmd.Width; x++)
                    {
                        GetPixel32(energyBiasBmd, x, y, ref alpha, ref red, ref green, ref blue);

                        if (red == 255 && green == 0 && blue == 0)
                        {
                            energyBias.EnergyBiasMap[x, y] = -50000;
                        }
                        else if (red == 0 && green == 255 && blue == 0)
                        {
                            energyBias.EnergyBiasMap[x, y] = 50000;
                        }
                        else
                        {
                            energyBias.EnergyBiasMap[x, y] = 0;
                        }
                    }
                }
            }

            // Unlock the bits.
            energyBiasBitmap.UnlockBits(energyBiasBmd);
        }
    }
}
