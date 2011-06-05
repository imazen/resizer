/* Copyright (c) 2011 Nathanael Jones. See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Drawing;
using ImageResizer.Resizing;
using ImageResizer.Util;

namespace ImageResizer {
    /// <summary>
    /// Represents the settings which will be used to process the image. 
    /// Extends NameValueCollection to provide friendly property names for commonly used settings.
    /// </summary>
    [Serializable]
    public class ResizeSettings : NameValueCollection {
        
        public ResizeSettings() : base() { }
        /// <summary>
        /// Copies the specified collection into a new ResizeSettings instance.
        /// </summary>
        /// <param name="col"></param>
        public ResizeSettings(NameValueCollection col) : base(col) { }
        /// <summary>
        /// Parses the specified querystring into name/value pairs. leading ? not required.
        /// </summary>
        /// <param name="queryString"></param>
        public ResizeSettings(string queryString) : base(PathUtils.ParseQueryStringFriendly(queryString)) { }


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
        /// Server caching mode suggestion for the result
        /// </summary>
        public ServerCacheMode Cache {
            get {
                return Utils.parseEnum<ServerCacheMode>(this["cache"],ServerCacheMode.Default);
            }
            set {
                this["cache"] = value.ToString();
            }
        }

        /// <summary>
        /// Server caching mode suggestion for the result
        /// </summary>
        public ProcessWhen Process {
            get {
                return Utils.parseEnum<ProcessWhen>(this["process"], this["useresizingpipeline"] != null ? ProcessWhen.Always : ProcessWhen.Default);
            }
            set {
                this["process"] = value.ToString(); this.Remove("useresizingpipeline");
            }
        }

        /// <summary>
        /// Crop settings. Defaults to None - letterboxing is used if both width and height are supplied, and stretch = proportionally.
        /// </summary>
        public CropMode CropMode                                {get {
            return Utils.parseCrop(this["crop"]).Key;           } set {
            this["crop"] = Utils.writeCrop(value, CropValues);  }}

        /// <summary>
        /// 4 values specify x1,y1,x2,y2 values for the crop rectangle.
        /// Negative values are relative to the bottom right - on a 100x100 picture, (10,10,90,90) is equivalent to (10,10,-10,-10). And (0,0,0,0) is equivalent to (0,0,100,100).
        /// </summary>
        protected double[] CropValues {
            get {
                //Return (0,0,0,0) when null.
                double[] vals = Utils.parseCrop(this["crop"]).Value;
                return vals != null ? vals : new double[] { 0, 0, 0, 0 };
            }
            set {
                //If values are valid, CropMode.Custom will automatically be selected
                if (value != null && (value.GetLength(0) == 4)) {
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
                CropValues = new double[] { CropValues[0], CropValues[1], value.X, value.Y };
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
            RectangleF defValue = new RectangleF(new PointF(0, 0), imageSize);
            double[] c = CropValues;

            //Step 1, parse units.
            KeyValuePair<CropUnits, double> xunits = Utils.parseCropUnits(this["cropxunits"]);
            KeyValuePair<CropUnits, double> yunits = Utils.parseCropUnits(this["cropyunits"]);

            //Step 2, Apply units to values, resolving against imageSize
            for (int i = 0; i < c.Length; i++){
                bool xvalue = i % 2 == 0;
                if (xvalue && xunits.Key == CropUnits.Custom) c[i] *= (imageSize.Width / xunits.Value);
                if (!xvalue && yunits.Key == CropUnits.Custom) c[i] *= (imageSize.Height / yunits.Value);

                //Prohibit values larger than imageSize
                if (xvalue && c[i] > imageSize.Width) c[i] = imageSize.Width;
                if (!xvalue && c[i] > imageSize.Height) c[i] = imageSize.Height;
            }

            //Step 3, expand width/height crop to 4-value crop (not currently used)
            if (c.Length == 2) {
                if (c[0] < 1 || c[1] < 1) return defValue; //We can't do anything with negative values here
                //Center horizontally and vertically.
                double x = (imageSize.Width - c[0]) /2;
                double y= (imageSize.Height - c[1]) /2;

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
        

        public void SetDefaultImageFormat(string format) {
            if (string.IsNullOrEmpty(this["thumbnail"]) && string.IsNullOrEmpty(this["format"])) this["format"] = format;
        }
    }
}
