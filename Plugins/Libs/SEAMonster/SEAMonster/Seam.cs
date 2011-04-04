using System;
using System.Collections;
using System.Text;

using System.Drawing;

namespace SEAMonster
{
    public class Seam
    {
        public Direction direction;         // Direction of the seam
        public int pixel = 0;               // Which pixel (x or y) does the seam start at?
        public int totalDiff = 0;           // Total energy of all pixels in seam
        public int totalEnergy = 0;           // Total of energy *differences* across seam
        public int fromPixel = 0;           // Leftmost/topmost pixel offset position that fully encompasses this seam
        public int toPixel = 0;             // Rightmost/bottommost pixel offset position that fully encompasses this seam
        public double compareValue = 0;     // Value to compare seams against if determining lowest energy (accomodates functions)
        public SeamPixel[] seamPixels;      // Data for each pixel in the seam

        public IEnumerable SeamPixels(Size size)
        {
            int x = 0, y = 0;
            int xInc = 0, yInc = 0;                            // x and y increments
            int pixelCount;                                    // Maximum number of pixels
            int pixelIndex = 0;

            // Set initial values
            if (direction == Direction.Vertical)
            {
                x = pixel;
                yInc = 1;
                pixelCount = size.Height;
            }
            else
            {
                y = pixel;
                xInc = 1;
                pixelCount = size.Width;
            }

            // Iterate through seam
            while (x < size.Width && y < size.Height)
            {
                SeamPixel seamPixel = seamPixels[pixelIndex];
                pixelIndex++;

                if (seamPixel.left)
                {
                    if (direction == Direction.Vertical)
                    {
                        x++;
                    }
                    else
                    {
                        y--;
                    }
                }
                else if (seamPixel.right)
                {
                    if (direction == Direction.Vertical)
                    {
                        x--;
                    }
                    else
                    {
                        y++;
                    }
                }

                yield return new Point(x, y);

                // Next pixel
                x += xInc;
                y += yInc;
            }
        }
    }
}
