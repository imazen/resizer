using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using AForge.Imaging;

namespace ImageResizer.Plugins.RedEye {
    class AutoThresholdCorrection {
        //In automatic mode, thresholds need to kick in
        //In manual mode, we have to autodetect (a) do we need to sublocate the eye? or (b) is they eye already selected such that trying to locate it will fail? 

        protected unsafe void SemiAutoCorrectRedEye(UnmanagedImage image, int cx, int cy) {
            Rectangle bright = new GridSearch().FindBrightestSquare(image, cx, cy);
            CorrectRedEye(image, bright.X + bright.Width / 2, bright.Y + bright.Height / 2, bright.Width / 2, 3 + (int)(bright.Width * 0.2));
        }


        protected unsafe void CorrectRedEye(UnmanagedImage image, int cx, int cy, int radius, int fadeEdge = 3, double threshold = -1) {
            int pixelSize = System.Drawing.Image.GetPixelFormatSize(image.PixelFormat) / 8;
            if (pixelSize > 4) throw new Exception("Invalid pixel depth");

            long scan0 = (long)image.ImageData;
            long stride = image.Stride;
            // do the job
            byte* src;
            //Establish bounds
            int top = Math.Max(0, cy - radius - fadeEdge);
            int bottom = Math.Min(image.Height, cy + radius + fadeEdge + 1);
            int left = Math.Max(0, cx - radius - fadeEdge);
            int right = Math.Min(image.Width, cx + radius + fadeEdge + 1);

            double fade = 0;
            double red = 0;
            byte gray;

            //Scan region (pass 1) using RMS
            double meansq = 0;
            long pixels = 0;
            for (int y = top; y < bottom; y++) {
                src = (byte*)(scan0 + y * stride + (left * pixelSize));
                for (int x = left; x < right; x++, src += pixelSize) {
                    pixels++;
                    meansq = meansq * ((pixels - 1) / pixels);
                    if (src[RGB.R] == 0) continue;

                    meansq += (src[RGB.R] - Math.Max(src[RGB.G], src[RGB.B])) / (double)src[RGB.R] * 100 / pixels;
                }
            }
            //Get a value between 0 and 1
            double rms = Math.Sqrt(meansq) / 100;

            if (threshold < 0) threshold = rms * 0.4;

            //Scan region
            for (int y = top; y < bottom; y++) {
                src = (byte*)(scan0 + y * stride + (left * pixelSize));
                for (int x = left; x < right; x++, src += pixelSize) {
                    if (src[RGB.R] == 0) continue; //Because 0 will crash the formula
                    //Calculate distance from center
                    fade = Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    //Calculate the fade factor (using a linear fade when we are between radius and radius + fadeEdge from the center). 
                    fade = (fade <= radius) ? 1 : Math.Max(0,
                        ((fadeEdge + radius - fade) / fadeEdge));

                    //Calculate red baddness between 0 and 1
                    red = (src[RGB.R] - Math.Max(src[RGB.G], src[RGB.B])) / (double)src[RGB.R];

                    //Skip if we're outside the threshold
                    if (red < threshold) continue;

                    //Calculate monochrome alternative
                    gray = (byte)(src[RGB.G] * 0.587f + src[RGB.B] * 0.114f);

                    //Apply monochrome alternative
                    src[RGB.R] = (byte)((fade * gray) + (1 - fade) * src[RGB.R]);
                }
            }
        }



    }
}
