using System;
using System.Collections.Generic;
using System.Text;

using System.Drawing;
using System.Drawing.Imaging;

namespace SEAMonster.EnergyFunctions
{
    public class Random: EnergyFunction
    {
        public override string ToString()
        {
            return "Random";
        }

        public override void Recompute(BitmapData bmd, Size size)
        {
            if (bmd != null)
            {
                
                System.Random rnd = new System.Random();
                energyMap = new byte[bmd.Width, bmd.Height];

                for (int x = 0; x < bmd.Width; x++)
                {
                    for (int y = 0; y < bmd.Height; y++)
                    {
                        if (x >= 0 && x < size.Width && y >= 0 && y < size.Height)
                        {
                            energyMap[x, y] = (byte)rnd.Next(255);
                        }
                        else
                        {
                            energyMap[x, y] = byte.MaxValue;
                        }
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
