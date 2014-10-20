/* Copyright (c) 2014 Imazen See license.txt */
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Drawing;
using ImageResizer.Resizing;
using ImageResizer.Util;
using System.Globalization;
using ImageResizer.ExtensionMethods;
using ImageResizer.Collections;

namespace ImageResizer {
    /// <summary>
    /// Represents the settings which will be used to process the image. 
    /// Extends NameValueCollection to provide friendly property names for commonly used settings.
    /// Replaced by the Instructions class. Will be removed in V4.0
    /// </summary>
    [Serializable]
    public class ResizeSettings : QuerystringBase<ResizeSettings> {
        
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
        /// Discards everything after the first '#' character as a URL fragment.
        /// </summary>
        /// <param name="queryString"></param>
        public ResizeSettings(string queryString) : base(PathUtils.ParseQueryStringFriendlyAllowSemicolons(queryString)) { }
        /// <summary>
        /// Merges the specified collection with a set of defaults into a new
        /// ResizeSettings instance.
        /// </summary>
        /// <param name="col"></param>
        /// <param name="defaultSettings"></param>
        public ResizeSettings(NameValueCollection col, NameValueCollection defaultSettings)
            : base(col.MergeDefaults(defaultSettings)) { }
        /// <summary>
        /// Parses the specified querystring into name/value pairs and merges
        /// it with defaultSettings in a new ResizeSettings instance.
        /// </summary>
        /// <param name="queryString"></param>
        /// <param name="defaultSettings"></param>
        public ResizeSettings(string queryString, NameValueCollection defaultSettings)
            : this(PathUtils.ParseQueryStringFriendlyAllowSemicolons(queryString), defaultSettings) { }

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


        protected int get(string name, int defaultValue){ return this.Get<int>(name,defaultValue);}
        protected void set(string name, int value) { this.Set<int>(name, value); }

        protected double get(string name, double defaultValue) { return this.Get<double>(name, defaultValue); }
        protected void set(string name, double value) { this.Set<double>(name, value); }

