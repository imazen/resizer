using System;
using System.Collections.Generic;
using System.Text;

using System.Drawing;
using System.Drawing.Imaging;

namespace SEAMonster.EnergyFunctions
{
    public static class Common
    {
        // Get the "energy" of a pixel
        // If outside the bounds of the bitmap, return maximum energy value
        public static byte GetPixel(BitmapData bmd, Size size, int x, int y)
        {
            if (x >= 0 && x < size.Width && y >= 0 && y < size.Height)
            {
                unsafe
                {
                    byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride) + (x * 3);
                    return (byte)((0.2126 * row[2]) + (0.7152 * row[1]) + (0.0722 * row[0]));
                }
            }
            else
            {
                // Pixel is out of bounds, so return maximum energy
                return byte.MaxValue;
            }
        }

        public static byte GetColor(BitmapData bmd, Size size, int x, int y, int index)
        {
            if (x >= 0 && x < size.Width && y >= 0 && y < size.Height)
            {
                unsafe
                {
                    byte* row = (byte*)bmd.Scan0 + (y * bmd.Stride) + (x * 3);
                    return row[index];
                }
            }
            else
            {
                // Pixel is out of bounds, so return maximum energy
                return byte.MaxValue;
            }
        }

    }
}
