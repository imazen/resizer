using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Drawing;
using ImageResizer.Resizing;
using ImageResizer.Util;

namespace ImageResizer {
    public class ResizeSettings : NameValueCollection {
        
        public ResizeSettings() : base() { }
        public ResizeSettings(NameValueCollection col) : base(col) { }

        public int get(string name, int defaultValue){ return Utils.getInt(this,name,defaultValue);}
        public void set(string name, int value) { this[name] = value.ToString();}
        public double get(string name, double defaultValue) { return Utils.getDouble(this, name, defaultValue); }
        public void set(string name, double value) { this[name] = value.ToString(); }


        public int Width                        { get { 
            return get("width", -1);            } set {   
            set("width",value);                 } }

        public int Height                        { get { 
            return get("height", -1);            } set {   
            set("height",value);                 } }

        public int MaxWidth                        { get { 
            return get("maxwidth", -1);            } set {   
            set("maxwidth",value);                 } }

        public int MaxHeight                        { get { 
            return get("maxheight", -1);            } set {   
            set("maxheight",value);                 } }

        public bool WasOneSpecified(params string[] keys) {
            foreach (String s in keys) if (!string.IsNullOrEmpty(this[s])) return true;
            return false;
        }

        public double Rotate                        { get { 
            return get("rotate", 0);               } set {   
            set("rotate",value);                    } }

        public RotateFlipType Flip                      { get {
            return Utils.parseFlip(this["flip"]);       } set {
            this["flip"] = Utils.writeFlip(value);      }}

        public RotateFlipType SourceFlip                    { get {
            return Utils.parseFlip(this["sourceFlip"]);     } set {
            this["sourceFlip"] = Utils.writeFlip(value);    }}

        /// <summary>
        /// Whether to downscale, upscale, or allow both on images
        /// </summary>
        public ScaleMode Scale                              { get {
            return Utils.parseScale(this["scale"]);         } set {
            this["scale"] = Utils.writeScale(value);        }}

        /// <summary>
        /// Whether to preserve aspect ratio or stretch.
        /// </summary>
        public StretchMode Stretch                          { get {
            return Utils.parseStretch(this["stretch"]);      } set {
            this["stretch"] = Utils.writeStretch(value);    }}

        /// <summary>
        /// Crop settings. Defaults to None - letterboxing is used if both width and height are supplied, and stretch = proportionally.
        /// </summary>
        public CropMode CropMode                                {get {
            return Utils.parseCrop(this["crop"]).Key;           } set {
            this["crop"] = Utils.writeCrop(value, CropValues);  }}

        protected double[] CropValues {
            get {
                //Return (0,0,0,0) when null.
                double[] vals = Utils.parseCrop(this["crop"]).Value;
                return vals != null ? vals : new double[] { 0, 0, 0, 0 };
            }
            set {
                //If values are valid, CropMode.Custom will automatically be selected
                if (value != null && value.GetLength(0) == 4) {
                    Utils.writeCrop(ImageResizer.CropMode.Custom, value);
                } else {
                    //Throw an exception if an invalid value is assigned when CropMode.Custom is in use.
                    if (CropMode == ImageResizer.CropMode.Custom)
                        throw new ArgumentException("CropValues must be an array of 4 double values when CropMode.Custom is in use.");
                    //Otherwise, ignore it.
                }
            }
        }
        public PointF CropTopLeft {
            get {
                return new PointF((float)CropValues[0], (float)CropValues[1]);
            }
            set {
                CropValues = new double[] { value.X, value.Y, CropValues[2], CropValues[3] };
            }
        }

        public PointF CropBottomRight {
            get {
                return new PointF((float)CropValues[2], (float)CropValues[3]);
            }
            set {
                CropValues = new double[] {CropValues[0], CropValues[1], value.X, value.Y };
            }
        }

        public Color BackgroundColor {
            get { return Utils.parseColor(this[ "bgcolor"], Color.Transparent); }
            set { this["bgcolor"] = Utils.writeColor(value); }
        }


        public Color PaddingColor {
            get { return Utils.parseColor(this["paddingColor"], Color.Transparent); }
            set { this["paddingColor"] = Utils.writeColor(value); }
        }
        public BoxPadding Padding {
            get {
                return Utils.parsePadding(this["paddingWidth"]);
            }
            set {
                this["paddingWidth"] = Utils.writePadding(value);
            }
        }

        public BoxPadding Margin {
            get {
                return Utils.parsePadding(this["margin"]);
            }
            set {
                this["margin"] = Utils.writePadding(value);
            }
        }

        public Color BorderColor {
            get { return Utils.parseColor(this["borderColor"], Color.Transparent); }
            set { this["borderColor"] = Utils.writeColor(value); }
        }
        public BoxPadding Border {
            get {
                return Utils.parsePadding(this["borderWidth"]);
            }
            set {
                this["borderWidth"] = Utils.writePadding(value);
            }
        }

        public string Format {
            get {
                if (!string.IsNullOrEmpty(this["format"])) return this["format"];
                return this["thumbnail"];
            }
            set {
                this["format"] = value;
                this["thumbnail"] = null;
            }
        }
        

        public RectangleF getCustomCropSourceRect(SizeF imageSize) {
            double[] c = CropValues;
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
        

        public void SetDefaultImageFormat(string format) {
            if (string.IsNullOrEmpty(this["thumbnail"]) && string.IsNullOrEmpty(this["format"])) this["format"] = format;
        }
    }
}
