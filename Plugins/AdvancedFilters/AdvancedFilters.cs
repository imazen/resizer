/* Copyright (c) 2011 Nathanael Jones. See license.txt for your rights */
using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer;
using ImageResizer.Resizing;
using AForge.Imaging.Filters;
using AForge;
namespace ImageResizer.Plugins.AdvancedFilters {
    public class AdvancedFilters:BuilderExtension, IPlugin, IQuerystringPlugin {
        public AdvancedFilters() {
        }

        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }
        protected override RequestedAction PostRenderImage(ImageState s) {

            string str = null;
            int i = 0;
            
            
            str = s.settings["blur"]; //radius
            if (string.IsNullOrEmpty(str)) str= s.settings["a.blur"];
            if (!string.IsNullOrEmpty(str) && int.TryParse(str, out i))
                new GaussianBlur(1.4, i).ApplyInPlace(s.destBitmap);
            
            str = s.settings["sharpen"]; //radius
            if (string.IsNullOrEmpty(str)) str= s.settings["a.sharpen"];
            if (!string.IsNullOrEmpty(str) && int.TryParse(str, out i))
                new GaussianSharpen(1.4, i).ApplyInPlace(s.destBitmap);

            str = s.settings["a.oilpainting"]; //radius
            if (!string.IsNullOrEmpty(str) && int.TryParse(str, out i))
                new OilPainting(i).ApplyInPlace(s.destBitmap);

            str = s.settings["a.removenoise"]; //radius
            if ("true".Equals(str, StringComparison.OrdinalIgnoreCase)) str = "3";
            if (!string.IsNullOrEmpty(str) && int.TryParse(str, out i))
                new ConservativeSmoothing(i).ApplyInPlace(s.destBitmap); 

            //Sobel only supports 8bpp grayscale images.
            //true/false
            if ("true".Equals(s.settings["a.sobel"], StringComparison.OrdinalIgnoreCase)){
                using (s.destBitmap){
                    s.destBitmap = Grayscale.CommonAlgorithms.Y.Apply(s.destBitmap);
                    
                }
                new SobelEdgeDetector().ApplyInPlace(s.destBitmap);

                str = s.settings["a.threshold"]; //radius
                if (!string.IsNullOrEmpty(str) && int.TryParse(str, out i))
                 new Threshold(i).ApplyInPlace(s.destBitmap);
    
            }
            //Canny Edge Detector only supports 8bpp grayscale images.
            //true/false
            if ("true".Equals(s.settings["a.canny"], StringComparison.OrdinalIgnoreCase)) {
                using (s.destBitmap) {
                    s.destBitmap = Grayscale.CommonAlgorithms.Y.Apply(s.destBitmap);

                }
                new CannyEdgeDetector().ApplyInPlace(s.destBitmap);


            }

            //true/false - duplicate with SimpleFilters?
            if ("true".Equals(s.settings["a.sepia"], StringComparison.OrdinalIgnoreCase))
                new Sepia().ApplyInPlace(s.destBitmap);
            
            //true/false
            if ("true".Equals(s.settings["a.equalize"], StringComparison.OrdinalIgnoreCase))
                new HistogramEqualization().ApplyInPlace(s.destBitmap);


            str = s.settings["a.posterize"]; //number of colors to merge
            if (!string.IsNullOrEmpty(str) && int.TryParse(str, out i)){
                SimplePosterization sp = new SimplePosterization();
                if (i < 1) i = 1; 
                if (i > 255) i = 255;
                sp.PosterizationInterval =(byte)i;
                sp.ApplyInPlace(s.destBitmap); 
            }

            //Pixellate doesn't support 32-bit images, only 24-bit
            //str = s.settings["a.pixelate"]; //number of colors to merge
            //if (!string.IsNullOrEmpty(str) && int.TryParse(str, out i)){
            //     if (i < 2) i = 2; 
            //    if (i > 32) i = 32;
            //    new Pixellate(i).ApplyInPlace(s.destBitmap); 
            //}
            

            str = s.settings["a.contrast"];
            string strB = s.settings["a.brightness"];
            string strS = s.settings["a.saturation"];
            

            if (!string.IsNullOrEmpty(str) || !string.IsNullOrEmpty(strB) || !string.IsNullOrEmpty(strS)) {
                float contrast, brightness, saturation;
                if (string.IsNullOrEmpty(str) || !float.TryParse(str, out contrast)) contrast = 0;
                if (string.IsNullOrEmpty(strB) || !float.TryParse(strB, out brightness)) brightness = 0;
                if (string.IsNullOrEmpty(strS) || !float.TryParse(strS, out saturation)) saturation = 0;

                HSLLinear adjust = new HSLLinear();
                AdjustContrastBrightnessSaturation(adjust, contrast, brightness, saturation, "true".Equals(s.settings["a.truncate"]));
                adjust.ApplyInPlace(s.destBitmap);
            }
            //TODO - add grayscale?

            //For adding fax-like thresholding, use BradleyLocalThresholding

            //For trimming solid-color whitespace, use Shrink

            return RequestedAction.None;
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

        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "blur", "sharpen" };
        }
    }
}
