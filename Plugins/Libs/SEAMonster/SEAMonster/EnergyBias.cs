using System;
using System.Collections.Generic;
using System.Text;

using System.Drawing;
using System.Drawing.Imaging;

namespace SEAMonster
{
    public class EnergyBias
    {
        private int[,] energyBiasMap;   // Energy bias map (to influence carving)

        public int[,] EnergyBiasMap
        {
            set { energyBiasMap = value; }
            get { return energyBiasMap; }
        }

        public EnergyBias(Size size)
        {
            // Initialize bias array
            energyBiasMap = new int[size.Width, size.Height];
        }

        public int GetBias(int x, int y, Size size)
        {
            if (energyBiasMap != null)
            {
                if (x >= 0 && x < size.Width && y >= 0 && y < size.Height)
                {
                    return energyBiasMap[x, y];
                }
                else
                {
                    // Pixel is out of bounds
                    return 0;
                }
            }
            else
            {
                // No bias map set
                return 0;
            }
        }
    }
}
