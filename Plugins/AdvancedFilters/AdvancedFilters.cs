/* Copyright (c) 2014 Imazen See license.txt for your rights */
using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer;
using ImageResizer.Resizing;
using AForge.Imaging.Filters;
using AForge;
using System.Globalization;
using ImageResizer.Util;
using System.Drawing;
using ImageResizer.ExtensionMethods;
using System.Drawing.Imaging;

namespace ImageResizer.Plugins.AdvancedFilters {
    public class AdvancedFilters:BuilderExtension, IPlugin, IQuerystringPlugin {
        public AdvancedFilters() {
        }
        /// <summary>
        /// Adds the plugin to the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }
        /// <summary>
        /// Removes the plugin from the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        /// <summary>
        /// Calculates a radius based on the provided value, using min(width/height) as the normalizing factor. Querystring values are interpreted as 1/1000ths of the normalizing factor.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="key"></param>
        /// <param name="key2"></param>
        /// <param name="units"></param>
        /// <returns></returns>
        protected int GetRadius(ImageState s, string key, string key2, double units) {
            string str = s.settings[key];
            if (string.IsNullOrEmpty(str) && key2 != null) str = s.settings[key2];
            if (string.IsNullOrEmpty(str)) return -1;
            double d;
            if (double.TryParse(str, ParseUtils.FloatingPointStyle, NumberFormatInfo.InvariantInfo, out d) && d > 0) {
                double factor = Util.PolygonMath.GetShortestPair(s.layout["image"]) / units;

                return (int)Math.Round(factor * d);
            }
            return -1;

        }

        protected override RequestedAction PreRenderImage(ImageState s)
        {
            if (s.sourceBitmap == null) return RequestedAction.None;
            if (!s.settings.WasOneSpecified("a.featheredges")) return RequestedAction.None;

            s.ApplyCropping();
            s.EnsureRGBA();
            s.EnsurePreRenderBitmap();
            ApplyPreFiltersTo(ref s.preRenderBitmap, s);

            return RequestedAction.None;
        }

        protected override RequestedAction PostRenderImage(ImageState s) {
            if (s.destBitmap == null) return RequestedAction.None;
            Bitmap b = s.destBitmap;
            ApplyFiltersTo(ref b, s);
            s.destBitmap = b;
            return RequestedAction.None;
        }

