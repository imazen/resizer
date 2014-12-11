/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using ImageResizer.Util;

namespace ImageResizer.Resizing {
    public class LayoutBuilder {
        [Flags()]
        public enum PointFlags {

            /// <summary>
            /// This polygon participates in the layout phase, and reserves space when added. Affected by all batch operations. Will be returned by LastRing until a new ring is added. 
            /// </summary>
            Ring = 1,
            /// <summary>
            /// Doesn't participate in layout, takes no space, but is affected by batch operations. Will never be returned by LastRing and ignored when calculating bounding boxes.
            /// </summary>
            Invisible = 2,
            /// <summary>
            /// Completely ignored by all operations, left intact.
            /// </summary>
            Ignored = 4

        }

        public enum PointTranslationBehavior { Exact, ClosestVisiblePoint, ClosestImagePoint, Empty }


        public class PointSet {
            public PointSet(PointF[] pts, PointFlags settings) {
                this.points = pts;
                this.flags = settings;

            }
            public PointSet(PointF[] pts) {
                this.points = pts;

            }
            public PointF[] points = null;
            public PointFlags flags = PointFlags.Ring;

            protected PointTranslationBehavior pointBehavior = PointTranslationBehavior.ClosestImagePoint;
            /// <summary>
            /// How the pointsToTranslate should be translated if they are cropped out of the destination image.
            /// </summary>
            public PointTranslationBehavior PointBehavior {
                get { return pointBehavior; }
            }

        }
        /// <summary>
        /// An ordered, named collection of polygons.
        /// pointsToTranslate, imageOuterEdge, imageAreaOuterEdge.
        /// 
        /// 
        /// </summary>
        protected Dictionary<string, PointSet> ring = new Dictionary<string, PointSet>(10, StringComparer.OrdinalIgnoreCase);
        protected List<PointSet> ringList = new List<PointSet>(10);
        /// <summary>
        /// Access and set the Point[] arrays of rings by key. Case-insensitive.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public PointF[] this[string key] {
            get {
                return ring[key].points;
            }
            set {
                ring[key].points = value;
            }
        }

        /// <summary>
        /// Returns the last ring that was added. Only returns PointSets where flags = Ring
        /// </summary>
        public PointSet LastRing {
            get {
                for (int i = ringList.Count - 1; i >= 0; i--) {
                    if (ringList[i].flags == PointFlags.Ring) return ringList[i];
                }
                return null;
            }
        }


        public PointSet AddRing(string name, PointF[] points) {
            PointSet ps = new PointSet(points);
            ring.Add(name, ps);
            ringList.Add(ps);
            return ps;
        }

        public bool ContainsRing(string name) {
            return ring.ContainsKey(name);
        }

        /// <summary>
        /// Inflates the last ring using the specified padding options. Returns the resulting ring object
        /// </summary>
        /// <param name="name"></param>
        /// <param name="padding"></param>
        /// <returns></returns>
        public PointSet AddRing(string name, BoxPadding padding) {
            return AddRing(name, PolygonMath.InflatePoly(LastRing.points, padding.GetEdgeOffsets()));
        }
        /// <summary>
        /// Add points this way to see where they will occur on the destination image
        /// </summary>
        /// <param name="name"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        public PointSet AddInvisiblePolygon(string name, PointF[] points) {
            PointSet ps = new PointSet(points);
            ps.flags = PointFlags.Invisible;
            ring.Add(name, ps);
            ringList.Add(ps);
            return ps;
        }
        public PointSet AddIgnoredPoints(string name, PointF[] points) {
            PointSet ps = new PointSet(points);
            ps.flags = PointFlags.Ignored;
            ring.Add(name, ps);
            ringList.Add(ps);
            return ps;
        }

        /// <summary>
        /// Gets a bounding box that encloses all rings that don't have ExcludeFromBoundingBox set.
        /// </summary>
        /// <returns></returns>
        public RectangleF GetBoundingBox() {
            List<PointF> points = new List<PointF>(ring.Count * 5);

            foreach (PointSet val in ringList) {
                if (val.flags == PointFlags.Ring) points.AddRange(val.points);
            }
            return PolygonMath.GetBoundingBox(points.ToArray());
        }

        /// <summary>
        /// Returns a rectangle representing the given ring - if it is an axis-parallel rectangle. Otherwise returns null;
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public RectangleF? GetRingAsRectF(string name)
        {
            var poly = this[name];
            var rect = PolygonMath.GetBoundingBox(poly);
            var poly2 = PolygonMath.ToPoly(rect);
            return PolygonMath.ArraysEqual(poly, poly2) ? (RectangleF?)rect : null;
        }


        /// <summary>
        /// Rotates all existing rings (Except those flagged ignore)
        /// </summary>
        /// <param name="degrees"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public void Rotate(double degrees, PointF origin) {
            foreach (PointSet ps in ringList)
                if (ps.flags != PointFlags.Ignored) ps.points = PolygonMath.RotatePoly(ps.points, degrees, origin);
        }


        /// <summary> 
        /// Normalizes all rings and invisible polygons so that the outermost ring's bounding box starts at the specified orign.
        /// </summary>
        /// <param name="origin"></param>
        /// <returns></returns>
        public void Normalize(PointF origin) {
            PointF offset = GetBoundingBox().Location;
            offset.X *= -1;
            offset.Y *= -1;

            foreach (PointSet ps in ringList)
                if (ps.flags != PointFlags.Ignored) ps.points = PolygonMath.MovePoly(ps.points, offset);
        }

       

        public void Round() {
            foreach (PointSet ps in ringList)
                if (ps.flags != PointFlags.Ignored) ps.points = PolygonMath.RoundPoints(ps.points);
        }
        /// <summary>
        /// Scales all rings and invisible polygons by the specified factor, around the specified point.
        /// </summary>
        /// <param name="factor"></param>
        /// <param name="origin"></param>
        public void Scale(double factor, PointF origin) {
            foreach (PointSet ps in ringList)
                if (ps.flags != PointFlags.Ignored) ps.points = PolygonMath.ScalePoints(ps.points, factor,factor, origin);
        }

        /// <summary>
        /// Translates and scales all rings and invisible polygons as specified. 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public void Shift(RectangleF from, RectangleF to) {

            PointF fromOrigin = new PointF(from.X + (from.Width /2), from.Y + (from.Height /2));
            PointF toOrigin = new PointF(to.X + (to.Width /2), to.Y + (to.Height /2));
            
            PointF offset = new PointF(toOrigin.X - fromOrigin.X,toOrigin.Y - fromOrigin.Y);
            double xd = to.Width / from.Width;
            double yd = to.Height / from.Height;

            //Offset points
            foreach (PointSet ps in ringList)
                if (ps.flags != PointFlags.Ignored) ps.points = PolygonMath.MovePoly(ps.points,offset );
            //Scale points
            foreach (PointSet ps in ringList)
                if (ps.flags != PointFlags.Ignored) ps.points = PolygonMath.ScalePoints(ps.points, xd,yd, toOrigin );
        }

    }
}
