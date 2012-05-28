using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using AForge.Imaging;
using AForge.Imaging.Filters;

namespace ImageResizer.Plugins.RedEye {
    public class CSearcher  {


        public RectangleF LocateEye(Bitmap img, Rectangle bounds, PointF estimatedPosition, float positionalWeight) {
            Bitmap red = null;
            try {
                //Get filtered image
                using (Bitmap area = new Crop(bounds).Apply(img)) {
                    red = new RedEyeFilter(2).Apply(area);
                }

                float maxRadius = img.Width * 0.025F;
                //
                RectangleF result = Locate(red, new PointF(estimatedPosition.X - bounds.X, estimatedPosition.Y - bounds.Y), positionalWeight, maxRadius);
                result.X += bounds.X;
                result.Y += bounds.Y;
                return result;
            } finally {
                if (red != null) red.Dispose();
            }
        }


        protected RectangleF Locate(Bitmap f, PointF pos, float weight, float maxEyeRadius) {
            return new RectangleF();
        }
    }
}
