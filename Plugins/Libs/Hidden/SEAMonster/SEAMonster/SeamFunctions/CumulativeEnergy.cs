using System;
using System.Collections.Generic;
using System.Text;

using System.Drawing;
using System.Drawing.Imaging;

using SEAMonster.EnergyFunctions;

namespace SEAMonster.SeamFunctions
{
    public class CumulativeEnergy: SeamFunction
    {
        private int[,] verticalCumulativeEnergyMap;
        private int[,] horizontalCumulativeEnergyMap;

        private bool verticalSeamsDirty = true;
        private bool horizontalSeamsDirty = true;

        public override string ToString()
        {
            return "Cumulative";
        }
        
        public override void ComputeAllSeams(Direction direction, Size size)
        {
            int endOffset = 0;

            // Initialize arrays
            if (direction == Direction.Vertical)
            {
                verticalCumulativeEnergyMap = new int[size.Width, size.Height];
                endOffset = size.Width - 1;
            }
            else if (direction == Direction.Horizontal)
            {
                horizontalCumulativeEnergyMap = new int[size.Width, size.Height];
                endOffset = size.Height - 1;
            }

            // Recompute the whole swath of pixels
            ComputeSeamRange(direction, 0, endOffset, size);
        }

        // Compute only a range of seams (cascades down or right)
        private void ComputeSeamRange(Direction direction, int startOffset, int endOffset, Size size)
        {
            int[,] cumulativeEnergyMap = null;
            int x = 0, y = 0;
            int xInc = 0, yInc = 0;                            // x and y increments
            int pixelCount = 0;                                // Maximum number of pixels

            // Set initial values
            if (direction == Direction.Vertical)
            {
                cumulativeEnergyMap = verticalCumulativeEnergyMap;
                x = startOffset;
                yInc = 1;
                pixelCount = size.Height;
            }
            else if (direction == Direction.Horizontal)
            {
                cumulativeEnergyMap = horizontalCumulativeEnergyMap;
                y = startOffset;
                xInc = 1;
                pixelCount = size.Width;
            }

            // Copy first row/column of values directly
            // TODO: Although this starts at the specified offset, it'd also be nice to *end* it at the endOffset
            if (pixelCount > startOffset)
            {
                while (x < size.Width && y < size.Height)
                {
                    cumulativeEnergyMap[x, y] = energyFunction.GetEnergy(x, y, size) + energyBias.GetBias(x, y, size);

                    // Increment
                    x += yInc;
                    y += xInc;
                }
            }

            // Increment
            // TODO: Keep to the DRY principle, and remove this!
            x += xInc;
            y += yInc;
            if (direction == Direction.Vertical && x > endOffset)
            {
                startOffset--;
                if (startOffset < 0)
                {
                    startOffset = 0;
                }
                endOffset++;
                if (endOffset > size.Width - 1)
                {
                    endOffset = size.Width - 1;
                }

                x = startOffset;
            }
            else if (direction == Direction.Horizontal && y >= endOffset)
            {
                startOffset--;
                if (startOffset < 0)
                {
                    startOffset = 0;
                }
                endOffset++;
                if (endOffset > size.Height - 1)
                {
                    endOffset = size.Height - 1;
                }

                y = startOffset;
            }
            while (x < size.Width && y < size.Height)
            {
                while (x < size.Width && y < size.Height)
                {
                    int e0 = 0, e1 = 0, e2 = 0, lowestEnergy = 0;

                    if (direction == Direction.Vertical)
                    {
                        e0 = GetCEnergy(direction, x - 1, y - 1, size);
                        e1 = GetCEnergy(direction, x, y - 1, size);
                        e2 = GetCEnergy(direction, x + 1, y - 1, size);
                    }
                    else if (direction == Direction.Horizontal)
                    {
                        e0 = GetCEnergy(direction, x - 1, y - 1, size);
                        e1 = GetCEnergy(direction, x - 1, y, size);
                        e2 = GetCEnergy(direction, x - 1, y + 1, size);
                    }

                    if (e0 < e1)
                    {
                        if (e0 < e2)
                        {
                            lowestEnergy = e0;
                        }
                        else
                        {
                            lowestEnergy = e2;
                        }
                    }
                    else
                    {
                        if (e1 < e2)
                        {
                            lowestEnergy = e1;
                        }
                        else
                        {
                            lowestEnergy = e2;
                        }
                    }

                    cumulativeEnergyMap[x, y] =
                        energyFunction.GetEnergy(x, y, size) + lowestEnergy + energyBias.GetBias(x, y, size);

                    // Increment
                    x += yInc;
                    y += xInc;
                }

                // Increment
                x += xInc;
                y += yInc;

                if (direction == Direction.Vertical && x >= endOffset)
                {
                    startOffset--;
                    if (startOffset < 0)
                    {
                        startOffset = 0;
                    }
                    endOffset++;
                    if (endOffset > size.Width - 1)
                    {
                        endOffset = size.Width - 1;
                    }

                    x = startOffset;
                }
                else if (direction == Direction.Horizontal && y >= endOffset)
                {
                    startOffset--;
                    if (startOffset < 0)
                    {
                        startOffset = 0;
                    }
                    endOffset++;
                    if (endOffset > size.Height - 1)
                    {
                        endOffset = size.Height - 1;
                    }

                    y = startOffset;
                }
            }

            // Clear dirty flag, since we recomputed
            if (direction == Direction.Vertical)
            {
                verticalSeamsDirty = false;
            }
            else if (direction == Direction.Horizontal)
            {
                horizontalSeamsDirty = false;
            }
        }


