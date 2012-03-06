using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using AForge.Imaging.Filters;
using System.Globalization;
using ImageResizer.Util;
using AForge;
using AForge.Imaging;
using System.Drawing.Imaging;
using System.Drawing;

namespace ImageResizer.Plugins.RedEye {
    public class RedEyePlugin:BuilderExtension, IPlugin, IQuerystringPlugin {
        public RedEyePlugin() {
        }

        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        //In automatic mode, thresholds need to kick in
        //In manual mode, we have to autodetect (a) do we need to sublocate the eye? or (b) is they eye already selected such that trying to locate it will fail? 

        protected unsafe void SemiAutoCorrectRedEye(UnmanagedImage image, int cx, int cy) {
            Rectangle bright = new GridSearch().FindBrightestSquare(image, cx, cy);
            CorrectRedEye(image, bright.X + bright.Width / 2, bright.Y + bright.Height / 2, bright.Width / 2,3 + (int)(bright.Width * 0.2));
        }


        protected unsafe void CorrectRedEye(UnmanagedImage image, int cx, int cy, int radius, int fadeEdge = 3, double threshold = -1) {
            int pixelSize = System.Drawing.Image.GetPixelFormatSize(image.PixelFormat) / 8;
            if (pixelSize > 4) throw new Exception("Invalid pixel depth");

            long scan0 = (long)image.ImageData;
            long stride = image.Stride;
            // do the job
            byte* src;
            //Establish bounds
            int top = Math.Max(0,cy - radius - fadeEdge);
            int bottom = Math.Min(image.Height, cy + radius + fadeEdge +1);
            int left = Math.Max(0, cx - radius - fadeEdge);
            int right = Math.Min(image.Width, cx + radius + fadeEdge + 1);

            double fade = 0;
            double red = 0;
            byte gray;

            //Scan region (pass 1) using RMS
            double meansq = 0;
            long pixels = 0;
            for (int y = top; y < bottom; y++) {
                src = (byte*)(scan0 + y * stride + (left * pixelSize));
                for (int x = left; x < right; x++, src += pixelSize) {
                    pixels++;
                    meansq = meansq * ((pixels - 1) / pixels);
                    if (src[RGB.R] == 0) continue;

                    meansq +=  (src[RGB.R] - Math.Max(src[RGB.G], src[RGB.B])) / (double)src[RGB.R] * 100 / pixels;
                }
            }
            //Get a value between 0 and 1
            double rms = Math.Sqrt(meansq) / 100;

            if (threshold < 0) threshold = rms * 0.4;

            //Scan region
            for (int y = top; y < bottom; y++) {
                src = (byte*)(scan0 + y * stride + (left * pixelSize));
                for (int x = left; x < right; x++, src += pixelSize) {
                    if (src[RGB.R] == 0) continue; //Because 0 will crash the formula
                    //Calculate distance from center
                    fade = Math.Sqrt((x - cx) * (x -cx) + (y - cy) * (y-cy));
                    //Calculate the fade factor (using a linear fade when we are between radius and radius + fadeEdge from the center). 
                    fade = (fade <= radius) ? 1 : Math.Max(0,
                        ((fadeEdge + radius - fade) / fadeEdge));
                    
                    //Calculate red baddness between 0 and 1
                    red = (src[RGB.R] - Math.Max(src[RGB.G], src[RGB.B])) / (double)src[RGB.R];
                    
                    //Skip if we're outside the threshold
                    if (red < threshold) continue;

                    //Calculate monochrome alternative
                    gray = (byte)(src[RGB.G] * 0.587f + src[RGB.B] * 0.114f);

                    //Apply monochrome alternative
                    src[RGB.R] = (byte)((fade * gray) + (1 - fade) * src[RGB.R]);
                }
            }
        }