        protected void ApplyPreFiltersTo(ref Bitmap b, ImageState s) {
            int i = 0;

            double units = s.settings.Get<double>("a.radiusunits", 1000);

            float fin = s.settings.Get<float>("a.featherin", 1);
            float fout = s.settings.Get<float>("a.featherout", 0);

            i = GetRadius(s, "a.featheredges", null, units * (Util.PolygonMath.GetShortestPair(s.layout["image"]) / Math.Min(b.Width,b.Height)));
            if (i > 0) new FeatherEdge(fout, fin, i).ApplyInPlace(b);
        }
        protected void ApplyFiltersTo(ref Bitmap b, ImageState s){

            var imageRectF = s.layout.GetRingAsRectF("image");
            var areaOfEffect = imageRectF == null ? new Rectangle(0,0,b.Width,b.Height) : PolygonMath.ToRectangle(imageRectF.Value);


            //TODO: if the image is unrotated, use a rectangle to limit the effect to the desired area

            string str = null;
            int i = 0;

            //If radiusunits is specified, use that code path.
            double units = s.settings.Get<double>("a.radiusunits",1000);
           
            i = GetRadius(s, "blur", "a.blur", units);
            if (i > 0) new GaussianBlur(1.4, i).ApplyInPlace(b, areaOfEffect);

            i = GetRadius(s, "sharpen", "a.sharpen", units);
            if (i > 0) new GaussianSharpen(1.4, Math.Min(11, i)).ApplyInPlace(b, areaOfEffect);

            i = GetRadius(s, "a.oilpainting", null, units);
            if (i > 0) new OilPainting(i).ApplyInPlace(b, areaOfEffect);

            if ("true".Equals(s.settings["a.removenoise"], StringComparison.OrdinalIgnoreCase)) {
                new ConservativeSmoothing(3).ApplyInPlace(b, areaOfEffect);
            } else {
                i = GetRadius(s, "a.removenoise", null, units);
                if (i > 0) new ConservativeSmoothing(i).ApplyInPlace(b, areaOfEffect);
            }



            //Sobel only supports 8bpp grayscale images.
            //true/false
            if ("true".Equals(s.settings["a.sobel"], StringComparison.OrdinalIgnoreCase)){
                Bitmap old = b;
                try{
                    b = Grayscale.CommonAlgorithms.Y.Apply(b);
                }finally{
                    if (old != s.sourceBitmap) old.Dispose();
                }

                new SobelEdgeDetector().ApplyInPlace(b, areaOfEffect);

                str = s.settings["a.threshold"]; //radius
                if (!string.IsNullOrEmpty(str) && int.TryParse(str, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out i) && i > 0)
                    new Threshold(i).ApplyInPlace(b, areaOfEffect);
    
            }
            //Canny Edge Detector only supports 8bpp grayscale images.
            //true/false
            if ("true".Equals(s.settings["a.canny"], StringComparison.OrdinalIgnoreCase)) {
                Bitmap old = b;
                try {
                    b = Grayscale.CommonAlgorithms.Y.Apply(b);
                } finally {
                    if (old != s.sourceBitmap) old.Dispose();
                }
                new CannyEdgeDetector().ApplyInPlace(b, areaOfEffect);


            }

            //true/false - duplicate with SimpleFilters?
            if ("true".Equals(s.settings["a.sepia"], StringComparison.OrdinalIgnoreCase))
                new Sepia().ApplyInPlace(b,areaOfEffect);
            
            //true/false
            if ("true".Equals(s.settings["a.equalize"], StringComparison.OrdinalIgnoreCase))
                new HistogramEqualization().ApplyInPlace(b, areaOfEffect);

            //White balance adjustment
            var whiteAlg = s.settings.Get<HistogramThresholdAlgorithm>("a.balancewhite");
            var whiteVal = s.settings.Get<double>("a.balancethreshold");


            if (whiteAlg != null || whiteVal != null) {
                var bal = new AutoWhiteBalance(whiteAlg ?? HistogramThresholdAlgorithm.Area);
                if (whiteVal != null) bal.LowThreshold = bal.HighThreshold = whiteVal.Value / 100;
                bal.ApplyInPlace(b, areaOfEffect);
            }

            str = s.settings["a.posterize"]; //number of colors to merge
            if (!string.IsNullOrEmpty(str) && int.TryParse(str, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out i) && i > 0) {
                SimplePosterization sp = new SimplePosterization();
                if (i < 1) i = 1; 
                if (i > 255) i = 255;
                sp.PosterizationInterval =(byte)i;
                sp.ApplyInPlace(b, areaOfEffect); 
            }

            //Pixellate doesn't support 32-bit images, only 24-bit
            //str = s.settings["a.pixelate"]; //number of colors to merge
            //if (!string.IsNullOrEmpty(str) && int.TryParse(str, out i)){
            //     if (i < 2) i = 2; 
            //    if (i > 32) i = 32;
            //    new Pixellate(i).ApplyInPlace(s.destBitmap); 
            //}
            

            float contrast = s.settings.Get<float>("a.contrast", 0);
            float brightness = s.settings.Get<float>("a.brightness", 0);
            float saturation = s.settings.Get<float>("a.saturation", 0);

            if (contrast != 0 || brightness != 0 || saturation != 0){
                HSLLinear adjust = new HSLLinear();
                AdjustContrastBrightnessSaturation(adjust, contrast, brightness, saturation, "true".Equals(s.settings["a.truncate"]));
                adjust.ApplyInPlace(b, areaOfEffect);
            }
            //TODO - add grayscale?

            //For adding fax-like thresholding, use BradleyLocalThresholding

            //For trimming solid-color whitespace, use Shrink

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="f"></param>
        /// <param name="contrast">-1..1 float to adjust contrast. </param>
        /// <param name="brightness">-1..1 float to adjust luminance (brightness). 0 does nothing</param>
        /// <param name="saturation">-1..1 float to adjust saturation.  0 does nothing  </param>
        /// <param name="truncate">If false, adjusting brightness and luminance will adjust contrast also. True causes white/black washout instead.</param>
        protected void AdjustContrastBrightnessSaturation(HSLLinear f, float contrast, float brightness, float saturation, bool truncate) {
            brightness = Math.Max(-1.0f, Math.Min(1.0f, brightness));
            saturation = Math.Max(-1.0f, Math.Min(1.0f, saturation));
            contrast = Math.Max(-1.0f, Math.Min(1.0f, contrast));


            // create luminance filter
            if (brightness > 0) {
                f.InLuminance = new Range(0.0f, 1.0f - (truncate ? brightness : 0)); //TODO - isn't it better not to truncate, but compress?
                f.OutLuminance = new Range(brightness, 1.0f);
            } else {
                f.InLuminance = new Range((truncate ? -brightness : 0), 1.0f);
                f.OutLuminance = new Range(0.0f, 1.0f + brightness);
            }
            // create saturation filter
            if (saturation > 0) {
                f.InSaturation = new Range(0.0f, 1.0f - (truncate ? saturation : 0)); //Ditto?
                f.OutSaturation = new Range(saturation, 1.0f);
            } else {
                f.InSaturation = new Range((truncate ? -saturation : 0), 1.0f);
                f.OutSaturation = new Range(0.0f, 1.0f + saturation);
            }

            if (contrast > 0) {
                float adjustment =  contrast * (f.InLuminance.Max - f.InLuminance.Min) / 2;
                f.InLuminance = new Range(f.InLuminance.Min + adjustment, f.InLuminance.Max - adjustment);
            } else if (contrast < 0) {
                float adjustment = -contrast * (f.OutLuminance.Max - f.OutLuminance.Min) / 2;
                f.OutLuminance = new Range(f.OutLuminance.Min + adjustment, f.OutLuminance.Max - adjustment);
            }
        }

        /// <summary>
        /// Returns the querystrings command keys supported by this plugin. 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "blur", "sharpen" , "a.blur", "a.sharpen", "a.oilpainting", "a.removenoise", 
                                "a.sobel", "a.threshold", "a.canny", "a.sepia", "a.equalize", "a.posterize", 
                                "a.contrast", "a.brightness", "a.saturation","a.truncate","a.balancewhite", "a.balancethreshold", "a.featheredges"};
        }
    }
}
