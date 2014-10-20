using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using ImageResizer.Util;
using ImageResizer.Resizing;

namespace ImageResizer.Plugins.CustomOverlay {
    public class LayoutEngine {


        protected double GetAngleFromXAxis(PointF a, PointF b) {
            return -Math.Atan2(b.Y - a.Y, b.X - a.X);
        }

        /// <summary>
        /// Translates the specified points from the source bitmap coordinate space to the destination image coordinate space.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public PointF[] TranslatePoints(PointF[] points, ImageState s) {
            PointF[] moved = new PointF[points.Length];
            PointF[] dest = s.layout["image"];
            double newWidth = PolygonMath.Dist(dest[0], dest[1]);
            double newHeight = PolygonMath.Dist(dest[1], dest[2]);
            double xScale = newWidth / s.copyRect.Width;
            double yScale = newHeight / s.copyRect.Height;

            double angle = GetAngleFromXAxis(dest[0], dest[1]);

            for (int i = 0; i < points.Length; i++) {
                PointF p = points[i];
                p.X -= s.copyRect.X; //Translate
                p.Y -= s.copyRect.Y;
                p.X = (float)(p.X * xScale); //Scale
                p.Y = (float)(p.Y * yScale);
                p = PolygonMath.RotateVector(p, angle, dest[0]); //Rotate
                p.X += dest[0].X;
                p.Y += dest[1].Y;
                moved[i] = p;
            }
            return moved;
        }


        public PointF[] GetOverlayParalellogram(Overlay o, Size nativeSize, ImageState translateToFinalCoordinateSpace) {

            //What's the polygon w/h?
            double cw = PolygonMath.Dist(o.Poly[0], o.Poly[1]);
            double ch = PolygonMath.Dist(o.Poly[1], o.Poly[2]);
            //Take a copy to shrink to content size
            double w = cw;
            double h = ch;

            double aspect = (double)nativeSize.Height / (double)nativeSize.Width;

            //If specified, what percentage of the space do we use?
            if (o.PolyWidthInLogoPixels > 0) {
                w = cw * (double)nativeSize.Width / o.PolyWidthInLogoPixels;
                if (o.RespectOnlyMatchingBound) h = w * aspect;
            }

            if (o.PolyHeightInLogoPixels > 0) {
                h = ch * (double)nativeSize.Height / o.PolyHeightInLogoPixels;
                if (o.RespectOnlyMatchingBound && o.PolyWidthInLogoPixels <= 0) w = h / aspect;
            }
            
            //Shrink to keep aspect ratio
            if (w / h > 1 / aspect) {
                w = h / aspect;
            } else {
                h = w * aspect;
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
            double angle = Math.Atan2(o.Poly[1].Y - o.Poly[0].Y, o.Poly[1].X - o.Poly[0].X);



            PointF[] t = new PointF[4];
            t[0] = CreateVector(CreateVector(o.Poly[0], angle, ox), angle + Math.PI / 2, oy);
            t[1] = CreateVector(t[0], angle, w);
            t[2] = CreateVector(t[1], angle + Math.PI / 2, h);
            t[3] = CreateVector(t[0], angle + Math.PI / 2, h);

            //Translate the points if a ImageState instance was specified
            if (translateToFinalCoordinateSpace != null) return this.TranslatePoints(t, translateToFinalCoordinateSpace);
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
                (float)(Math.Cos(radians) * length) + origin.X,
                (float)(Math.Sin(radians) * length) + origin.Y);
        }
    }
}
