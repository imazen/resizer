using System;
using System.Collections.Generic;
using System.Text;

using System.Drawing;
using System.Drawing.Imaging;

namespace SEAMonster.EnergyFunctions
{
    public class Luminance: EnergyFunction
    {
        public override string ToString()
        {
            return "Luminance";
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
                        energyMap[x, y] = Common.GetPixel(bmd, size, x, y);
                    }
                }
            }
        }

        public override void RecomputeSeam(BitmapData bmd, Size size, Seam seam)
        {
            return;
        }
    }
}
