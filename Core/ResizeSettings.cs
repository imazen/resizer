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
        
        /// <summary>
        /// Creates an empty settings collection. 
        /// </summary>
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
        public ResizeSettings(string queryString) : base(PathUtils.ParseQueryStringFriendlyAllowSemicolons(queryString)) { }

        /// <summary>
        /// Creates a new resize settings object with the specified resizing settings
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="mode"></param>
        /// <param name="imageFormat">The desired image format, like 'jpg', 'gif', or 'png'. Leave null if you want to preserve the original format.</param>
        public ResizeSettings(int width, int height, FitMode mode, string imageFormat) {
            this.Width = width;
            this.Height = height;
            this.Mode = mode;
            if (imageFormat != null) this.Format = imageFormat;
        }


        protected int get(string name, int defaultValue){ return Utils.getInt(this,name,defaultValue);}
        protected void set(string name, int value) { this[name] = value.ToString(); }

        protected double get(string name, double defaultValue) { return Utils.getDouble(this, name, defaultValue); }
        protected void set(string name, double value) { this[name] = value.ToString(); }

        /// <summary>
        /// ["width"]: Sets the desired width of the image. (minus padding, borders, margins, effects, and rotation). 
        /// The only instance the resulting image will be smaller is if the original source image is smaller. 
        /// Set Scale=Both to upscale these images and ensure the output always matches 'width' and 'height'. 
        /// If both width and height are specified, the image will be 'letterboxed' to match the desired aspect ratio. 
        /// Use maxwidth/maxheight, crop=auto, or stretch=fill to avoid this behavior.
        /// </summary>
        public int Width                        { get {
                return get("width", get("w", -1)); }set {
                set("width", value); this.Remove("w");
            }
        }

        /// <summary>
        /// ["height"]: Sets the desired height of the image.  (minus padding, borders, margins, effects, and rotation)
        /// The only instance the resulting image will be smaller is if the original source image is smaller. 
        /// Set Scale=Both to upscale these images and ensure the output always matches 'width' and 'height'. 
        /// If both width and height are specified, the image will be 'letterboxed' to match the desired aspect ratio. 
        /// Use maxwidth/maxheight, crop=auto, or stretch=fill to avoid this behavior.
        /// </summary>
        public int Height                        { get { 
            return get("height", get("h",-1));            } set {
                set("height", value); this.Remove("height");
            }
        }

        /// <summary>
        /// ["maxwidth"]: Sets the maximum desired width of the image.  (minus padding, borders, margins, effects, and rotation). 
        /// The image may be smaller than this value to maintain aspect ratio when both maxwidth and maxheight are specified.
        /// </summary>
        public int MaxWidth                        { get { 
            return get("maxwidth", -1);            } set {   
            set("maxwidth",value);                 } }


        /// <summary>
        /// ["quality"]: The jpeg encoding quality to use. (10..100). 90 is the default and best value, you should leave it.
        /// </summary>
        public int Quality {
            get {
                return get("quality", 90);
            }
            set {
                set("quality", value);
            }
        }


        /// <summary>
        /// ["maxheight"]: Sets the maximum desired height of the image.  (minus padding, borders, margins, effects, and rotation). 
        /// The image may be smaller than this value to maintain aspect ratio when both maxwidth and maxheight are specified.
        /// </summary>
        public int MaxHeight                        { get { 
            return get("maxheight", -1);            } set {   
            set("maxheight",value);                 } }

        /// <summary>
        /// ["mode"]: Sets the fit mode for the image. max, min, pad, crop, carve, stretch
        /// </summary>
        public FitMode Mode{                        get {
            return Utils.parseEnum<FitMode>(this["mode"], FitMode.None);       } set {
            this["mode"] = value.ToString();      }}

        /// <summary>
        /// Returns true if any of the specified keys are present in this NameValueCollection
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public bool WasOneSpecified(params string[] keys) {
            foreach (String s in keys) if (!string.IsNullOrEmpty(this[s])) return true;
            return false;
        }
        /// <summary>
        /// ["rotate"] The degress to rotate the image clockwise. -360 to 360.
        /// </summary>
        public double Rotate                        { get { 
            return get("rotate", 0.0d);               } set {   
            set("rotate",value);                    } }


        /// <summary>
        /// How to anchor the image when cropping or adding whitespace to meet sizing requirements.
        /// </summary>
        public ContentAlignment Anchor                   { get {
            return Utils.parseEnum<ContentAlignment>(this["anchor"],ContentAlignment.MiddleCenter);       } set {
            this["anchor"] = value.ToString();      }}



        /// <summary>
        /// Allows you to flip the entire resulting image vertically, horizontally, or both. Rotation is not supported.
        /// </summary>
        public RotateFlipType Flip                      { get {
            return Utils.parseFlip(this["flip"]);       } set {
            this["flip"] = Utils.writeFlip(value);      }}

        /// <summary>
        /// ["sFlip"] Allows you to flip the source image vertically, horizontally, or both. Rotation is not supported.
        /// </summary>
        public RotateFlipType SourceFlip                    { get {
                return Utils.parseFlip(string.IsNullOrEmpty(this["sFlip"]) ? this["sourceFlip"] : this["sFlip"]);
            }
            set {
            this["sFlip"] = Utils.writeFlip(value);    }}

        /// <summary>
        /// ["scale"] Whether to downscale, upscale, upscale the canvas, or both upscale or downscale the image as needed. Defaults to
        /// DownscaleOnly when maxwidth/maxheight is used, and Both when width/height are used.
        /// </summary>
        public ScaleMode Scale                              { get {
            return Utils.parseScale(this["scale"]);         } set {
            this["scale"] = Utils.writeScale(value);        }}

        /// <summary>
        /// [Depreciated] (Replaced by mode=stretch) Whether to preserve aspect ratio or stretch to fill the bounds.
        /// </summary>
        [Obsolete("Replaced by Mode=Stretch")]
        public StretchMode Stretch                          { get {
            return Utils.parseStretch(this["stretch"]);      } set {
            this["stretch"] = Utils.writeStretch(value);    }}


        /// <summary>
        /// ["cache"]: Server caching mode suggestion for the result
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
        /// ["process"]: Server processing suggestion for the result. Allows you to 'disable' processing of the image (so you can use disk caching with non-image files). Allows you to 'force' processing of the image, for images without a querystring.
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
        /// ["crop"]=none|auto Defaults to None - letterboxing is used if both width and height are supplied, and stretch = proportionally.
        /// Set CropTopLeft and CropBottomRight when you need to specify a custom crop rectangle.
        /// </summary>
        [Obsolete("Replaced by Mode=Crop. Use CropTopLeft and CropTopRight instead for setting a custom crop mode.")]
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
                if (value != null && (value.Length == 4)) {
                    this["crop"] =  Utils.writeCrop(ImageResizer.CropMode.Custom, value);
                } else {
                    //Throw an exception if an invalid value is assigned when CropMode.Custom is in use.
                    throw new ArgumentException("CropValues must be an array of 4 double values.");
                    //Otherwise, ignore it.
                }
            }
        }

        /// <summary>
        /// ["crop"]=([x1],[y1],x2,y2). Sets x1 and y21, the top-right corner of the crop rectangle. If 0 or greater, the coordinate is relative to the top-left corner of the image.
        /// If less than 0, the value is relative to the bottom-right corner. This allows for easy trimming: crop=(10,10,-10,-10).
        /// Set ["cropxunits"] and ["cropyunits"] to the width/height of the rectangle your coordinates are relative to, if different from the original image size.
        /// </summary>
        public PointF CropTopLeft {
            get {
                return new PointF((float)CropValues[0], (float)CropValues[1]);
            }
            set {
                CropValues = new double[] { value.X, value.Y, CropValues[2], CropValues[3] };
            }
        }

        /// <summary>
        /// ["crop"]=(x1,y1,[x2],[y2]). Sets x2 and y2, the bottom-right corner of the crop rectangle. If 1 or greater, the coordinate is relative to the top-left corner of the image.
        /// If 0 or less, the value is relative to the bottom-right corner. This allows for easy trimming: crop=(10,10,-10,-10).
        /// Set ["cropxunits"] and ["cropyunits"] to the width/height of the rectangle your coordinates are relative to, if different from the original image size.
        /// </summary>
        public PointF CropBottomRight {
            get {
                return new PointF((float)CropValues[2], (float)CropValues[3]);
            }
            set {
                CropValues = new double[] { CropValues[0], CropValues[1], value.X, value.Y };
            }
        }

        /// <summary>
        /// ["bgcolor"]: Named and hex values are supported. (rgb and rgba, both 3, 6, and 8 digits).
        /// </summary>
        public Color BackgroundColor {
            get { return Utils.parseColor(this[ "bgcolor"], Color.Transparent); }
            set { this["bgcolor"] = Utils.writeColor(value); }
        }

        /// <summary>
        /// Gets/sets ["paddingColor"]. Named and hex values are supported. (rgb and rgba, both 3, 6, and 8 digits).
        /// </summary>
        public Color PaddingColor {
            get { return Utils.parseColor(this["paddingColor"], Color.Transparent); }
            set { this["paddingColor"] = Utils.writeColor(value); }
        }
        /// <summary>
        /// ["paddingWidth"]: Gets/sets the width(s) of padding inside the image border.
        /// </summary>
        public BoxPadding Padding {
            get {
                return Utils.parsePadding(this["paddingWidth"]);
            }
            set {
                this["paddingWidth"] = Utils.writePadding(value);
            }
        }
        /// <summary>
        /// ["margin"]: Gets/sets the width(s) of the margin outside the image border and effects.
        /// </summary>
        public BoxPadding Margin {
            get {
                return Utils.parsePadding(this["margin"]);
            }
            set {
                this["margin"] = Utils.writePadding(value);
            }
        }
        /// <summary>
        /// Gets/sets ["borderColor"]. Named and hex values are supported. (rgb and rgba, both 3, 6, and 8 digits).
        /// </summary>
        public Color BorderColor {
            get { return Utils.parseColor(this["borderColor"], Color.Transparent); }
            set { this["borderColor"] = Utils.writeColor(value); }
        }
        /// <summary>
        /// Friendly get/set accessor for the ["borderWidth"] value. Returns BoxPadding.Empty when unspecified.
        /// </summary>
        public BoxPadding Border {
            get {
                return Utils.parsePadding(this["borderWidth"]);
            }
            set {
                this["borderWidth"] = Utils.writePadding(value);
            }
        }

        /// <summary>
        /// Like this["format"]. 
        /// Gets or sets the output file format to use. "png", "jpg", and "gif" are valid values.
        /// Returns null if unspecified. When format is not specified, the original format of the image is used (unless it is not a web safe format  - jpeg is the fallback in that scenario).
        /// <remarks>Also checks the 'thumbnail' value for V2 compatibility. When set, 'thumnail' is removed and only 'format' is used.
        /// </remarks>
        /// </summary>
        public string Format {
            get {
                if (!string.IsNullOrEmpty(this["format"])) return this["format"];
                return this["thumbnail"];
            }
            set {
                this["format"] = value;
                if (this["thumbnail"] != null) Remove("thumbnail"); 
            }
        }


        public double CropXUnits { get { return Utils.parseCropUnits(this["cropxunits"]).Value; } set { this["cropxunits"] = value == default(double) ? "sourcepixels" : value.ToString(); } }
        public double CropYUnits { get { return Utils.parseCropUnits(this["cropyunits"]).Value; } set { this["cropyunits"] = value == default(double) ? "sourcepixels" : value.ToString(); } }


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
        
        /// <summary>
        /// If 'thumbnail' and 'format' are not specified, sets 'format' to the specified value.
        /// </summary>
        /// <param name="format"></param>
        public void SetDefaultImageFormat(string format) {
            if (string.IsNullOrEmpty(this["thumbnail"]) && string.IsNullOrEmpty(this["format"])) this["format"] = format;
        }

        /// <summary>
        /// Returns a string containing all the settings in the class, in querystring form. Use ToStringEncoded() to get a URL-safe querystring. 
        /// This method does not encode commas, spaces, etc.
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return PathUtils.BuildQueryString(this,false);
        }
        /// <summary>
        /// Returns a querystring with all the settings in this class. Querystring keys and values are URL encoded properly.
        /// </summary>
        /// <returns></returns>
        public  string ToStringEncoded() {
            return PathUtils.BuildQueryString(this);
        }
    }
}
