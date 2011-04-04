using System;
using System.Collections.Generic;
using System.Text;

namespace ImageResizer.Resizing {
    public class BoxPadding {
        public BoxPadding(BoxPadding original) {
            this.top = original.top;
            this.bottom = original.bottom;
            this.right = original.right;
            this.left = original.left;
        }
        public BoxPadding(double all) {
            this.all = all;
        }
        public BoxPadding(double left, double top, double right, double bottom) {
            this.top = top; this.left = left; this.bottom = bottom; this.right = right;
        }

        protected double top = 0, left = 0, bottom =0, right = 0;

        public double Top { get { return top; } }
        public double Left { get { return left; } }
        public double Right { get { return right; } }
        public double Bottom { get { return bottom; } }

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

        public double All { get { return this.all; } }

        protected double all {
            get {
                if (top == left && left == bottom && bottom == right) return top;
                else return -1;
            }
            set {
                top = left = bottom = right = value;
            }
        }

        public static BoxPadding Empty { get { return new BoxPadding(0); } }
        public bool IsEmpty { get { return all == 0; } }
        /// <summary>
        /// Gets edge offsets as a clockwise array, starting with Top.
        /// </summary>
        /// <returns></returns>
        public float[] GetEdgeOffsets() {
            return new float[4] { (float)top, (float)right, (float)bottom, (float)left };
        }
    }
}
