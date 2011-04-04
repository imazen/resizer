using System;
using System.Collections.Generic;
using System.Text;
using SEAMonster;
using System.Drawing;

namespace fbs.ImageResizer.SeamCarving {
    public class SeamCarving {
        public SeamCarving() {
            
        }


        public Bitmap Carve(Bitmap b) {
            Direction d = Direction.Optimal;
            ComparisonMethod cm = ComparisonMethod.DiffBias;

            SEAMonster.SMImage sm = new SEAMonster.SMImage(b, SEAMonster.SeamFunctions.Standard, SEAMonster.EnergyFunctions.Sobel);


            // Squash the whole image
            Size minimumSize = new Size(Convert.ToInt16(txtX.Text), Convert.ToInt16(txtY.Text));
            this.smImage.Carve(direction, comparisonMethod, minimumSize);

            ToggleControls(true);

        }
    }
}
