using System;
using System.Collections.Generic;
using System.Text;

using System.Drawing;
using System.Drawing.Imaging;

using SEAMonster.EnergyFunctions;

namespace SEAMonster.SeamFunctions
{
    public class Standard: SeamFunction
    {
        public override string ToString()
        {
            return "Standard";
        }

        // Compute all seams
        public override void ComputeAllSeams(Direction direction, Size size)
        {
            if (direction == Direction.Vertical)
            {
                verticalSeams = new Seam[size.Width];

                // Iterate through pixels within specified range
                for (int index = 0; index < size.Width; index++)
                {
                    verticalSeams[index] = ComputeSeam(Direction.Vertical, index, EnergyFunction, EnergyBias, size);
                }
            }
            else if (direction == Direction.Horizontal)
            {
                horizontalSeams = new Seam[size.Height];

                // Iterate through pixels within specified range
                for (int index = 0; index < size.Height; index++)
                {
                    horizontalSeams[index] = ComputeSeam(Direction.Horizontal, index, EnergyFunction, EnergyBias, size);
                }
            }
        }

        // Re-compute seams that are affected by the "width" of the carved seam
        public override void RecomputeSeams(Seam seam, Size size)
        {
            // Compute the affected region
            int fromPixel = seam.pixel - seam.fromPixel;
            int toPixel = seam.pixel + seam.toPixel;

            // Shift all seams beyond the affected region to the left
            int dstIndex = seam.pixel;
            int srcIndex = dstIndex + 1;
            int maxIndex = 0;

            if (seam.direction == Direction.Vertical)
            {
                maxIndex = size.Width;
            }
            else if (seam.direction == Direction.Horizontal)
            {
                maxIndex = size.Height;
            }

            for (dstIndex = seam.pixel; dstIndex < maxIndex; dstIndex++)
            {
                // Depends on direction
                if (seam.direction == Direction.Vertical)
                {
                    verticalSeams[dstIndex] = verticalSeams[srcIndex];
                    verticalSeams[dstIndex].pixel--;                    // Shift the reference pixel
                }
                else
                {
                    horizontalSeams[dstIndex] = horizontalSeams[srcIndex];
                    horizontalSeams[dstIndex].pixel--;                    // Shift the reference pixel
                }
                srcIndex++;
            }

            // TODO: Somewhere in this method, we need to subtract energy from the total calculations
            //       (since we just removed a seam of pixels)

            // Set last seam to null
            if (seam.direction == Direction.Vertical)
            {
                verticalSeams[dstIndex] = null;
            }
            else if (seam.direction == Direction.Horizontal)
            {
                horizontalSeams[dstIndex] = null;
            }

            // Iterate through all seams
            int recomputedSeamCount = 0;
            int fromPixelCheck = 0;
            int toPixelCheck = 0;
            int maxCount = 0;

            if (seam.direction == Direction.Vertical)
            {
                maxCount = size.Width;
            }
            else
            {
                maxCount = size.Height;
            }

            for (int index = 0; index < maxCount; index++)
            {
                // Get the right kind of seam
                Seam checkSeam = null;
                if (seam.direction == Direction.Vertical)
                {
                    checkSeam = verticalSeams[index];
                }
                else if (seam.direction == Direction.Horizontal)
                {
                    checkSeam = horizontalSeams[index];
                }

                // Compute boundaries of this seam
                fromPixelCheck = index - checkSeam.fromPixel;
                toPixelCheck = index + checkSeam.toPixel;

                // Does it interfere with the affected region?
                if (!(toPixelCheck < fromPixel || fromPixelCheck > toPixel))
                {
                    if (seam.direction == Direction.Vertical)
                    {
                        verticalSeams[index] = ComputeSeam(Direction.Vertical, index, EnergyFunction, EnergyBias, size);
                    }
                    else if (seam.direction == Direction.Horizontal)
                    {
                        horizontalSeams[index] = ComputeSeam(Direction.Horizontal, index, EnergyFunction, EnergyBias, size);
                    }
                    recomputedSeamCount++;
                }
            }
        }

