using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using ImageResizer.Resizing;
using System.Drawing;
using ImageResizer.Util;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.Watermark {
    public class Layer {

        public Layer() {
        }

        public Layer(NameValueCollection settings) {
            Top = DistanceUnit.TryParse(settings["top"]);
            Left = DistanceUnit.TryParse(settings["left"]);
            Bottom = DistanceUnit.TryParse(settings["bottom"]);
            Right = DistanceUnit.TryParse(settings["right"]);
            Width = DistanceUnit.TryParse(settings["width"]);
            Height = DistanceUnit.TryParse(settings["height"]);
            if (!string.IsNullOrEmpty(settings["relativeTo"])) RelativeTo = settings["relativeTo"];
            DrawAs = settings.Get( "drawAs", DrawAs);
            Align = settings.Get("align", Align);
            Fill = settings.Get("fill", false);
        }

        public virtual void CopyTo(Layer other) {
            other.Top = this.Top;
            other.Left = this.Left;
            other.Bottom = Bottom;
            other.Right = Right;
            other.Width = Width;
            other.Height = Height;
            other.RelativeTo = RelativeTo;
            other.DrawAs = DrawAs;
            other.Align = Align;
        }

        public virtual object[] GetHashBasis()
        {
            return new object[] { Top, Left, Bottom, Right, Width, Height, RelativeTo, DrawAs, Align };
        }

        public int GetDataHash()
        {
            var sb = new StringBuilder();
            foreach (object o in GetHashBasis())
            {
                if (o == null) sb.Append("null");
                else sb.Append(o.ToString());
                sb.Append("||");
            }
            return sb.ToString().GetHashCode();
        }

        private DistanceUnit _top = null;
        /// <summary>
        /// The offset from the top of the container. Percentages are relative to the container height. Defines the upper boundary for the layer. 
        /// If null, Bottom will be used to calcuate the value based on the height. If Bottom is not specified, defaults to 0.
        /// Positive values are inside the container, negative values outside it.
        /// </summary>
        public DistanceUnit Top {
            get { return _top; }
            set { _top = value; }
        }

        private DistanceUnit _left = null;
        /// <summary>
        /// The offset from the left of the container. Percentages are relative to the container width. Defines the leftmost boundary for the layer. 
        /// If null, Right will be used to calcuate the value based on the width. If Right is not specified, defaults to 0.
        /// Positive values are inside the container, negative values outside it.
        /// </summary>
        public DistanceUnit Left {
            get { return _left; }
            set { _left = value; }
        }

        private DistanceUnit _right = null;

        /// <summary>
        /// The offset relative to the right side of the container. Percentages are relative to the container width. Defines the rightmost boundary for the layer.
        /// If null, Top will be used to calcuate the value based on the height. If Top is not specified, defaults to 0.
        /// Positive values are inside the container, negative values outside it.
        /// </summary>
        public DistanceUnit Right {
            get { return _right; }
            set { _right = value; }
        }
        private DistanceUnit _bottom = null;
        /// <summary>
        /// The offset relative to the bottom of the container. Percentages are relative to the container height. Defines the bottom-most boundary for the layer.
        /// If null, Top will be used to calcuate the value based on the height. If Top is not specified, defaults to 0.
        /// Positive values are inside the container, negative values outside it.
        /// </summary>
        public DistanceUnit Bottom {
            get { return _bottom; }
            set { _bottom = value; }
        }
        private DistanceUnit _width = null;
        /// <summary>
        /// The width of the layer. If used with both Left and Right, the smaller result wins. I.e, with a 100px container, width=50, left=30, right=30, the resulting width will be 40.
        /// If null, Left and Right will be used to calcuate the value. If both Left and Right are not specified, the natural width of the layer's contents will be used.
        /// Percentages are relative to the container width. 
        /// </summary>
        public DistanceUnit Width {
            get { return _width; }
            set { _width = value; }
        }
        private DistanceUnit _height = null;

        /// <summary>
        /// The height of the layer. If used with both Top and Bottom, the smaller result wins. I.e, with a 100px container, height=50, top=30, top=30, the resulting height will be 40.
        /// If null, Top and Bottom will be used to calcuate the value. If both Top and Bottom are not specified, the natural height of the layer's contents will be used.
        /// Percentages are relative to the container height. 
        /// </summary>
        public DistanceUnit Height {
            get { return _height; }
            set { _height = value; }
        }

        private string _relativeTo = "image";

        /// <summary>
        /// Specifies the container that the position values (top,left,right,bottom,width,heght) are relative to. 
        /// The default is 'image' (the innermost square, which contains the original photo). Additional valid values include 'imageArea' (includes whitespace added to preserve aspect ratio), 'padding', 'border', 'margin', and 'canvas'. 
        /// </summary>
        public string RelativeTo {
            get { return _relativeTo; }
            set { _relativeTo = value; }
        }
        //top, left, bottom, right = px or percentages (relative to container)
        //relativeTo = image|imageArea|padding|border|margin|canvas
        //drawAs overlay|background

        private ContentAlignment _align = ContentAlignment.MiddleCenter;

        /// <summary>
        /// The alignment to use when 
        /// (a) all 3 horizontal or vertical values are specified, and they need to be resolved,
        /// (b) when only width/height are specified, 
        /// (c) when no positioning values are specified, or 
        /// (d) when the content doesn't precisely fill they layer bounds.
        /// </summary>
        public ContentAlignment Align {
            get { return _align; }
            set { _align = value; }
        }


        private bool _fill = false;
        /// <summary>
        /// (defaults false). When true, the image or text will attempt to fill 1 of the layer's bounds, even if upscaling is required. 
        /// When Width is not specified, and both left and right are not specififed, this causes the image to fill the container width (if possible).
        /// When Height is not specified, and both top and bottom are not specififed, this causes the image to fill the container height (if possible).
        /// This causes &amp;scale=both to be used on images unless another setting is specified in imageQuery.
        /// </summary>
        public bool Fill {
            get { return _fill; }
            set { _fill = value; }
        }

        public enum LayerPlacement { Overlay, Background }

        private LayerPlacement _drawAs = LayerPlacement.Overlay;
        /// <summary>
        /// The z-order at which to draw the layer. Curret options are Overlay (over everything) and Background (over the background color).
        /// </summary>
        public LayerPlacement DrawAs {
            get { return _drawAs; }
            set { _drawAs = value; }
        }


        public delegate Size CalculateLayerContentSize(double maxwidth, double maxheight);
        /// <summary>
        /// Returns a rectangle with canvas-relative coordinates. A callback is required to calculate the actual size of the content based on the specified bounds. 
        /// The callback may be passed double.NaN for one or more paramters to indicate that they are not specified.
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="actualSizeCalculator"></param>
        /// <param name="forceInsideCanvas"></param>
        /// <returns></returns>
        public RectangleF CalculateLayerCoordinates(ImageState s, CalculateLayerContentSize actualSizeCalculator, bool forceInsideCanvas) {
            //Find container 
            RectangleF cont;
            if (s.layout.ContainsRing(RelativeTo))
                cont = PolygonMath.GetBoundingBox(s.layout[RelativeTo]);
            else if ("canvas".Equals(RelativeTo, StringComparison.OrdinalIgnoreCase)) 
                cont = new RectangleF(new PointF(), s.destSize);
            else 
                cont = PolygonMath.GetBoundingBox(s.layout["image"]);
            
            //Calculate layer coords
            RectangleF rect = new RectangleF();

            //Resolve all values to the same coordinate plane, null values will be transformed to NaN
            double left = Resolve(Left, cont.X, cont.Width, false);
            double top = Resolve(Top, cont.Y, cont.Height, false);
            double right = Resolve(Right, cont.Right, cont.Width, true);
            double bottom = Resolve(Bottom, cont.Bottom, cont.Height, true);
            double width = Resolve(Width, 0, cont.Width, false);
            double height = Resolve(Height, 0, cont.Height, false);

            //Force all values to be within the canvas area.
            if (forceInsideCanvas) {
                SizeF canvas = s.destSize;
                if (!double.IsNaN(left)) left = Math.Min(Math.Max(0, left), canvas.Width);
                if (!double.IsNaN(right)) right = Math.Min(Math.Max(0, right), canvas.Width);
                if (!double.IsNaN(width)) width = Math.Min(Math.Max(0, width), canvas.Width);
                if (!double.IsNaN(bottom)) bottom = Math.Min(Math.Max(0, bottom), canvas.Height);
                if (!double.IsNaN(top)) top = Math.Min(Math.Max(0, top), canvas.Height);
                if (!double.IsNaN(height)) height = Math.Min(Math.Max(0, height), canvas.Height);
            }

            //If right and left (or top and bottom) are inverted, avg them and set them equal.
            if (!double.IsNaN(left) && !double.IsNaN(right) && right < left) left = right = ((left + right) / 2);
            if (!double.IsNaN(top) && !double.IsNaN(bottom) && bottom < top) bottom = top = ((bottom + top) / 2);


            //Fill in width/height if enough stuff is specified
            if (!double.IsNaN(left) && !double.IsNaN(right) && double.IsNaN(width))  width = Math.Max(right - left,0);
            if (!double.IsNaN(top) && !double.IsNaN(bottom) && double.IsNaN(height)) height = Math.Max(bottom - top,0);

  
            //Execute the callback to get the actual size. Update the width and height values if the actual size is smaller. 
            SizeF normalSize = actualSizeCalculator((double.IsNaN(width) && Fill) ? cont.Width : width, (double.IsNaN(height) && Fill) ? cont.Height : height);
            if (double.IsNaN(width) || width > normalSize.Width) width = normalSize.Width;
            if (double.IsNaN(height) || height > normalSize.Height) height = normalSize.Height;



            //If only width and height are specified, set the other values to match the container, and let alignment sort it out.
            if (double.IsNaN(left) && double.IsNaN(right)) { left = cont.X; right = cont.Right; }//Handle situations where neither left nor right is specified, pretend left=0
            if (double.IsNaN(top) && double.IsNaN(bottom)) { top = cont.X; bottom = cont.Bottom; } //Handle situations where neither top nor bottom is specified, pretend top=0


            //When all 3 values are specified in either direction, we must use the alignment setting to determine which direction to snap to.
            if (!double.IsNaN(left) && !double.IsNaN(right) && !double.IsNaN(width)) {
                if (width > right - left) width = right - left; //Use the smaller value in this case, no need to align.
                else {
                    if (Align == ContentAlignment.BottomLeft || Align == ContentAlignment.MiddleLeft || Align == ContentAlignment.TopLeft)
                        right = left + width;
                    if (Align == ContentAlignment.BottomCenter || Align == ContentAlignment.MiddleCenter || Align == ContentAlignment.TopCenter){
                        left += (right-left-width) /2;
                        right = left + width;
                    }
                    if (Align == ContentAlignment.BottomRight || Align == ContentAlignment.MiddleRight || Align == ContentAlignment.TopRight)
                        left = right - width;
                }
            }

            //When all 3 values are specified in either direction, we must use the alignment setting to determine which direction to snap to.
            if (!double.IsNaN(top) && !double.IsNaN(bottom) && !double.IsNaN(height)) {
                if (height > bottom - top) height = bottom - top; //Use the smaller value in this case, no need to align.
                else {
                    if (Align == ContentAlignment.TopLeft || Align == ContentAlignment.TopCenter || Align == ContentAlignment.TopRight)
                        bottom = top + height;
                    if (Align == ContentAlignment.MiddleLeft || Align == ContentAlignment.MiddleCenter || Align == ContentAlignment.MiddleRight) {
                        top += (bottom - top - height) / 2;
                        bottom = top + height;
                    }
                    if (Align == ContentAlignment.BottomLeft || Align == ContentAlignment.BottomCenter || Align == ContentAlignment.BottomRight)
                        top = bottom - height;
                }
            }


            //Calculate values for top and left based off bottom and right
            if (double.IsNaN(left)) left = right - width;
            if (double.IsNaN(top)) top = bottom - height;

            //Calculate values for bottom and right based off top and left
            if (double.IsNaN(right)) right = left + width;
            if (double.IsNaN(bottom)) bottom = top + height;


            return new RectangleF((float)left, (float)top, (float)width, (float)height);

        }

        public double Resolve(DistanceUnit value, double relativeToValue, double percentRelativeTo, bool invert) {
            if (value == null) return double.NaN;
            double val = value.Value;
            if (value.Type == DistanceUnit.Units.Percentage) val *= percentRelativeTo / 100;
            if (invert) val = relativeToValue - val;
            else val = relativeToValue + val;
            return val;
        }

        public virtual void RenderTo(ImageState s) { }
    }
}
