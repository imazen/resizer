using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Plugins.RedEye {

    //int sn = 1;
    //str = s.settings["r.sn"]; //multiple negative weights
    //if (string.IsNullOrEmpty(str) || int.TryParse(str, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out sn)) sn = 1;



    //str = s.settings["r.conv"]; //convolution multiplier
    //if (!string.IsNullOrEmpty(str) && int.TryParse(str, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out i))
    //    new Convolution(EyeKernels.Scale(EyeKernels.GetStandardKernel(i),1,sn)).ApplyInPlace(s.destBitmap);

    //str = s.settings["r.econv"]; //convolution multiplier
    //if (!string.IsNullOrEmpty(str) && int.TryParse(str, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out i))
    //    new Convolution(EyeKernels.Scale(EyeKernels.GetElongatedKernel(i), 1, sn)).ApplyInPlace(s.destBitmap);
                



    public class EyeKernels {

        /// <summary>
        /// For eyes looking at the camera
        /// </summary>
        /// <returns></returns>
        public static int[,] GetStandardKernel(int factor) {
            return GrowKernel(new int[5, 5] {
            {-2,-4,-7,-4,-2},
            {-9,20,20,20,-9},
            {-20,20,0,20,-20},
            {-10,20,20,20,-10},
            {-3,-5,-10,-5,-3}}, factor);  
        }


        public static int[,] Scale(int[,] k, int postiveFactor, int negativeFactor) {
            int[,] k2 = new int[k.GetLength(0), k.GetLength(1)];
            for (int i = 0; i < k.GetLength(0); i++)
                for (int j = 0; j < k.GetLength(1); j++)
                    k2[i, j] = k[i, j] > 0 ? k[i, j] * postiveFactor : k[i, j] * negativeFactor;
            return k2;
        }

        /// <summary>
        /// Eyes that are not looking at the camera directly only have a cat-like red shape. 
        /// </summary>
        /// <returns></returns>
        public static int[,] GetElongatedKernel(int factor) {
            return GrowKernel(new int[5, 5] {
            {0,-3,-10,-3,0},
            {-3,-10,53,-10,-3},
            {-10,-10,53,-10,-10},
            {-3,-10,53,-10,-3},
            {0,-3,-10,-3,0}},factor);
        }


        public static int[,] GrowKernel(int[,] k, int factor, double snap = 3) {
            if (factor == 1) return k;
            int oldSize = k.GetLength(0);
            int newSize = oldSize * factor;
            int[,] k2 = new int[newSize, newSize];
            double scaleFactor = ((double)newSize - 1) / ((double)oldSize - 1);
            for (int i = 0; i < newSize; i++) {
                for (int j = 0; j < newSize; j++) {
                    double oldI = Math.Min(oldSize - 1, (double)i / (double)scaleFactor);
                    double oldJ = Math.Min(oldSize - 1, (double)j / (double)scaleFactor);
                    double a = k[(int)Math.Floor(oldI), (int)Math.Floor(oldJ)];
                    double b = k[(int)Math.Ceiling(oldI), (int)Math.Floor(oldJ)];
                    double c = k[(int)Math.Floor(oldI), (int)Math.Ceiling(oldJ)];
                    double d = k[(int)Math.Ceiling(oldI), (int)Math.Ceiling(oldJ)];
                    double iFactor = oldI - Math.Floor(oldI);
                    double jFactor = oldJ - Math.Floor(oldJ);
                    if (jFactor < 0.5) jFactor /= snap; else jFactor = 1 - (1 - jFactor) / snap;
                    if (iFactor < 0.5) iFactor /= snap; else iFactor = 1 - (1 - iFactor) / snap;


                    double avg = a * (1 - iFactor) * (1 - jFactor) + b * (iFactor) * (1 - jFactor) + c * (1 - iFactor) * jFactor + d * iFactor * jFactor;
                    k2[i, j] = (int)Math.Round(avg);
                }
            }
            return k2;
        }

    }
}