        /// <summary>
        /// ["width"]: Sets the desired width of the image. (minus padding, borders, margins, effects, and rotation). 
        /// The only instance the resulting image will be smaller is if the original source image is smaller. 
        /// Set Scale=Both to upscale these images and ensure the output always matches 'width' and 'height'. 
        /// If both width and height are specified, the image will be 'letterboxed' to match the desired aspect ratio. 
        /// Change the Mode property to adjust this behavior.
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
        /// Change the Mode property to adjust this behavior.
        /// </summary>
        public int Height                        { get { 
            return get("height", get("h",-1));            } set {
                set("height", value); this.Remove("h");
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
        public FitMode Mode {
            get {
                return this.Get<FitMode>("mode", FitMode.None);
            }
            set {
                this.Set<FitMode>("mode", value);
            }
        }

        /// <summary>
        /// Returns true if any of the specified keys are present in this NameValueCollection
        /// </summary>
        /// <param name="keys"></param>
        /// <returns></returns>
        public bool WasOneSpecified(params string[] keys) {
            return this.IsOneSpecified(keys);
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
            return this.Get<ContentAlignment>("anchor",ContentAlignment.MiddleCenter);       } set {
            this.Set<ContentAlignment>("anchor",value);      }}



        /// <summary>
        /// Allows you to flip the entire resulting image vertically, horizontally, or both. Rotation is not supported.
        /// </summary>
        public RotateFlipType Flip                      { get {
                return (RotateFlipType)this.Get<FlipMode>("flip", FlipMode.None);
            }
            set {
            this.Set<FlipMode>("flip",(FlipMode)value);      }}

        /// <summary>
        /// ["sFlip"] Allows you to flip the source image vertically, horizontally, or both. Rotation is not supported.
        /// </summary>
        public RotateFlipType SourceFlip                    { get {
            return (RotateFlipType)this.Get<FlipMode>("sFlip", this.Get<FlipMode>("sourceFlip", FlipMode.None));
            }
            set {
                this.Set<FlipMode>("sflip", (FlipMode)value);
            }
        }

        /// <summary>
        /// ["scale"] Whether to downscale, upscale, upscale the canvas, or both upscale or downscale the image as needed. Defaults to
        /// DownscaleOnly. See the DefaultSettings plugin to adjust the default.
        /// </summary>
        public ScaleMode Scale                              { get {
                return this.Get<ScaleMode>("scale", ScaleMode.DownscaleOnly);
            }
            set {
                this.Set<ScaleMode>("scale", value);        }}

        /// <summary>
        /// [Deprecated] (Replaced by mode=stretch) Whether to preserve aspect ratio or stretch to fill the bounds.
        /// </summary>
        [Obsolete("Replaced by Mode=Stretch")]
        public StretchMode Stretch                          { get {
            return this.Get<StretchMode>("stretch", StretchMode.Proportionally);
            }
            set {
                this.Set<StretchMode>("stretch", value);   }}


        /// <summary>
        /// ["cache"]: Server caching mode suggestion for the result
        /// </summary>
        public ServerCacheMode Cache {
            get {
                return this.Get<ServerCacheMode>("cache",ServerCacheMode.Default);
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
                return this.Get<ProcessWhen>("process", this["useresizingpipeline"] != null ? ProcessWhen.Always : ProcessWhen.Default);
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
            if ("auto".Equals(this["crop"], StringComparison.OrdinalIgnoreCase)) return ImageResizer.CropMode.Auto;
            if (this.GetList<double>("crop", 0, 4) != null) return ImageResizer.CropMode.Custom;
            return ImageResizer.CropMode.None;
       } set {
                if (value == ImageResizer.CropMode.None) this.Remove("crop");
                else if (value == ImageResizer.CropMode.Auto) this["crop"] = "auto";
            }
        }

        /// <summary>
        /// 4 values specify x1,y1,x2,y2 values for the crop rectangle.
        /// Negative values are relative to the bottom right - on a 100x100 picture, (10,10,90,90) is equivalent to (10,10,-10,-10). And (0,0,0,0) is equivalent to (0,0,100,100).
        /// </summary>
        protected double[] CropValues {
            get {
                //Return (0,0,0,0) when null.
                double[] vals = this.GetList<double>( "crop", 0, 4);
                return vals != null ? vals : new double[] { 0, 0, 0, 0 };
            }
            set {
                this.SetList("crop", value, true, 4);
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
            get { return ParseUtils.ParseColor(this["bgcolor"], Color.Transparent); }
            set { this["bgcolor"] = Util.ParseUtils.SerializeColor(value); }
        }

        /// <summary>
        /// Gets/sets ["paddingColor"]. Named and hex values are supported. (rgb and rgba, both 3, 6, and 8 digits).
        /// </summary>
        public Color PaddingColor {
            get { return ParseUtils.ParseColor(this["paddingColor"], Color.Transparent); }
            set { this["paddingColor"] = Util.ParseUtils.SerializeColor(value); }
        }
        /// <summary>
        /// ["paddingWidth"]: Gets/sets the width(s) of padding inside the image border.
        /// </summary>
        public BoxPadding Padding {
            get {
                return BoxPadding.Parse(this["paddingWidth"], BoxPadding.Empty);
            }
            set {
                this.SetAsString<BoxPadding>("paddingWidth", value);
            }
        }
        /// <summary>
        /// ["margin"]: Gets/sets the width(s) of the margin outside the image border and effects.
        /// </summary>
        public BoxPadding Margin {
            get {
                return BoxPadding.Parse(this["margin"], BoxPadding.Empty);
            }
            set {
                this.SetAsString<BoxPadding>("margin", value);
            }
        }
        /// <summary>
        /// Gets/sets ["borderColor"]. Named and hex values are supported. (rgb and rgba, both 3, 6, and 8 digits).
        /// </summary>
        public Color BorderColor {
            get { return ParseUtils.ParseColor(this["borderColor"], Color.Transparent); }
            set { this["borderColor"] = Util.ParseUtils.SerializeColor(value); }
        }
        /// <summary>
        /// Friendly get/set accessor for the ["borderWidth"] value. Returns BoxPadding.Empty when unspecified.
        /// </summary>
        public BoxPadding Border {
            get {
                return BoxPadding.Parse(this["borderWidth"], BoxPadding.Empty);
            }
            set {
                this.SetAsString<BoxPadding>("borderWidth", value);
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

        /// <summary>
        /// The width which the X and X2 crop values should be applied. For example, a value of '100' makes X and X2 percentages of the original image width.
        /// This can be set to any non-negative value. Very useful for performing cropping when the original image size is unknown.
        /// 0 indicates that the crop values are relative to the original size of the image.
        /// </summary>
        public double CropXUnits { get { return this.Get<double>("cropxunits",0); } set { this.Set<double>("cropxunits", value <= 0 ? null : (double?)value); } }
        /// <summary>
        /// The width which the Y and Y2 crop values should be applied. For example, a value of '100' makes Y and Y2 percentages of the original image height.
        /// This can be set to any non-negative  value. Very useful for performing cropping when the original image size is unknown.
        /// 0 indicates that the crop values are relative to the original size of the image.
        /// </summary>        
        public double CropYUnits { get { return this.Get<double>("cropyunits", 0); } set { this.Set<double>("cropyunits", value <= 0 ? null : (double?)value); } }


        public RectangleF getCustomCropSourceRect(SizeF imageSize) {
            double xunits = this.Get<double>("cropxunits",0);
            double yunits = this.Get<double>("cropyunits",0);

            return PolygonMath.GetCroppingRectangle(CropValues, xunits, yunits, imageSize);
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

        /// <summary>
        /// This method will 'normalize' command aliases to the primary key name and resolve duplicates. 
        /// w->width, h->height, sourceFlip->sFlip, thumbnail->format
        /// </summary>
        public void Normalize() {
            this.Normalize("width", "w")
                .Normalize("height", "h")
                .Normalize("sFlip", "sourceFlip")
                .Normalize("format", "thumbnail");
        }



        /// <summary>
        /// Normalizes a command that has two possible names. 
        /// If either of the commands has a null or empty value, those keys are removed. 
        /// If both the the primary and secondary are present, the secondary is removed. 
        /// Otherwise, the secondary is renamed to the primary name.
        /// </summary>
        /// <param name="primary"></param>
        /// <param name="secondary"></param>
        public ResizeSettings Normalize(string primary, string secondary) {
            return (ResizeSettings)ImageResizer.ExtensionMethods.NameValueCollectionExtensions.Normalize(this, primary, secondary);
        }

    }
}
