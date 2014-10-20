using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Util;
using System.Globalization;

namespace ImageResizer {
    /// <summary>
    /// Represents the widths of edges of a box.
    /// </summary>
    public class BoxEdges {

        public static BoxEdges Parse(string text, BoxEdges fallbackValue) {
            double[] coords = ParseUtils.ParseList<double>(text, 0, 1, 4);
            if (coords == null) return fallbackValue; //Failed to parse, or was empty

            if (coords.Length == 1) return new BoxEdges(coords[0]);
            if (coords.Length == 4) return new BoxEdges(coords[0], coords[1], coords[2], coords[3]);

            return fallbackValue;
        }

        /// <summary>
        /// Create a box with all edges the same width.
        /// </summary>
        /// <param name="all"></param>
        public BoxEdges(double all) {
            this.all = all;
        }
        /// <summary>
        /// Create a box, specifying individual widths for each size
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        public BoxEdges(double left, double top, double right, double bottom) {
            this.top = top; this.left = left; this.bottom = bottom; this.right = right;
        }
        /// <summary>
        /// Copies the specified BoxEdges instance
        /// </summary>
        /// <param name="original"></param>
        public BoxEdges(BoxEdges original) {
            this.top = original.top;
            this.bottom = original.bottom;
            this.right = original.right;
            this.left = original.left;
        }

        protected double top = 0, left = 0, bottom = 0, right = 0;

        public double Top { get { return top; } }
        public double Left { get { return left; } }
        public double Right { get { return right; } }
        public double Bottom { get { return bottom; } }

        /// <summary>
        /// Sets the width of all edges, returning a new instance
        /// </summary>
        /// <param name="all"></param>
        /// <returns></returns>
        public BoxEdges SetAll(double all) {
            BoxEdges b = new BoxEdges(this); b.all = all;
            return b;
        }
        public BoxEdges SetTop(double top) {
            BoxEdges b = new BoxEdges(this); b.top = top;
            return b;
        }
        public BoxEdges SetLeft(double left) {
            BoxEdges b = new BoxEdges(this); b.left = left;
            return b;
        }
        public BoxEdges SetRight(double right) {
            BoxEdges b = new BoxEdges(this); b.right = right;
            return b;
        }
        public BoxEdges SetBottom(double bottom) {
            BoxEdges b = new BoxEdges(this); b.bottom = bottom;
            return b;
        }

        /// <summary>
        /// Returns double.NaN unless all edges are the same width, in which case that width is returned
        /// </summary>
        public double All { get { return this.all; } }

        protected double all {
            get {
                if (top == left && left == bottom && bottom == right) return top;
                else return double.NaN;
            }
            set {
                top = left = bottom = right = value;
            }
        }
        /// <summary>
        /// Returns an instance with a width of 0
        /// </summary>
        public static BoxEdges Empty { get { return new BoxEdges(0); } }
        /// <summary>
        /// Returns true if th
        /// </summary>
        public bool IsEmpty { get { return all == 0; } }
        /// <summary>
        /// Gets edge offsets as a clockwise array, starting with Top.
        /// </summary>
        /// <returns></returns>
        public float[] GetEdgeOffsets() {
            return new float[4] { (float)top, (float)right, (float)bottom, (float)left };
        }

        public override string ToString() {
            if (!double.IsNaN(All)) return All.ToString(NumberFormatInfo.InvariantInfo); //Easy

            return "(" + Left.ToString(NumberFormatInfo.InvariantInfo) + "," + Top.ToString(NumberFormatInfo.InvariantInfo) + "," +
                Right.ToString(NumberFormatInfo.InvariantInfo) + "," + Bottom.ToString(NumberFormatInfo.InvariantInfo) + ")";
        }

    }
}
