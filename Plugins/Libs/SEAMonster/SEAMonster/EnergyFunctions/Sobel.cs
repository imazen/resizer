using System;
using System.Collections.Generic;
using System.Text;

using System.Drawing;
using System.Drawing.Imaging;

namespace SEAMonster.EnergyFunctions
{
    public class Sobel: EnergyFunction
    {
        public override string ToString()
        {
            return "Sobel";
        }

        public override void Recompute(BitmapData bmd, Size size)
        {
            if (bmd != null)
            {
                energyMap = new byte[bmd.Width, bmd.Height];

                for (int x = 0; x < bmd.Width; x++)
                {
                    for (int y = 0; y < bmd.Height; y++)
                    {
                        energyMap[x, y] = GetSobelValue(bmd, size, x, y);
                    }
                }
            }
        }

        // Get a Sobel value for a given pixel
        private byte GetSobelValue(BitmapData bmd, Size size, int x, int y)
        {
            if (x >= 0 && x < size.Width && y >= 0 && y < size.Height)
            {
                byte p1, p2, p3, p4, p6, p7, p8, p9;
                int sobelX, sobelY, sobel;

                p1 = (byte)GetPixelSobel(bmd, size, x - 1, y - 1);
                p2 = (byte)GetPixelSobel(bmd, size, x, y - 1);
                p3 = (byte)GetPixelSobel(bmd, size, x + 1, y - 1);
                p4 = (byte)GetPixelSobel(bmd, size, x - 1, y);
                p6 = (byte)GetPixelSobel(bmd, size, x + 1, y);
                p7 = (byte)GetPixelSobel(bmd, size, x - 1, y + 1);
                p8 = (byte)GetPixelSobel(bmd, size, x, y + 1);
                p9 = (byte)GetPixelSobel(bmd, size, x + 1, y + 1);

                sobelX = (p1 + (p2 + p2) + p3 - p7 - (p8 + p8) - p9);
                sobelY = (p3 + (p6 + p6) + p9 - p1 - (p4 + p4) - p7);

                sobel = (int)Math.Sqrt((sobelX * sobelX) + (sobelY * sobelY));

                if (sobel > 255) sobel = 255;

                return (byte)sobel;
            }
            else
            {
                // Out of bounds
                return byte.MaxValue;
            }
        }

        // Get a pixel value, but with Sobel considerations (consideration for out-of-bounds pixel values)
        private double GetPixelSobel(BitmapData bmd, Size size, int x, int y)
        {
            int x1 = x;
            int y1 = y;

            if (x1 < 0) x1 = 0;
            if (x1 > size.Width - 1) x1 = size.Width - 1;
            if (y1 < 0) y1 = 0;
            if (y1 > size.Height - 1) y1 = size.Height - 1;

            return Common.GetPixel(bmd, size, x1, y1);
        }

        // Recompute the Sobel energy map based on the seam that changed (to be efficient)
        public override void RecomputeSeam(BitmapData bmd, Size size, Seam seam)
        {
            if (bmd != null)
            {
                // Iterate through seam
                foreach (Point p in seam.SeamPixels(size))
                {
                    // Make sure to include the pixels to the "left" too
                    if (seam.direction == Direction.Vertical)
                    {
                        if (p.X > 0)
                        {
                            energyMap[p.X - 1, p.Y] = GetSobelValue(bmd, size, p.X - 1, p.Y);
                        }
                        if (p.X < size.Width)
                        {
                            energyMap[p.X, p.Y] = GetSobelValue(bmd, size, p.X, p.Y);
                        }
                    }
                    else
                    {
                        if (p.Y > 0)
                        {
                            energyMap[p.X, p.Y - 1] = GetSobelValue(bmd, size, p.X, p.Y - 1);
                        }
                        if (p.Y < size.Height)
                        {
                            energyMap[p.X, p.Y] = GetSobelValue(bmd, size, p.X, p.Y);
                        }
                    }
                }
            }
        }
    }
}
