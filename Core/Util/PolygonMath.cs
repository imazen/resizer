/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ImageResizer.Util
{
    /// <summary>
    /// Defines a collection of utility functions for manipulating polygons. These functions may be (re)moved in future releases; be prepared. 
    /// </summary>
    public class PolygonMath
    {
        /// <summary>
        /// Rounds the elements of the specified array [not used]
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static PointF[] RoundPoints(PointF[] a)
        {
            
            ForEach(a, delegate(object o){
                PointF p = (PointF)o;
                p.X = (float)Math.Round(p.X);
                p.Y = (float)Math.Round(p.Y);
                return p;
            });
            return a;
        }
        /// <summary>
        /// Rounds the elements of the specified array [not used]
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        [CLSCompliant(false)]
        public static PointF[,] RoundPoints(PointF[,] a)
        {
            
            ForEach(a, delegate(object o)
            {
                PointF p = (PointF)o;
                p.X = (float)Math.Round(p.X);
                p.Y = (float)Math.Round(p.Y);
                return p;
            });
            return a;
        }

        public delegate object ForEachFunction(object o);
        /// <summary>
        /// Modifies the specified array by applying the specified function to each element.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="func">object delegate(object o){}</param>
        /// <returns></returns>
        public static void ForEach(Array a, ForEachFunction func)
        {
            long[] ix = new long[a.Rank];
            //Init index
            for (int i = 0; i < ix.Length;i++) ix[i] = a.GetLowerBound(i);

            //Loop through all items
            for (long i = 0; i < a.LongLength; i++){
                a.SetValue(func(a.GetValue(ix)),ix);

                //Increment ix, the index
                for (int j = 0; j < ix.Length; j++)
                {
                    if (ix[j] < a.GetUpperBound(j))
                    {
                        ix[j]++;
                        break; //We're done incrementing.
                    }
                    else
                    {
                        //Ok, reset this one and increment the next.
                        ix[j] = a.GetLowerBound(j);
                        //If this is the last dimension, assert
                        //that we are at the last element
                        if (j == ix.Length - 1)
                        {
                            if (i < a.LongLength - 1) throw new Exception();
                        }
                        continue;
                    }
                }
            }
            return;
        }

        /// <summary>
        /// Rotates the specified polygon (or set of points) around the origin. 
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="degrees"></param>
        /// <returns></returns>
        public static PointF[] RotatePoly(PointF[] poly, double degrees)
        {
            PointF[] pts = new PointF[poly.Length];
            for (int i = 0; i < poly.Length; i++)
                pts[i] = RotateVector(poly[i], degrees * Math.PI / 180);
            return pts;
        }

        /// <summary>
        /// Rotates the specified polygon (or set of points) around the origin. 
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="degrees"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static PointF[] RotatePoly(PointF[] poly, double degrees, PointF origin) {
            PointF[] pts = new PointF[poly.Length];
            for (int i = 0; i < poly.Length; i++)
                pts[i] = RotateVector(poly[i], degrees * Math.PI / 180,origin);
            return pts;
        }

        public static PointF[] ScalePoints(PointF[] poly, double xfactor, double yfactor, PointF origin) {
            PointF[] pts = new PointF[poly.Length];
            for (int i = 0; i < poly.Length; i++)
                pts[i] = ScalePoint(poly[i], xfactor,yfactor, origin);
            return pts;
        }
        public static PointF ScalePoint(PointF point, double xfactor, double yfactor, PointF origin) {
            return new PointF((float)((point.X - origin.X) * xfactor + origin.X),
                               (float)((point.Y - origin.Y) * yfactor + origin.Y));
        }

        /// <summary>
        /// Returns a clockwise array of points on the rectangle.
        /// Point 0 is top-left.
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public static PointF[] ToPoly(RectangleF rect)
        {
            PointF[] r = new PointF[4];
            r[0] = rect.Location;
            r[1] = new PointF(rect.Right, rect.Top);
            r[2] = new PointF(rect.Right, rect.Bottom);
            r[3] = new PointF(rect.Left, rect.Bottom);
            return r;
        }
        /// <summary>
        /// Moves the polygon so that the upper-left corner of its bounding box is located at 0,0.
        /// </summary>
        /// <param name="poly"></param>
        /// <returns></returns>
        public static PointF[] NormalizePoly(PointF[] poly)
        {
            RectangleF box = GetBoundingBox(poly);
            return PolygonMath.MovePoly(poly, new PointF(-box.X, -box.Y));
        }
        /// <summary>
        /// Rotates the specified point around the origin.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="radians"></param>
        /// <returns></returns>
        public static PointF RotateVector(PointF v, double radians)
        {
/*
             *  2D Rotation
             *  A point <x,y> can be rotated around the origin <0,0> by running it through the following equations 
             * to get the new point <x',y'> :
             * x' = cos(theta)*x - sin(theta)*y //cos(90) or cos(-90) = 0
             * y' = sin(theta)*x + cos(theta)*y //sin(90) or sin(-90) = +/- 1
*/
            return new PointF(
                (float)(Math.Cos(radians) * v.X - Math.Sin(radians) * v.Y),
                (float)(Math.Sin(radians) * v.X + Math.Cos(radians) * v.Y));
        }

                /// <summary>
        /// Rotates the specified point around the specified origin.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="radians"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public static PointF RotateVector(PointF v, double radians, PointF origin)
        {
/*
             * 2D Rotation
             *  A point <x,y> can be rotated around the origin <0,0> by running it through the following equations 
             * to get the new point <x',y'> :
             * x' = cos(theta)*x - sin(theta)*y //cos(90) or cos(-90) = 0
             * y' = sin(theta)*x + cos(theta)*y //sin(90) or sin(-90) = +/- 1
*/
            return new PointF(
                (float)(Math.Cos(radians) * (v.X - origin.X) - Math.Sin(radians) * (v.Y - origin.Y)) + origin.X,
                (float)(Math.Sin(radians) *  (v.X - origin.X) + Math.Cos(radians) *  (v.Y - origin.Y)) + origin.Y);
        }



        /// <summary>
        /// Returns a modified version of the specified vector with the desired length.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static PointF ChangeMagnitude(PointF v, float length)
        {
            double curLength = Math.Sqrt((v.X * v.X) + (v.Y * v.Y));
            float factor = (float)(length / curLength);
            return new PointF(v.X * factor, v.Y * factor);
        }


        /// <summary>
        /// Returns a bounding box for the specified set of points.
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static RectangleF GetBoundingBox(PointF[] points)
        {
            float left = float.MaxValue;
            float top = float.MaxValue;
            float right = float.MinValue;
            float bottom = float.MinValue;
            foreach (PointF f in points)
            {
                if (f.X < left) left = f.X;
                if (f.X > right) right = f.X;
                if (f.Y < top) top = f.Y;
                if (f.Y > bottom) bottom = f.Y;
            }
            return new RectangleF(left, top, right - left, bottom - top);
        }
        /// <summary>
        /// Returns a bounding box for the specified set of points. Odd points are Y values, even points are X values
        /// </summary>
        /// <param name="flattenedPoints"></param>
        /// <returns></returns>
        public static RectangleF GetBoundingBox(double[] flattenedPoints)  {
            double? minx = null, maxx = null, miny = null, maxy = null;
            for (var i = 0; i < flattenedPoints.Length; i++) {
                var v = flattenedPoints[i];
                if (i % 2 == 0) {
                    if (minx == null || v < minx.Value) minx = v;
                    if (maxx == null || v > maxx.Value) maxx = v;
                } else {
                    if (miny == null || v < miny.Value) miny = v;
                    if (maxy == null || v > maxy.Value) maxy = v;
                }
            }
            return new RectangleF((float)minx, (float)miny, (float)(maxx - minx), (float)(maxy - miny));
        }
        /// <summary>
        /// Returns a modified version of the array, with each element being offset by the specified amount.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static PointF[] MovePoly(PointF[] points, PointF offset)
        {
            PointF[] pts = new PointF[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                pts[i].X = points[i].X + offset.X;
                pts[i].Y = points[i].Y + offset.Y;
            }
            return pts;
        }
        /// <summary>
        /// Returns true if the member elements of the specified arrays match, and the arrays 
        /// are of the same length.
        /// </summary>
        /// <param name="a1"></param>
        /// <param name="a2"></param>
        /// <returns></returns>
        public static Boolean ArraysEqual(PointF[] a1, PointF[] a2)
        {

            if (a1.Length != a2.Length) return false;
            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i]) return false;
            }
            return true;
        }

        /// <summary>
        /// Moves element 4 to spot 3 and truncates to 3 elements.
        /// For compatiblity with Graphics.DrawImage
        /// </summary>
        /// <param name="quad"></param>
        /// <returns></returns>
        public static PointF[] getParallelogram(PointF[] quad)
        {
            PointF[] p = new PointF[3];
            p[0] = quad[0];
            p[1] = quad[1];
            p[2] = quad[3];
            return p;
        }

        /// <summary>
        /// Determines the width and height of the paralellogram.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static SizeF getParallelogramSize(PointF[] p) {
            double width = Math.Sqrt(Math.Pow(p[0].X - p[1].X,2) + Math.Pow(p[0].Y - p[1].Y,2));
            double height = Math.Sqrt(Math.Pow(p[1].X - p[2].X,2) + Math.Pow(p[1].Y - p[2].Y,2));
            return new SizeF((float)width, (float)height);
        }


        /// <summary>
        /// Grabs a single-dimension array from a 2 dimensional array, using the specified primary index.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public static PointF[] GetSubArray(PointF[,] array, int index)
        {
            PointF[] sub = new PointF[array.GetUpperBound(1) + 1];
            for (int i = 0; i < array.GetUpperBound(1) + 1; i++)
                sub[i] = array[index, i];
            return sub;
        }
        /// <summary>
        /// Approximates a radial brush using a high-rez PathGradientBrush.
        /// </summary>
        /// <param name="inner"></param>
        /// <param name="outer"></param>
        /// <param name="pt"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static Brush GenerateRadialBrush(Color inner, Color outer, PointF pt, float width)
        {
            //This should approximate one outer point per pixel.
            PointF[] path = new PointF[(int)Math.Round(width * 2 * Math.PI) + 1];
            for (int i = 0; i < path.Length - 1; i++)
            {
                double radians = ((double)i - width) / width; //calculate the radians at this index.
                //Calculate a point based off the radians.
                path[i] = new PointF((float)(Math.Sin(radians) * width + pt.X), (float)(Math.Cos(radians) * width + pt.Y));
            }
            path[path.Length - 1] = path[0]; //Loop back to complete the circle.

            PathGradientBrush b = new PathGradientBrush(path);
            b.CenterColor = inner;
            b.CenterPoint = pt;
            b.WrapMode = WrapMode.Clamp;
            //All outer colors are the same.
            Color[] colors = new Color[path.Length];
            for (int i = 0; i < colors.Length; i++) colors[i] = outer;
            b.SurroundColors = colors;
            b.SetSigmaBellShape(1);
            return b;
        }

        /// <summary>
        /// Scales 'inner' to fit inside 'bounding' while maintaining aspect ratio. Upscales and downscales.
        /// </summary>
        /// <param name="inner"></param>
        /// <param name="bounding"></param>
        /// <returns></returns>
        public static SizeF ScaleInside(SizeF inner, SizeF bounding )
        {
            double innerRatio = inner.Width / inner.Height;
            double outerRatio = bounding.Width / bounding.Height;

            if (outerRatio > innerRatio)
            {
                //Width is wider - so bound by height.
                return new SizeF((float)(innerRatio * bounding.Height), (float)(bounding.Height));
            }
            else
            {
                //Height is higher, or aspect ratios are identical.
                return new SizeF((float)(bounding.Width), (float)(bounding.Width / innerRatio));
            }
        }

        /// <summary>
        /// Scales 'outer' to be equal or larger than 'innerBounds' while maintaining aspect ratio. Upscales and downscales.
        /// </summary>
        /// <param name="innerBounds"></param>
        /// <param name="outer"></param>
        /// <returns></returns>
        public static SizeF ScaleOutside(SizeF innerBounds, SizeF outer) {
            
            double innerRatio = innerBounds.Width / innerBounds.Height;
            double outerRatio = outer.Width / outer.Height;

            if (outerRatio > innerRatio) {
                //Width is wider - so bound by height.
                return new SizeF((float)(outerRatio * innerBounds.Height), (float)(innerBounds.Height));
            } else {
                //Height is higher, or aspect ratios are identical.
                return new SizeF((float)(innerBounds.Width), (float)(innerBounds.Width / outerRatio));
            }
        }

        
        /// <summary>
        /// Scales 'inner' to fit inside 'bounding' while maintaining aspect ratio. Only downscales.
        /// </summary>
        /// <param name="inner"></param>
        /// <param name="bounding"></param>
        /// <returns></returns>
        public static SizeF DownScaleInside(SizeF inner, SizeF bounding)
        {
            SizeF result = ScaleInside(inner, bounding);
            if (result.Width > inner.Width || result.Height > inner.Height) return inner;
            else return result;
        }
        /// <summary>
        /// Returns true if 'inner' fits inside or equals 'outer'
        /// </summary>
        /// <param name="inner"></param>
        /// <param name="outer"></param>
        /// <returns></returns>
        public static bool FitsInside(SizeF inner, SizeF outer)
        {
            if (inner.Width > outer.Width) return false;
            if (inner.Height > outer.Height) return false;
            return true;
        }
        /// <summary>
        /// Returns an array of parallelograms. These parallelgrams are the 'corners' outside each vertex in 'poly'.
        /// The adjacent edges are perpendicular to 'poly'. Point 1 of each parallelogram will match the respective point in 'poly'
        /// Points are clockwise.
        ///
        /// TODO - some rounding issues going on, not exact numbers here
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static PointF[,] GetCorners(PointF[] poly, float width)
        {
            //Build the widths array.
            float[] widths = new float[poly.Length];
            for(int i = 0; i < widths.Length;i++) widths[i] = width;
            //Call
            return GetCorners(poly,widths);
        }
        /// <summary>
        /// Returns an array of parallelograms. These parallelgrams are the 'corners' outside each vertex in 'poly'.
        /// The adjacent edges are perpendicular to 'poly'. Point 1 of each parallelogram will match the respective point in 'poly'
        /// Points are clockwise.
        /// 
        /// Each float in widths[] corresponds to the point in poly[]. This is the distance to go perpendicularly from 
        /// the line beween poly[i] and poly[i +1].
        /// 
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="widths"></param>
        /// <returns></returns>
        public static PointF[,] GetCorners(PointF[] poly, float[] widths)
        {
            if (poly.Length != widths.Length) throw new ArgumentException("Arrays 'poly' and 'widths' must have the same number of elements");

            PointF[,] corners = new PointF[poly.Length, 4];
            int end = (poly.Length - 1); //the last index in the array
            for (int i = 0; i < poly.Length; i++)
            {

                //Get next an prev points. Wrap around. Clockwise.
                PointF next = (i < end) ? poly[i + 1] : poly[i - end];
                PointF prev = (i > 0) ? poly[i - 1] : poly[i + end];
                PointF current = poly[i];

                float prevWidth = (i > 0) ? widths[i-1] : widths[i + end];
                float width = widths[i];

                //Radians = pi/(180*degrees)
                //Degrees = radians*180/pi
                //Get vectors perpendicular to next and prev, with lengths of 'offset'.
                PointF pP = ChangeMagnitude(
                    RotateVector(new PointF(prev.X - current.X, prev.Y - current.Y), Math.PI / 2) //rotate 90 clockwise.
                    , prevWidth); //scale to offset length.

                PointF pN = ChangeMagnitude(
                    RotateVector(new PointF(next.X - current.X, next.Y - current.Y), Math.PI / -2) //rotate 90 counter-clockwise.
                    , width); //scale to offset length.

                //Add to get points 2 and 4 of the parallelogram.
                //Add both to get point 3
                corners[i, 0] = current;
                corners[i, 1] = new PointF(current.X + pP.X, current.Y + pP.Y);
                corners[i, 2] = new PointF(current.X + pP.X + pN.X, current.Y + pP.Y + pN.Y);
                corners[i, 3] = new PointF(current.X + pN.X, current.Y + pN.Y);
            }
            return corners;
        }
        /// <summary>
        /// Returns an array of parallelograms. These parallelgrams are the 'sides' bounding the polygon.
        /// Points are clockwise. Point 1 is the top-left outer point, point 2 the top-right, point 3 the bottom-right, and point 4 the bottom-left.
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static PointF[,] GetSides(PointF[] poly, float width)
        {
            //Just grab the sides between the corners.
            PointF[,] corners = GetCorners(poly, width);
            PointF[,] sides = new PointF[corners.GetUpperBound(0) + 1, 4];
            for (int i = 0; i <= corners.GetUpperBound(0); i++)
            {
                int next = (i < corners.GetUpperBound(0)) ? i + 1 : i - corners.GetUpperBound(0);
                sides[i, 0] = corners[i, 3];
                sides[i, 3] = corners[i, 0];
                sides[i, 1] = corners[next, 1];
                sides[i, 2] = corners[next, 0];
            }
            return sides;
        }
        /// <summary>
        /// Expands all sides on the specified polygon by the specified offset. Assumes the polygon is concave.
        /// Returns a new polygon
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static PointF[] InflatePoly(PointF[] poly, float offset)
        {
            PointF[,] corners = GetCorners(poly, offset);
            PointF[] newPoly = new PointF[poly.Length];
            for (int i = 0; i <= corners.GetUpperBound(0); i++)
                newPoly[i] = corners[i, 2]; //Just grab the outer corner

            return newPoly;

        }
        /// <summary>
        /// Expands all sides on the specified polygon by the specified offsets. Assumes the polygon is concave.
        /// Returns a new polygon.
        /// 
        /// </summary>
        /// <param name="poly"></param>
        /// <param name="offsets">An array the same size as poly[], with the distances to expand the edges. Edges are between i and i+1</param>
        /// <returns></returns>
        public static PointF[] InflatePoly(PointF[] poly, float[] offsets)
        {
            PointF[,] corners = GetCorners(poly, offsets);
            PointF[] newPoly = new PointF[poly.Length];
            for (int i = 0; i <= corners.GetUpperBound(0); i++)
                newPoly[i] = corners[i, 2]; //Just grab the outer corner

            return newPoly;

        }
      
        /// <summary>
        /// Moves 'inner' so that the center of its bounding box equals the center of the bounding box of 'outer'
        /// </summary>
        /// <param name="inner"></param>
        /// <param name="outer"></param>
        /// <returns></returns>
        public static PointF[] CenterInside(PointF[] inner, PointF[] outer)
        {
            RectangleF inBox = GetBoundingBox(inner);
            RectangleF outBox = GetBoundingBox(outer);
           
            return MovePoly(NormalizePoly(inner), new PointF((outBox.Width - inBox.Width) / 2 + outBox.X,
                                                    (outBox.Height - inBox.Height) / 2 + outBox.Y));
        }


        /// <summary>
        /// Creates a rectangle of size 'size' with a center matching that of bounds.
        /// </summary>
        /// <returns></returns>
        public static RectangleF CenterInside(SizeF size, RectangleF bounds) {
            return new RectangleF(bounds.Width / 2 + bounds.X - (size.Width / 2), bounds.Height / 2 + bounds.Y - (size.Height / 2), size.Width, size.Height);
        }


        /// <summary>
        /// Rounds a floating-point rectangle to an integer rectangle using System.Round
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static Rectangle ToRectangle(RectangleF r)
        {
            return new Rectangle((int)Math.Round(r.X), (int)Math.Round(r.Y), (int)Math.Round(r.Width),(int)Math.Round( r.Height));
        }

        public static Size RoundPoints(SizeF sizeF) {
            return new Size((int)Math.Round(sizeF.Width), (int)Math.Round(sizeF.Height));
        }


        /// <summary>
        /// Aligns the specified rectangle object with its reference ('container') rectangle using the specified alignment. The container can be smaller than 'obj'.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="container"></param>
        /// <param name="align"></param>
        /// <returns></returns>
        public static RectangleF AlignWith(RectangleF obj, RectangleF container, ContentAlignment align) {
            if (align == ContentAlignment.BottomLeft || align == ContentAlignment.MiddleLeft || align == ContentAlignment.TopLeft)
                obj.X = container.X;
            if (align == ContentAlignment.BottomCenter || align == ContentAlignment.MiddleCenter || align == ContentAlignment.TopCenter)
                obj.X = container.X + (container.Width - obj.Width) / 2;
            if (align == ContentAlignment.BottomRight || align == ContentAlignment.MiddleRight || align == ContentAlignment.TopRight)
                obj.X = container.X + (container.Width - obj.Width);
            if (align == ContentAlignment.TopLeft || align == ContentAlignment.TopCenter || align == ContentAlignment.TopRight)
                obj.Y = container.Y;
            if (align == ContentAlignment.MiddleLeft || align == ContentAlignment.MiddleCenter || align == ContentAlignment.MiddleRight)
                obj.Y = container.Y + (container.Height - obj.Height) / 2;
            if (align == ContentAlignment.BottomLeft || align == ContentAlignment.BottomCenter || align == ContentAlignment.BottomRight)
                obj.Y = container.Y + (container.Height - obj.Height);

            return obj;
        }
        /// <summary>
        /// Aligns the specified polygon with its container (reference) polygon using the specified alignment. The container can be smaller than 'obj'.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="container"></param>
        /// <param name="align"></param>
        /// <returns></returns>
        public static PointF[] AlignWith(PointF[] obj, PointF[] container, ContentAlignment align) {
            RectangleF origBounds = PolygonMath.GetBoundingBox(obj);
            RectangleF newBounds = AlignWith(PolygonMath.GetBoundingBox(obj), PolygonMath.GetBoundingBox(container), align);
            return PolygonMath.MovePoly(obj, new PointF(newBounds.X - origBounds.X, newBounds.Y - origBounds.Y));
        }

        /// <summary>
        /// Returns a point equidistant beweteen A and B
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static PointF Average(PointF a, PointF b) {
            return new PointF((a.X + b.X) / 2, (a.Y + b.Y) / 2);
        }
        /// <summary>
        /// Verifies that the specified 4 points are not rotated - that each point shares either the x or y coordinate with the previous point
        /// </summary>
        /// <returns></returns>
        public static bool IsUnrotated(PointF[] a) {
            PointF lastPoint = a[a.GetUpperBound(0)];
            foreach (PointF p in a) {
                if (p.X != lastPoint.X && p.Y != lastPoint.Y) return false;
                lastPoint = p;
            }
            return true;
        }
        /// <summary>
        /// Returns the length of the shortest line segment in the given polygon.
        /// </summary>
        /// <returns></returns>
        public static double GetShortestPair(PointF[] poly) {
            PointF last = poly[poly.Length - 1];
            double max = 0;
            foreach (PointF p in poly) {
                double dist = Math.Sqrt((p.X - last.X) * (p.X - last.X) + (p.Y - last.Y) * (p.Y - last.Y));
                if (dist > max) max = dist;
                last = p;
            }
            return max;
        }

        /// <summary>
        /// Returns the distance between two points
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double Dist(PointF a, PointF b) {
            return Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));
        }

        /// <summary>
        /// Normalizes the given angle to a positive multiple of 90 degrees between 0 and 270.
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static double NormalizeTo90Intervals(double d) {
            d = d % 360;
            if (d < 0) d += 360;

            if (d >= 315 && d < 360) return 0;
            if (d >= 0 && d < 45) return 0;
            if (d >= 45 && d < 135) return 90;
            if (d >= 135 && d < 225) return 180;
            if (d >= 225 && d < 315) return 270;

            throw new Exception("Impossible");
        }

        public static RotateFlipType CombineFlipAndRotate(RotateFlipType flip, double angle) {
            if (flip != (RotateFlipType)FlipMode.None && 
                flip != (RotateFlipType)FlipMode.X && 
                flip != (RotateFlipType)FlipMode.Y && 
                flip != (RotateFlipType)FlipMode.XY)
                throw new ArgumentException("Valid flip values are RotateNoneFlipNone, RotateNoneFlipX, RotateNoneFlipY, and RotateNoneFlipXY. Rotation must be specified with Rotate or srcRotate instead. Received: " + flip.ToString());
            return CombineFlipAndRotate((FlipMode)flip, angle);
        }

        /// <summary>
        /// Combines the given flipping info and rotation angle into a RotateFlipType value. Rotation angle will snap to nearest 90-degree multiple
        /// </summary>
        /// <param name="flip"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static RotateFlipType CombineFlipAndRotate(FlipMode flip, double angle) {
            angle = NormalizeTo90Intervals(angle);
            if (flip == FlipMode.None) {
                return (RotateFlipType)(int)(angle / 90);
            } else if (flip == FlipMode.X) {
                return (RotateFlipType)(int)(4 + (angle / 90));
            } else if (flip == FlipMode.Y) {
                if (angle == 0) return (RotateFlipType)6;
                if (angle == 90) return (RotateFlipType)7;
                if (angle == 180) return (RotateFlipType)4;
                if (angle == 270) return (RotateFlipType)5;
            } else if (flip == FlipMode.XY) {
                if (angle == 0) return (RotateFlipType)2;
                if (angle == 90) return (RotateFlipType)3;
                if (angle == 180) return (RotateFlipType)0;
                if (angle == 270) return (RotateFlipType)1;
            } else {
                throw new ArgumentException("Invalid FlipMode value " + flip.ToString());
            }
            throw new ArgumentException("Invalid angle value " + angle.ToString());
        }


        /// <summary>
        /// Used for converting custom crop rectangle coordinates into a valid cropping rectangle. Positive values are relative to 0,0, negative values relative to width, height.
        /// X2 and Y2 values of 0 become width and height respectively. 
        /// </summary>
        /// <param name="cropValues">An array of 4 elements defining x1, y1, x2, and y2 of the cropping rectangle</param>
        /// <param name="xunits">The width x1 and x2 are relative to</param>
        /// <param name="yunits">The height y1 and y2 are relative to</param>
        /// <param name="imageSize">The size of the uncropped image</param>
        /// <returns></returns>
        public static RectangleF GetCroppingRectangle(double[] cropValues, double xunits, double yunits, SizeF imageSize) {
            RectangleF defValue = new RectangleF(new PointF(0, 0), imageSize);
            double[] c = cropValues;

            //Step 2, Apply units to values, resolving against imageSize
            for (int i = 0; i < c.Length; i++) {
                bool xvalue = i % 2 == 0;
                if (xvalue && xunits != 0) c[i] *= (imageSize.Width / xunits);
                if (!xvalue && yunits != 0) c[i] *= (imageSize.Height / yunits);

                //Prohibit values larger than imageSize
                if (xvalue && c[i] > imageSize.Width) c[i] = imageSize.Width;
                if (!xvalue && c[i] > imageSize.Height) c[i] = imageSize.Height;
            }

            //Step 3, expand width/height crop to 4-value crop (not currently used)
            if (c.Length == 2) {
                if (c[0] < 1 || c[1] < 1) return defValue; //We can't do anything with negative values here
                //Center horizontally and vertically.
                double x = (imageSize.Width - c[0]) / 2;
                double y = (imageSize.Height - c[1]) / 2;

                c = new double[] { x, y, x + c[0], y + c[1] };
            }

            double x1 = c[0], y1 = c[1], x2 = c[2], y2 = c[3];

            //allow negative offsets 
            if (x1 < 0) x1 += imageSize.Width;
            if (y1 < 0) y1 += imageSize.Height;
            if (x2 <= 0) x2 += imageSize.Width;
            if (y2 <= 0) y2 += imageSize.Height;


            //Require box stay in bounds.
            if (x1 < 0) x1 = 0; if (x2 < 0) x2 = 0;
            if (y1 < 0) y1 = 0; if (y2 < 0) y2 = 0;
            if (x1 > imageSize.Width) x1 = imageSize.Width;
            if (x2 > imageSize.Width) x2 = imageSize.Width;
            if (y1 > imageSize.Height) y1 = imageSize.Height;
            if (y2 > imageSize.Height) y2 = imageSize.Height;

            //Require positive width and height.
            if (x2 <= x1 || y2 <= y1) {
                //Use original dimensions - can't recover from negative width or height in cropping rectangle
                return new RectangleF(new PointF(0, 0), imageSize);
            }

            return new RectangleF((float)x1, (float)y1, (float)(x2 - x1), (float)(y2 - y1));
        }

        /// <summary>
        /// Returns 'box' clipped to be within 'bounds'
        /// </summary>
        /// <param name="box"></param>
        /// <param name="bounds"></param>
        /// <returns></returns>
        public static RectangleF ClipRectangle(RectangleF box, RectangleF bounds) {
            var topleft = new PointF(Math.Min(bounds.Right, Math.Max(box.X,bounds.X)),
                                Math.Min(bounds.Bottom, Math.Max(box.Y,bounds.Y)));
            var bottomright = new PointF(Math.Min(bounds.Right, Math.Max(box.Right,bounds.X)),
                                Math.Min(bounds.Bottom, Math.Max(box.Bottom,bounds.Y)));

            return new RectangleF(topleft,new SizeF(bottomright.X - topleft.X, bottomright.Y - topleft.Y));
        }

        /// <summary>
        /// Expands the given rectangle to be the given size while keeping it centered.
        /// </summary>
        /// <param name="box"></param>
        /// <param name="copySize"></param>
        /// <returns></returns>
        public static RectangleF ExpandTo(RectangleF box, SizeF copySize) {
            var dx = copySize.Width - box.Width;
            var dy = copySize.Height - box.Height;
            return new RectangleF(box.X - (dx / 2), box.Y - (dy / 2), copySize.Width, copySize.Height);
        }

        /// <summary>
        /// Expands the given rectangle using the specified scale multipliers, leaving it centered
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="ExpandX"></param>
        /// <param name="ExpandY"></param>
        /// <returns></returns>
        public static RectangleF ScaleRect(RectangleF rect, double ExpandX, double ExpandY) {
            var dx = (float)(rect.Width * ExpandX);
            var dy = (float)(rect.Height * ExpandY);
            return new RectangleF(rect.X - (dx / 2), rect.Y - (dy / 2), rect.Width + dx, rect.Height + dy);
        }
    }
}
