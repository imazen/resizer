using System;
using System.Collections.Generic;
using System.Text;

using System.Drawing;
using System.Drawing.Imaging;

using SEAMonster.EnergyFunctions;

namespace SEAMonster.SeamFunctions
{
    public abstract class SeamFunction
    {
        protected Seam[] verticalSeams;
        protected Seam[] horizontalSeams;
        protected EnergyFunction energyFunction = null;
        protected EnergyBias energyBias = null;

        public EnergyFunction EnergyFunction
        {
            get { return energyFunction; }
            set { energyFunction = value; }
        }

        public EnergyBias EnergyBias
        {
            get { return energyBias; }
            set { energyBias = value; }
        }

        public abstract void ComputeAllSeams(Direction direction, Size size);
        public abstract void RecomputeSeams(Seam seam, Size size);
        public abstract void UpdateSeamEnergy(Direction direction, int x, int y);
        public abstract Seam FindLowestEnergy(Direction direction, ComparisonMethod comparisonMethod, Size size);
    }
}