        private int GetCEnergy(Direction direction, int x, int y, Size size)
        {
            if (x >= 0 && x < size.Width && y >= 0 && y < size.Height)
            {
                if (direction == Direction.Vertical)
                {
                    return verticalCumulativeEnergyMap[x, y];
                }
                else
                {
                    return horizontalCumulativeEnergyMap[x, y];
                }
            }
            else
            {
                // Pixel is out of bounds, so return maximum energy
                // TODO: Return maximum here? Or should it be 0?
                return int.MaxValue;
            }
        }

        public override void RecomputeSeams(Seam seam, Size size)
        {
            if (seam.direction == Direction.Vertical)
            {
                // Iterate through seam and shift pixels
                foreach (Point p in seam.SeamPixels(size))
                {
                    // Shift array values
                    Common.ShiftArray(verticalCumulativeEnergyMap, seam.direction, p.Y, p.X, size.Width, int.MaxValue);
                }

                ComputeSeamRange(Direction.Vertical, seam.pixel, seam.pixel, size);
                horizontalSeamsDirty = true;
            }
            else if (seam.direction == Direction.Horizontal)
            {
                // Iterate through seam and shift pixels
                foreach (Point p in seam.SeamPixels(size))
                {
                    // Shift array values
                    Common.ShiftArray(horizontalCumulativeEnergyMap, seam.direction, p.X, p.Y, size.Height, int.MaxValue);
                }

                ComputeSeamRange(Direction.Horizontal, seam.pixel, seam.pixel, size);
                verticalSeamsDirty = true;
            }
        }

        public override void UpdateSeamEnergy(Direction direction, int x, int y)
        {
            // Not necessary using this approach
            return;
        }

        public override Seam FindLowestEnergy(Direction direction, ComparisonMethod comparisonMethod, Size size)
        {
            Seam seam = null;
            int lowestEnergyX = 0, lowestEnergyY = 0;
            double lowestEnergy = double.MaxValue;
            double energyCompare = 0;
            int x = 0, y = 0;
            int xInc = 0, yInc = 0;                            // x and y increments

            // Set initial values
            if (direction == Direction.Vertical)
            {
                // If the direction we're evaluating is "dirty," force a recompute
                if (verticalSeamsDirty)
                {
                    ComputeAllSeams(Direction.Vertical, size);
                    horizontalSeamsDirty = true;
                }

                x = 0;
                y = size.Height - 1;
                yInc = 1;
            }
            else if (direction == Direction.Horizontal)
            {
                // If the direction we're evaluating is "dirty," force a recompute
                if (horizontalSeamsDirty)
                {
                    ComputeAllSeams(Direction.Horizontal, size);
                    verticalSeamsDirty = true;
                }

                x = size.Width - 1;
                y = 0;
                xInc = 1;
            }

            // Find the end of the lowest energy seam
            while (x < size.Width && y < size.Height)
            {
                energyCompare = GetSeamEnergy(direction, comparisonMethod, x, y, size);

                // Is this the lowest energy seam so far?
                if (energyCompare < lowestEnergy)
                {
                    lowestEnergy = energyCompare;
                    lowestEnergyX = x;
                    lowestEnergyY = y;
                }

                // Increment
                x += yInc;
                y += xInc;
            }

            // Reset position to lowest energy
            x = lowestEnergyX;
            y = lowestEnergyY;

            // Build the seam
            seam = BuildSeam(direction, x, y, size);

            seam.totalDiff = (int)lowestEnergy;              // TODO: Is this the correct value to set?
            seam.compareValue = lowestEnergy;

            return seam;
        }

