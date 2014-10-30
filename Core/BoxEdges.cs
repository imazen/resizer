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
            this.All = all;
        }
        /// <summary>
        /// Create a box, specifying individual widths for each size
        /// </summary>
        /// <param name="left"></param>
        /// <param name="top"></param>
        /// <param name="right"></param>
        /// <param name="bottom"></param>
        public BoxEdges(double left, double top, double right, double bottom) {
            Top = top;
            Left = left;
            Right = right;
            Bottom = bottom;
        }
        /// <summary>
        /// Copies the specified BoxEdges instance
        /// </summary>
        /// <param name="original"></param>
        public BoxEdges(BoxEdges original) {
            Top = original.Top;
            Bottom = original.Bottom;
            Right = original.Right;
            Left = original.Left;
        }

        public double Top { get; private set; }
        public double Left { get; private set; }
        public double Right { get; private set; }
        public double Bottom { get; private set; }

        /// <summary>
        /// Sets the width of all edges, returning a new instance
        /// </summary>
        /// <param name="all"></param>
        /// <returns></returns>
        public BoxEdges SetAll(double all) {
            var b = new BoxEdges(this); b.All = all;
            return b;
        }
        public BoxEdges SetTop(double top) {
            var b = new BoxEdges(this); b.Top = top;
            return b;
        }
        public BoxEdges SetLeft(double left) {
            var b = new BoxEdges(this); b.Left = left;
            return b;
        }
        public BoxEdges SetRight(double right) {
            var b = new BoxEdges(this); b.Right = right;
            return b;
        }
        public BoxEdges SetBottom(double bottom) {
            var b = new BoxEdges(this); b.Bottom = bottom;
            return b;
        }

        /// <summary>
        /// Returns double.NaN unless all edges are the same width, in which case that width is returned
        /// </summary>
        public double All { 
            get { 
                if (Top == Left && Left == Bottom && Bottom == Right) return Top;

                return double.NaN;
            }
            private set{
                Top = Left = Bottom = Right = value;
            }
        }

        
        /// <summary>
        /// Returns an instance with a width of 0
        /// </summary>
        public static BoxEdges Empty { get { return new BoxEdges(0); } }
        /// <summary>
        /// Returns true if th
        /// </summary>
        public bool IsEmpty { get { return All == 0; } }
        /// <summary>
        /// Gets edge offsets as a clockwise array, starting with Top.
        /// </summary>
        /// <returns></returns>
        public float[] GetEdgeOffsets() {
            return new[]{ (float)Top, (float)Right, (float)Bottom, (float)Left };
        }

        public override string ToString() {
            if (!double.IsNaN(All)) return All.ToString(NumberFormatInfo.InvariantInfo); //Easy

            return "(" + Left.ToString(NumberFormatInfo.InvariantInfo) + "," + Top.ToString(NumberFormatInfo.InvariantInfo) + "," +
                Right.ToString(NumberFormatInfo.InvariantInfo) + "," + Bottom.ToString(NumberFormatInfo.InvariantInfo) + ")";
        }

    }
}
