using System;
using System.Collections.Generic;
using System.Text;

using System.Drawing;
using System.Drawing.Imaging;

namespace SEAMonster.EnergyFunctions
{
    public abstract class EnergyFunction
    {
        protected int width = 0;          // Width of the energy map
        protected int height = 0;         // Height of the energy map
        protected byte[,] energyMap;      // Energy map array

        public byte[,] EnergyMap
        {
            get { return energyMap; }
        }

        public abstract void Recompute(BitmapData bmd, Size size);
        public abstract void RecomputeSeam(BitmapData bmd, Size size, Seam seam);

        public byte GetEnergy(int x, int y, Size size)
        {
            if (x >= 0 && x < size.Width && y >= 0 && y < size.Height)
            {
                return energyMap[x, y];
            }
            else
            {
                // Pixel is out of bounds, so return maximum energy
                return byte.MaxValue;
            }
        }
    }
}