        // Gets the energy of a seam depending on comparisonMethod
        private double GetSeamEnergy(Direction direction, ComparisonMethod comparisonMethod, int x, int y, Size size)
        {
            double energyCompare = 0;
            int[,] cumulativeEnergyMap = null;

            // Set initial values
            if (direction == Direction.Vertical)
            {
                cumulativeEnergyMap = verticalCumulativeEnergyMap;
            }
            else if (direction == Direction.Horizontal)
            {
                cumulativeEnergyMap = horizontalCumulativeEnergyMap;
            }

            if (comparisonMethod == ComparisonMethod.Total)
            {
                // Compare simple totals for each seam
                energyCompare = cumulativeEnergyMap[x, y];
            }
            else if (comparisonMethod == ComparisonMethod.Average)
            {
                // Compare the average energy value for each seam (depends on seam direction)
                if (direction == Direction.Vertical)
                {
                    energyCompare = cumulativeEnergyMap[x, y] / size.Height;
                }
                else if (direction == Direction.Horizontal)
                {
                    energyCompare = cumulativeEnergyMap[x, y] / size.Width;
                }
            }
            else if (comparisonMethod == ComparisonMethod.DiffBias)
            {
                // TODO: Not supported using this method (because we don't track total energy here)
                //       So, default to simple total.
                energyCompare = cumulativeEnergyMap[x, y];
            }

            return energyCompare;
        }

        private Seam BuildSeam(Direction direction, int x, int y, Size size)
        {
            int pixelCount = 0;                                // Maximum number of pixels
            int pixelIndex = 0;
            int xInc = 0, yInc = 0;                            // x and y increments

            // Set initial values
            if (direction == Direction.Vertical)
            {
                yInc = 1;
                pixelCount = size.Height;
            }
            else if (direction == Direction.Horizontal)
            {
                xInc = 1;
                pixelCount = size.Width;
            }

            // Assemble a seam in reverse order
            Seam seam = new Seam();
            seam.seamPixels = new SeamPixel[pixelCount];
            pixelIndex = pixelCount - 1;
            while (pixelIndex > 0)
            {
                int e0 = 0, e1 = 0, e2 = 0;
                seam.seamPixels[pixelIndex] = new SeamPixel();
                SeamPixel seamPixel = seam.seamPixels[pixelIndex];

                if (direction == Direction.Vertical)
                {
                    e0 = GetCEnergy(direction, x - 1, y - 1, size);
                    e1 = GetCEnergy(direction, x, y - 1, size);
                    e2 = GetCEnergy(direction, x + 1, y - 1, size);
                }
                else if (direction == Direction.Horizontal)
                {
                    e0 = GetCEnergy(direction, x - 1, y - 1, size);
                    e1 = GetCEnergy(direction, x - 1, y, size);
                    e2 = GetCEnergy(direction, x - 1, y + 1, size);
                }

                if (e0 < e1)
                {
                    if (e0 < e2)
                    {
                        if (direction == Direction.Vertical)
                        {
                            x--;
                            if (x < 0)
                            {
                                x = 0;
                            }
                            else
                            {
                                seamPixel.left = true;
                            }
                        }
                        else if (direction == Direction.Horizontal)
                        {
                            y--;
                            if (y < 0)
                            {
                                y = 0;
                            }
                            else
                            {
                                seamPixel.right = true;
                            }
                        }
                    }
                    else
                    {
                        if (direction == Direction.Vertical)
                        {
                            x++;
                            if (x > size.Width - 1)
                            {
                                x = size.Width - 1;
                            }
                            else
                            {
                                seamPixel.right = true;
                            }
                        }
                        else if (direction == Direction.Horizontal)
                        {
                            y++;
                            if (y > size.Height - 1)
                            {
                                y = size.Height - 1;
                            }
                            else
                            {
                                seamPixel.left = true;
                            }
                        }
                    }
                }
                else
                {
                    if (e1 < e2)
                    {
                        // Nothing to do
                    }
                    else
                    {
                        if (direction == Direction.Vertical)
                        {
                            x++;
                            if (x > size.Width - 1)
                            {
                                x = size.Width - 1;
                            }
                            else
                            {
                                seamPixel.right = true;
                            }
                        }
                        else if (direction == Direction.Horizontal)
                        {
                            y++;
                            if (y > size.Height - 1)
                            {
                                y = size.Height - 1;
                            }
                            else
                            {
                                seamPixel.left = true;
                            }
                        }
                    }
                }

                // Decrement
                x -= xInc;
                y -= yInc;

                // Reduce our pixel index
                pixelIndex--;
            }

            // Add "top pixel" (should be index 0)
            seam.seamPixels[pixelIndex] = new SeamPixel();

            // Set other seam values
            seam.direction = direction;
            if (direction == Direction.Vertical)
            {
                seam.pixel = x;
            }
            else if (direction == Direction.Horizontal)
            {
                seam.pixel = y;
            }

            return seam;
        }
    }
}
