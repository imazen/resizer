/* Copyright (c) 2014 Imazen See license.txt for your rights. */
using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using System.Drawing.Imaging;
using System.Drawing;
using ImageResizer.Util;
using System.Globalization;
using System.Drawing.Drawing2D;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.SimpleFilters {
    /// <summary>
    /// This plugin provides grayscale, sepia, brightness, saturation, contrast, inversion, and alpha filtering options. It also includes beta support for rounded corners.
    /// </summary>
    public class SimpleFilters : BuilderExtension, IPlugin, IQuerystringPlugin {
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
        /// Returns the querystrings command keys supported by this plugin. 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "filter", "s.grayscale", "s.overlay", "s.shift", "s.sepia", "s.alpha", "s.brightness", "s.contrast", "s.saturation", "s.invert","s.roundcorners" };
        }

        /// <summary>
        /// Supporting rounded corners.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        protected override RequestedAction PreRenderImage(ImageState s) {
            if (s.sourceBitmap == null) return RequestedAction.None;
            double[] vals = s.settings.GetList<double>("s.roundcorners",0,1,4);
            if (vals == null) return RequestedAction.None;

            if (vals.Length == 1)  vals = new double[]{vals[0],vals[0],vals[0],vals[0]};
            
            bool hasValue = false;
            foreach (double d in vals) if (d > 0) hasValue = true;
            if (!hasValue) return RequestedAction.None;
            
            Bitmap cropped = null;
            try{
                //Make sure cropping is applied, and use existing prerendered bitmap if present.
                s.ApplyCropping();

                cropped = s.preRenderBitmap ?? s.sourceBitmap;
               
                s.preRenderBitmap = new Bitmap(cropped.Width,cropped.Height, PixelFormat.Format32bppArgb);

                int[] radius = new int[4];
                //Radius percentages are 0-100, a percentage of the smaller of the width and height.
                for (int i = 0; i < vals.Length; i++) radius[i] = (int)Math.Round(Math.Max(0,Math.Min(99.999,vals[i])) * ((double)Math.Min(s.preRenderBitmap.Width,s.preRenderBitmap.Height) / 100));


                s.preRenderBitmap.MakeTransparent();
                using (Graphics g = Graphics.FromImage(s.preRenderBitmap)) {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.CompositingMode = CompositingMode.SourceOver;
                    g.CompositingQuality = CompositingQuality.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    using (TextureBrush tb = new TextureBrush(cropped))
                    using (GraphicsPath gp = new GraphicsPath(FillMode.Winding)) {
                        Rectangle bounds = new Rectangle(0, 0, s.preRenderBitmap.Width, s.preRenderBitmap.Height);
                        int[] angles = new int[]{180,270,0,90};
                        int[] xs = new int[]{bounds.X,bounds.Right - radius[1], bounds.Right - radius[2], bounds.X};
                        int[] ys = new int[]{bounds.Y,bounds.Y,bounds.Bottom - radius[2], bounds.Bottom - radius[3]};
                        for (int i =0; i < 4; i++){
                            if (radius[i] > 0){
                                gp.AddArc(xs[i],ys[i],radius[i],radius[i],angles[i],90);
                            }else{
                                gp.AddLine(xs[i],ys[i],xs[i],ys[i]);
                            }
                        }
                        g.FillPath(tb, gp);

                    }
                }
            }finally{
                if (cropped != null & cropped != s.sourceBitmap) cropped.Dispose();
            }
            return RequestedAction.None;
        }

        protected override RequestedAction PostCreateImageAttributes(ImageState s) {
            
            if (!s.settings.WasOneSpecified((string[])GetSupportedQuerystringKeys())) return RequestedAction.None;

            List<float[][]> filters = new List<float[][]>();

            string filter = s.settings["filter"];
            if (!string.IsNullOrEmpty(filter)) {
                int valuesStart = filter.IndexOf('(');
                string valStr = null;
                double[] values = null;
                if (valuesStart > -1) {
                    valStr = filter.Substring(valuesStart);
                    filter = filter.Substring(0, valuesStart);
                    values = ParseUtils.ParseList<double>(valStr, 0,0,1);
                }

                if ("grayscale".Equals(filter, StringComparison.OrdinalIgnoreCase)) filters.Add(GrayscaleFlat());
                if ("sepia".Equals(filter, StringComparison.OrdinalIgnoreCase)) filters.Add(Sepia());
                if (values != null && values.Length == 1) {
                    if ("alpha".Equals(filter, StringComparison.OrdinalIgnoreCase)) filters.Add(Alpha((float)values[0]));
                    if ("brightness".Equals(filter, StringComparison.OrdinalIgnoreCase)) filters.Add(Brightness((float)values[0]));
                }
            }
            if ("true".Equals(s.settings["s.grayscale"], StringComparison.OrdinalIgnoreCase)) filters.Add(GrayscaleNTSC());
            if ("flat".Equals(s.settings["s.grayscale"], StringComparison.OrdinalIgnoreCase)) filters.Add(GrayscaleFlat());
            if ("y".Equals(s.settings["s.grayscale"], StringComparison.OrdinalIgnoreCase)) filters.Add(GrayscaleY());
            if ("ry".Equals(s.settings["s.grayscale"], StringComparison.OrdinalIgnoreCase)) filters.Add(GrayscaleRY());
            if ("ntsc".Equals(s.settings["s.grayscale"], StringComparison.OrdinalIgnoreCase)) filters.Add(GrayscaleNTSC());
            if ("bt709".Equals(s.settings["s.grayscale"], StringComparison.OrdinalIgnoreCase)) filters.Add(GrayscaleBT709());

            if ("true".Equals(s.settings["s.sepia"], StringComparison.OrdinalIgnoreCase)) filters.Add(Sepia());
            if ("true".Equals(s.settings["s.invert"], StringComparison.OrdinalIgnoreCase)) filters.Add(Invert());

            Color? c = Util.ParseUtils.ParseColor(s.settings["s.shift"]);

            if (c != null) filters.Add(Shift(c.Value));

            string alpha = s.settings["s.alpha"];
            string brightness = s.settings["s.brightness"];
            string contrast = s.settings["s.contrast"];
            string saturation = s.settings["s.saturation"];

            double temp = 0;
            if (!string.IsNullOrEmpty(alpha) && double.TryParse(alpha, ParseUtils.FloatingPointStyle, NumberFormatInfo.InvariantInfo, out temp)) filters.Add(Alpha((float)temp));
            if (!string.IsNullOrEmpty(brightness) && double.TryParse(brightness, ParseUtils.FloatingPointStyle, NumberFormatInfo.InvariantInfo, out temp)) filters.Add(Brightness((float)temp));
            if (!string.IsNullOrEmpty(contrast) && double.TryParse(contrast, ParseUtils.FloatingPointStyle, NumberFormatInfo.InvariantInfo, out temp)) filters.Add(Contrast((float)temp));
            if (!string.IsNullOrEmpty(saturation) && double.TryParse(saturation, ParseUtils.FloatingPointStyle, NumberFormatInfo.InvariantInfo, out temp)) filters.Add(Saturation((float)temp));


            if (filters.Count == 0) return RequestedAction.None;
            if (filters.Count == 1) s.colorMatrix = filters[0];
            else {
                //Multiple all the filters
                float[][] first = filters[0];

                for (int i = 1; i < filters.Count; i++) {
                    first = Multiply(first, filters[i]);
                }
                s.colorMatrix = first;

            }


            return RequestedAction.None;
        }


        protected override RequestedAction PostRenderImage(ImageState s) {
            Color? c = Util.ParseUtils.ParseColor(s.settings["s.overlay"]);

            if (c != null && s.destGraphics != null) {
                using (var b = new SolidBrush(c.Value)) {
                    s.destGraphics.FillPolygon(b, s.layout["image"]);
                }
            }
            return RequestedAction.None;
        }

        private static float[][] Multiply(float[][] f1, float[][] f2) {
            float[][] X = new float[5][];
            for (int d = 0; d < 5; d++)
                X[d] = new float[5];
            int size = 5;
            float[] column = new float[5];
            for (int j = 0; j < 5; j++) {
                for (int k = 0; k < 5; k++) {
                    column[k] = f1[k][j];
                }
                for (int i = 0; i < 5; i++) {
                    float[] row = f2[i];
                    float s = 0;
                    for (int k = 0; k < size; k++) {
                        s += row[k] * column[k];
                    }
                    X[i][j] = s;
                }
            }
            return X;
        }

        static float[][] Sepia() {
            //from http://www.techrepublic.com/blog/howdoi/how-do-i-convert-images-to-grayscale-and-sepia-tone-using-c/120
            return (new float[][]{   
                    new float[] {0.393f,0.349f,0.272f, 0, 0},
                    new float[] {0.769f,0.686f,0.534f, 0, 0},
                    new float[] {0.189f, 0.168f,0.131f, 0, 0},
                    new float[] {     0,      0,      0, 1, 0},
                    new float[] {     0,      0,      0, 0, 0}});

        }

        /*Y = 0.299 * R + 0.587 * G + 0.114 * B
I = 0.596 * R - 0.274 * G - 0.322 * B
Q = 0.212 * R - 0.523 * G + 0.311 * B
         */
        /// <summary>
        /// Not yet implemented
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        static float[][] Grayscale(Color c) {
            float r = (float)c.R / 255.0F;
            float g = (float)c.G / 255.0F;
            float b = (float)c.B / 255.0F;
            //Could do a grayscale filter on a color
            return Grayscale(r, g, b);

        }