        // Compute an individual seam
        private Seam ComputeSeam(Direction direction, int index, EnergyFunction energyFunction, EnergyBias energyBias, Size size)
        {
            int totalDiff = 0;
            int totalEnergy = 0;
            int lowestEnergy = 0, e0 = 0, e1 = 0, e2 = 0, b0 = 0, b1 = 0, b2 = 0, d0 = 0, d1 = 0, d2 = 0, lowestDiff = 0;
            Seam seam = new Seam();
            SeamPixel seamPixel;
            int lowestPixel = 0, highestPixel = 0;
            int x = 0, y = 0;
            int xInc = 0, yInc = 0;                            // x and y increments
            int pixelCount = 0;                                    // Maximum number of pixels
            int pixelIndex = 0;

            // Set seam values
            seam.direction = direction;
            seam.pixel = index;

            // Set initial values
            if (direction == Direction.Vertical)
            {
                x = index;
                yInc = 1;
                pixelCount = size.Height;
                lowestPixel = x;
                highestPixel = x;
            }
            else if (direction == Direction.Horizontal)
            {
                y = index;
                xInc = 1;
                pixelCount = size.Width;
                lowestPixel = y;
                highestPixel = y;
            }

            // Begin chain of seam pixels
            seam.seamPixels = new SeamPixel[pixelCount];

            // Get energy at first pixel in seam
            lowestEnergy = energyFunction.GetEnergy(x, y, size);

            // Store first pixel
            seamPixel = new SeamPixel();
            seamPixel.pixelDiff = 0;
            seam.seamPixels[pixelIndex] = seamPixel;
            pixelIndex++;

            // Set totals
            totalEnergy = lowestEnergy;
            totalDiff = 0;

            // Iterate through seam finding lowest energy delta on the way
            x += xInc;
            y += yInc;
            while (x < size.Width && y < size.Height)
            {
                // TODO: Not sure that biases are being used correctly...should check this
                if (direction == Direction.Vertical)
                {
                    b0 = energyBias.GetBias(x - 1, y, size);
                    b1 = energyBias.GetBias(x, y, size);
                    b2 = energyBias.GetBias(x + 1, y, size);

                    e0 = energyFunction.GetEnergy(x - 1, y, size) + b0;
                    e1 = energyFunction.GetEnergy(x, y, size) + b1;
                    e2 = energyFunction.GetEnergy(x + 1, y, size) + b2;
                }
                else if (direction == Direction.Horizontal)
                {
                    b0 = energyBias.GetBias(x, y - 1, size);
                    b1 = energyBias.GetBias(x, y, size);
                    b2 = energyBias.GetBias(x, y + 1, size);

                    e0 = energyFunction.GetEnergy(x, y - 1, size) + b0;
                    e1 = energyFunction.GetEnergy(x, y, size) + b1;
                    e2 = energyFunction.GetEnergy(x, y + 1, size) + b2;
                }

                // Would flipping a sign bit be faster?
                d0 = Math.Abs(lowestEnergy - e0) + b0;
                d1 = Math.Abs(lowestEnergy - e1) + b1;
                d2 = Math.Abs(lowestEnergy - e2) + b2;

                // Create a new seam pixel
                seamPixel = new SeamPixel();

                if (d0 < d1)
                {
                    if (d0 < d2)
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
                                seamPixel.right = true;
                            }
                        }
                        else
                        {
                            y--;
                            if (y < 0)
                            {
                                y = 0;
                            }
                            else
                            {
                                seamPixel.left = true;
                            }
                        }
                        lowestEnergy = e0;
                        lowestDiff = d0;
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
                                seamPixel.left = true;
                            }
                        }
                        else
                        {
                            y++;
                            if (y > size.Height - 1)
                            {
                                y = size.Height - 1;
                            }
                            else
                            {
                                seamPixel.right = true;
                            }
                        }
                        lowestEnergy = e2;
                        lowestDiff = d2;
                    }
                }
                else
                {
                    if (d1 <= d2)
                    {
                        lowestEnergy = e1;
                        lowestDiff = d1;
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
                                seamPixel.left = true;
                            }
                        }
                        else
                        {
                            y++;
                            if (y > size.Height - 1)
                            {
                                y = size.Height - 1;
                            }
                            else
                            {
                                seamPixel.right = true;
                            }
                        }
                        lowestEnergy = e2;
                        lowestDiff = d2;
                    }
                }

                // Track energy difference for this pixel
                seamPixel.pixelDiff = lowestDiff;

                // Add seam pixel to the collection
                seam.seamPixels[pixelIndex] = seamPixel;
                pixelIndex++;

                // Keep track of totals
                totalEnergy += lowestEnergy;
                totalDiff += lowestDiff;

                if (direction == Direction.Vertical)
                {
                    // Is this the new leftmost pixel?
                    if (x < lowestPixel) lowestPixel = x;

                    // Is this the new rightmost pixel?
                    if (x > highestPixel) highestPixel = x;
                }
                else
                {
                    // Is this the new leftmost pixel?
                    if (y < lowestPixel) lowestPixel = y;

                    // Is this the new rightmost pixel?
                    if (y > highestPixel) highestPixel = y;
                }

                // Next pixel
                x += xInc;
                y += yInc;
            }

            // Track information about this seam
            seam.totalEnergy = totalEnergy;
            seam.totalDiff = totalDiff;
            seam.fromPixel = index - lowestPixel;
            seam.toPixel = highestPixel - index;

            // Return the seam
            return seam;
        }

        // Find the lowest energy seam in the array
        // Since we're sometimes comparing horizontal to vertical, we need to compute based on an average
        public override Seam FindLowestEnergy(Direction direction, ComparisonMethod comparisonMethod, Size size)
        {
            int lowestEnergyIndex = 0;
            double lowestEnergy = double.MaxValue;
            double energyCompare = 0;                   // The energy value to compare to lowest
            int maxIndex = 0;
            Seam[] seams = null;

            if (direction == Direction.Vertical)
            {
                seams = verticalSeams;
                maxIndex = size.Width;
            }
            else if (direction == Direction.Horizontal)
            {
                seams = horizontalSeams;
                maxIndex = size.Height;
            }

            // Find the lowest energy seam
            for (int index = 0; index < maxIndex; index++)
            {
                if (comparisonMethod == ComparisonMethod.Total)
                {
                    // Compare simple totals for each seam
                    energyCompare = seams[index].totalDiff;
                }
                else if (comparisonMethod == ComparisonMethod.Average)
                {
                    // Compare the average energy value for each seam (depends on seam direction)
                    if (seams[index].direction == Direction.Vertical)
                    {
                        energyCompare = seams[index].totalDiff / size.Height;
                    }
                    else if (seams[index].direction == Direction.Horizontal)
                    {
                        energyCompare = seams[index].totalDiff / size.Width;
                    }
                }
                else if (comparisonMethod == ComparisonMethod.DiffBias)
                {
                    // TODO: Experimental computation...not sure how well it really works
                    if (seams[index].direction == Direction.Vertical)
                    {
                        energyCompare = seams[index].totalEnergy / size.Height;
                    }
                    else if (seams[index].direction == Direction.Horizontal)
                    {
                        energyCompare = seams[index].totalEnergy / size.Width;
                    }

                    energyCompare += seams[index].totalDiff;
                }

                // Is this the lowest energy seam so far?
                if (energyCompare < lowestEnergy)
                {
                    lowestEnergy = energyCompare;
                    lowestEnergyIndex = index;
                }
            }

            seams[lowestEnergyIndex].compareValue = lowestEnergy;       // Set value to compare (since it won't always be simple)

            return seams[lowestEnergyIndex];
        }

        public override void UpdateSeamEnergy(Direction direction, int x, int y)
        {
            if (direction == Direction.Vertical)
            {
                // If we remove a pixel in the vertical direction, we need to subtract the energy values from the horizontal sums
                horizontalSeams[y].totalEnergy -= EnergyFunction.EnergyMap[x, y];
                if (y > 0)
                {
                    horizontalSeams[y].totalDiff -= verticalSeams[x].seamPixels[y - 1].pixelDiff;
                }
            }
            else if (direction == Direction.Horizontal)
            {
                // If we remove a pixel in the horizontal direction, we need to subtract the energy values from the vertical sums
                verticalSeams[x].totalEnergy -= EnergyFunction.EnergyMap[x, y];
                if (x > 0)
                {
                    verticalSeams[x].totalDiff -= horizontalSeams[y].seamPixels[x - 1].pixelDiff;
                }
            }
        }
    }
}
