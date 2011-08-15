using System;
using System.Collections.Generic;
using System.Text;

using System.Drawing;
using System.Drawing.Imaging;

namespace SEAMonster.EnergyFunctions
{
    public class Blue: EnergyFunction
    {
        public override string ToString()
        {
            return "Blue";
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
                        energyMap[x, y] = Common.GetColor(bmd, size, x, y, 0);
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