        protected override RequestedAction PostRenderImage(ImageState s) {

            if (s.destBitmap == null) return RequestedAction.None;
            string str = null;
            int i = 0;

            if (!string.IsNullOrEmpty(s.settings["r.eyes"])) {
                double[] eyes = Utils.parseList(s.settings["r.eyes"], 0);
                // lock source bitmap data
                BitmapData data = s.destBitmap.LockBits(
                    new Rectangle(0, 0, s.destBitmap.Width, s.destBitmap.Height),
                    ImageLockMode.ReadWrite, s.destBitmap.PixelFormat);

                try {
                    UnmanagedImage ui = new UnmanagedImage(data);

                    for (i = 0; i < eyes.Length -2; i += 3) {
                        AdaptiveCircleFill.MarkEye(ui, new System.Drawing.Point((int)eyes[i], (int)eyes[i + 1]), (int)Math.Ceiling(0.02 * Math.Max(ui.Width, ui.Height)), 24);
                            
                        /*if (eyes[i + 2] > 0) {
                            AdaptiveCircleFill.MarkEye(ui, new System.Drawing.Point((int)eyes[i], (int)eyes[i + 1]),(int)Math.Ceiling(0.025 * Math.Max(ui.Width,ui.Height)),24);
                            //CorrectRedEye(ui, (int)eyes[i], (int)eyes[i + 1], (int)eyes[i + 2]);
                        }else{
                            SemiAutoCorrectRedEye(ui, (int)eyes[i], (int)eyes[i + 1]);
                        }*/
                    }

                } finally {
                    // unlock image
                    s.destBitmap.UnlockBits(data);
                }
            }


            str = s.settings["r.filter"]; //radius
            if (!string.IsNullOrEmpty(str) && int.TryParse(str, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out i)) {
                using (s.destBitmap) {
                    s.destBitmap = new RedEyeFilter((short)i).Apply(s.destBitmap);
                }

                //Sobel only supports 8bpp grayscale images.
                //true/false
                if ("true".Equals(s.settings["r.sobel"], StringComparison.OrdinalIgnoreCase)) {
                    new SobelEdgeDetector().ApplyInPlace(s.destBitmap);

                    str = s.settings["r.threshold"]; //radius
                    if (!string.IsNullOrEmpty(str) && int.TryParse(str, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out i))
                        new Threshold(i).ApplyInPlace(s.destBitmap);

                }
                //Canny Edge Detector only supports 8bpp grayscale images.
                //true/false
                if ("true".Equals(s.settings["r.canny"], StringComparison.OrdinalIgnoreCase)) {
                    new CannyEdgeDetector().ApplyInPlace(s.destBitmap);
                }

                int sn = 1;
                str = s.settings["r.sn"]; //multiple negative weights
                if (string.IsNullOrEmpty(str) || int.TryParse(str, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out sn)) sn = 1;
                    


                str = s.settings["r.conv"]; //convolution multiplier
                if (!string.IsNullOrEmpty(str) && int.TryParse(str, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out i))
                    new Convolution(EyeKernels.Scale(EyeKernels.GetStandardKernel(i),1,sn)).ApplyInPlace(s.destBitmap);

                str = s.settings["r.econv"]; //convolution multiplier
                if (!string.IsNullOrEmpty(str) && int.TryParse(str, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out i))
                    new Convolution(EyeKernels.Scale(EyeKernels.GetElongatedKernel(i), 1, sn)).ApplyInPlace(s.destBitmap);
                

            }

            str = s.settings["a.contrast"];
            string strB = s.settings["a.brightness"];
            string strS = s.settings["a.saturation"];
            

            if (!string.IsNullOrEmpty(str) || !string.IsNullOrEmpty(strB) || !string.IsNullOrEmpty(strS)) {
                float contrast, brightness, saturation;
                if (string.IsNullOrEmpty(str) || !float.TryParse(str, Utils.floatingPointStyle, NumberFormatInfo.InvariantInfo, out contrast)) contrast = 0;
                if (string.IsNullOrEmpty(strB) || !float.TryParse(strB, Utils.floatingPointStyle, NumberFormatInfo.InvariantInfo, out brightness)) brightness = 0;
                if (string.IsNullOrEmpty(strS) || !float.TryParse(strS, Utils.floatingPointStyle, NumberFormatInfo.InvariantInfo, out saturation)) saturation = 0;

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
            return new string[] { "blur", "sharpen" , "a.blur", "a.sharpen", "a.oilpainting", "a.removenoise", 
                                "a.sobel", "a.threshold", "a.canny", "a.sepia", "a.equalize", "a.posterize", 
                                "a.contrast", "a.brightness", "a.saturation","a.truncate"};
        }
    }
}
