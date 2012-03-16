using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using ImageResizer.Util;

namespace ImageResizer.Plugins.CustomOverlay {
    public class LayoutEngine {

        public PointF[] Layout(Overlay o, Resizing.ImageState s, Size nativeSize) {

            //What's the polygon w/h?
            double cw = PolygonMath.Dist(o.Poly[0], o.Poly[1]);
            double ch = PolygonMath.Dist(o.Poly[1], o.Poly[2]);
            //Take a copy to shrink to content size
            double w = cw;
            double h = ch;
            //Apply limits
            if (o.MaxOverlaySize.Width > 0 && o.MaxOverlaySize.Width < w) w = o.MaxOverlaySize.Width;
            if (o.MaxOverlaySize.Height > 0 && o.MaxOverlaySize.Height < h) h = o.MaxOverlaySize.Height;
            //Shrink to keep aspect ratio
            if (w / h > nativeSize.Width / nativeSize.Height) {
                w = nativeSize.Width / nativeSize.Height * h;
            } else {
                h = w * nativeSize.Height / nativeSize.Width;
            }
            //Let's define our width/height offsets
            double ox = 0; double oy = 0; ;

            //Apply alignment to ox, oy
            if (o.Align == ContentAlignment.BottomLeft || o.Align == ContentAlignment.MiddleLeft || o.Align == ContentAlignment.TopLeft)
                ox = 0;
            if (o.Align == ContentAlignment.BottomCenter || o.Align == ContentAlignment.MiddleCenter || o.Align == ContentAlignment.TopCenter)
                ox = (cw - w) / 2;
            if (o.Align == ContentAlignment.BottomRight || o.Align == ContentAlignment.MiddleRight || o.Align == ContentAlignment.TopRight)
                ox = cw - w;
            if (o.Align == ContentAlignment.TopLeft || o.Align == ContentAlignment.TopCenter || o.Align == ContentAlignment.TopRight)
                oy = 0;
            if (o.Align == ContentAlignment.MiddleLeft || o.Align == ContentAlignment.MiddleCenter || o.Align == ContentAlignment.MiddleRight) 
                oy = (ch - h) / 2;
            if (o.Align == ContentAlignment.BottomLeft || o.Align == ContentAlignment.BottomCenter || o.Align == ContentAlignment.BottomRight)
                oy = ch - h;

            //Now, we need to rotate everything to match the rotation of the original parallelogram
            double angle = -Math.Atan2(o.Poly[1].Y - o.Poly[0].Y, o.Poly[1].X - o.Poly[0].X);



            PointF[] t = new PointF[4];
            t[0] = CreateVector(o.Poly[0], angle, ox);
            t[1] = CreateVector(o.Poly[0], angle, ox + w);
            t[2] = CreateVector(o.Poly[2], angle, ox);
            t[3] = CreateVector(o.Poly[2], angle, ox + w);

            return t;
        }


        public static PointF CreateVector(PointF origin, double radians, double length) {
            /*
                         *  2D Rotation
                         *  A point <x,y> can be rotated around the origin <0,0> by running it through the following equations 
                         * to get the new point <x',y'> :
                         * x' = cos(theta)*x - sin(theta)*y //cos(90) or cos(-90) = 0
                         * y' = sin(theta)*x + cos(theta)*y //sin(90) or sin(-90) = +/- 1
            */
            return new PointF(
                (float)(Math.Cos(radians) * length - Math.Sin(radians) * 0) + origin.X,
                (float)(Math.Sin(radians) * length + Math.Cos(radians) * 0) + origin.Y);
        }
    }
}