//Warming Filter (85) #EC8A00
//Warming Filter (LBA) #FA9600
//Warming Filter (81) #EBB113
//Coolling Filter (80) #006DFF
//Cooling Filter (LBB) #005DFF
//Cooling Filter (82) #00B5FF
//Red #EA1A1A
//Orange #F38417
//Yellow #F9E31C
//Green #19C919
//Cyan #1DCBEA
//Blue #1D35EA
//Violet #9B1DEA
//Magenta #E318E3
//Sepia #AC7A33
//Deep Red #FF0000
//Deep Blue #0022CD
//Deep Emerald #008C00
//Deep Yellow #FFD500
//Underwater #00C1B1

        static float[][] Shift(Color c) {
            float percent = (float)c.A / 255.0f;
            return (new float[][]{   
                    new float[] {1 - percent,0,0, 0,0},
                    new float[] {0, 1-percent, 0, 0,0},
                    new float[] {0,0,1-percent, 0,  0},
                    new float[] {     0,      0,      0, 1, 0},
                    new float[] { ((float)c.R - 128f) / 128f * percent,      ((float)c.G - 128f) / 128f * percent,     ((float)c.B - 128f) / 128f * percent, 0, 1}});
        }

        static float[][] Grayscale(float r, float g, float b) {
            return (new float[][]{   
                                  new float[]{r,r,r,0,0},
                                  new float[]{g,g,g,0,0},
                                  new float[]{b,b,b,0,0},
                                  new float[]{0,0,0,1,0},
                                  new float[]{0,0,0,0,1}});

        }

        static float[][] GrayscaleFlat() {
            return Grayscale(0.5f, 0.5f, 0.5f);
        }

        static float[][] GrayscaleBT709() {
            return Grayscale(0.2125f, 0.7154f, 0.0721f);
        }
        static float[][] GrayscaleRY() {
            return Grayscale(0.5f, 0.419f, 0.081f);
        }
        static float[][] GrayscaleY() {
            return Grayscale(0.229f, 0.587f, 0.114f);
        }

        static float[][] GrayscaleNTSC() {
            return GrayscaleY();
        }

        static float[][] Invert() {
            return (new float[][]{   
                                    new float[]{-1,0,0,0,0},
                                  new float[]{0,-1,0,0,0},
                                  new float[]{0,0,-1,0,0},
                                  new float[]{0,0,0,1,0},
                                  new float[]{1,1,1,0,1}});

        }






        static float[][] Alpha(float alpha) {
            //http://www.codeproject.com/KB/GDI-plus/CsTranspTutorial2.aspx
            return (new float[][]{
                                  new float[]{1,0,0,0,0},
                                  new float[]{0,1,0,0,0},
                                  new float[]{0,0,1,0,0},
                                  new float[]{0,0,0,alpha,0},
                                  new float[]{0,0,0,0,1}});
        }
        static float[][] Contrast(float c) {


            c++; //Stop at -1

            float factorT = 0.5f * (1.0f - c); 

            return (new float[][]{
                                  new float[]{c,0,0,0,0},
                                  new float[]{0,c,0,0,0},
                                  new float[]{0,0,c,0,0},
                                  new float[]{0,0,0,1,0},
                                  new float[]{factorT,factorT,factorT,0,1}});
        }

        static float[][] Brightness(float factor) {
            return new float[][]{
                                  new float[]{1,0,0,0,0},
                                  new float[]{0,1,0,0,0},
                                  new float[]{0,0,1,0,0},
                                  new float[]{0,0,0,1,0},
                                  new float[]{factor,factor,factor,0,1}};

        }
        /// <summary>
        /// Saturation is between -1 and infinity
        /// </summary>
        /// <param name="saturation"></param>
        /// <returns></returns>
        static float[][] Saturation(float saturation) {
            //http://www.bobpowell.net/imagesaturation.htm

            saturation = Math.Max(0, saturation + 1); //Stop at -1

            //saturation = Math.Max(Math.Min(saturation, 0), 1);

            float SaturationComplement = 1.0f - saturation;
            float SaturationComplementR = 0.3086f * SaturationComplement;
            float SaturationComplementG = 0.6094f * SaturationComplement;
            float SaturationComplementB = 0.0820f * SaturationComplement;
            return new float[][]{
                               new float[]{SaturationComplementR + saturation,  SaturationComplementR,  SaturationComplementR,  0.0f, 0.0f},
                               new float[]{SaturationComplementG,  SaturationComplementG + saturation,  SaturationComplementG,  0.0f, 0.0f},
                               new float[]{SaturationComplementB,  SaturationComplementB,  SaturationComplementB + saturation,  0.0f, 0.0f},
                               new float[]{0.0f,  0.0f,  0.0f,  1.0f,  0.0f},
                               new float[]{0.0f,  0.0f,  0.0f,  0.0f,  1.0f}};

        }



    }
}
