/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using ImageResizer.Util;

namespace ImageResizer.Resizing {
    /// <summary>
    /// Represents the widths of edges of a box.
    /// </summary>
    public class BoxPadding {

        public static BoxPadding Parse(string text, BoxPadding fallbackValue) {
            double[] coords = ParseUtils.ParseList<double>(text, 0, 1, 4);
            if (coords == null) return fallbackValue; //Failed to parse, or was empty

            if (coords.Length == 1) return new BoxPadding(coords[0]);
            if (coords.Length == 4) return new BoxPadding(coords[0], coords[1], coords[2], coords[3]);

            return fallbackValue; 
        }
        
        /// <summary>
        /// Create a box with all edges the same width.
        /// </summary>
        /// <param name="all"></param>
        public BoxPadding(double all) {
            this.all = all;
        }
        /// <summary>
        /// Create a box, specifying individual widths for each size
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        public BoxPadding(double left, double top, double right, double bottom) {
            this.top = top; this.left = left; this.bottom = bottom; this.right = right;
        }
        /// <summary>
        /// Copies the specified BoxPadding instance
        /// </summary>
        /// <param name="original"></param>
        public BoxPadding(BoxPadding original) {
            this.top = original.top;
            this.bottom = original.bottom;
            this.right = original.right;
            this.left = original.left;
        }

        protected double top = 0, left = 0, bottom =0, right = 0;

        public double Top { get { return top; } }
        public double Left { get { return left; } }
        public double Right { get { return right; } }
        public double Bottom { get { return bottom; } }

        /// <summary>
        /// Sets the width of all edges, returning a new instance
        /// </summary>
        /// <param name="all"></param>
        /// <returns></returns>
        public BoxPadding SetAll(double all) {
            BoxPadding b = new BoxPadding(this); b.all = all;
            return b;
        }
        public BoxPadding SetTop(double top) {
            BoxPadding b = new BoxPadding(this); b.top = top;
            return b;
        }
        public BoxPadding SetLeft(double left) {
            BoxPadding b = new BoxPadding(this); b.left = left;
            return b;
        }
        public BoxPadding SetRight(double right) {
            BoxPadding b = new BoxPadding(this); b.right = right;
            return b;
        }
        public BoxPadding SetBottom(double bottom) {
            BoxPadding b = new BoxPadding(this); b.bottom = bottom;
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
        public static BoxPadding Empty { get { return new BoxPadding(0); } }
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
