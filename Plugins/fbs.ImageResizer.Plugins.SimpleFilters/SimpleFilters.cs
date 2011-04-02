using System;
using System.Collections.Generic;
using System.Text;
using fbs.ImageResizer.Resizing;
using System.Drawing.Imaging;
using System.Drawing;

namespace fbs.ImageResizer.Plugins.SimpleFilters {
    public class SimpleFilters : ImageBuilderExtension, IPlugin, IUrlPlugin {
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        public IEnumerable<string> GetSupportedFileExtensions() {
            return null;
        }

        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "filter" };
        }


        protected override RequestedAction PostCreateImageAttributes(ImageState s) {
            if (s.copyAttibutes == null) return RequestedAction.None;

            string filter = s.settings["filter"];
            if (string.IsNullOrEmpty(filter)) return RequestedAction.None;
            int valuesStart = filter.IndexOf('(');
            string valStr = null;
            double[] values = null;
            if (valuesStart > -1) {
                valStr = filter.Substring(valuesStart);
                filter = filter.Substring(0, valuesStart);
                values = Util.Utils.parseList(valStr, 0);
            }

            if ("grayscale".Equals(filter, StringComparison.OrdinalIgnoreCase)) s.copyAttibutes.SetColorMatrix(GetGrayscaleTransform());
            if ("sepia".Equals(filter, StringComparison.OrdinalIgnoreCase)) s.copyAttibutes.SetColorMatrix(GetGrayscaleTransform());
            if (values != null && values.Length == 1) {
                if ("alpha".Equals(filter, StringComparison.OrdinalIgnoreCase)) s.copyAttibutes.SetColorMatrix(GetAlphaTransform((float)values[0]));
                if ("brightness".Equals(filter, StringComparison.OrdinalIgnoreCase)) s.copyAttibutes.SetColorMatrix(GetBrightnessTransform((float)values[0]));
            }
            return RequestedAction.None;
        }

        public static ColorMatrix GetSepiaTransform() {
            //from http://www.techrepublic.com/blog/howdoi/how-do-i-convert-images-to-grayscale-and-sepia-tone-using-c/120
            return new ColorMatrix(new float[][]{   
                    new float[] {0.299f, 0.299f, 0.299f, 0, 0},
                    new float[] {0.587f, 0.587f, 0.587f, 0, 0},
                    new float[] {0.114f, 0.114f, 0.114f, 0, 0},
                    new float[] {     0,      0,      0, 1, 0},
                    new float[] {     0,      0,      0, 0, 0}});
            
        }

        public static ColorMatrix GetGrayscaleTransform(Color c) {
            float r = (float)c.R / 255.0F;
            float g = (float)c.G / 255.0F;
            float b = (float)c.B / 255.0F;
            //Could do a grayscale filter on a color
            return new ColorMatrix(new float[][]{   
                                    new float[]{0.5f,0.5f,0.5f,0,0},
                                  new float[]{0.5f,0.5f,0.5f,0,0},
                                  new float[]{0.5f,0.5f,0.5f,0,0},
                                  new float[]{0,0,0,1,0},
                                  new float[]{0,0,0,0,1}});
            
        }

        public static ColorMatrix GetGrayscaleTransform() {
            //from http://bobpowell.net/grayscale.htm 
            return new ColorMatrix(new float[][]{   
                                    new float[]{0.5f,0.5f,0.5f,0,0},
                                  new float[]{0.5f,0.5f,0.5f,0,0},
                                  new float[]{0.5f,0.5f,0.5f,0,0},
                                  new float[]{0,0,0,1,0},
                                  new float[]{0,0,0,0,1}});
           
        }

        public static ColorMatrix GetAlphaTransform(float alpha) {
            //http://www.codeproject.com/KB/GDI-plus/CsTranspTutorial2.aspx
            return new ColorMatrix(new float[][]{
                                  new float[]{1,0,0,0,0},
                                  new float[]{0,1,0,0,0},
                                  new float[]{0,0,1,0,0},
                                  new float[]{0,0,0,alpha,0},
                                  new float[]{0,0,0,0,1}});
        }
        public static ColorMatrix GetBrightnessTransform(float factor) {
            //http://www.codeproject.com/KB/GDI-plus/CsTranspTutorial2.aspx
            return new ColorMatrix(new float[][]{
                                  new float[]{factor,0,0,0,0},
                                  new float[]{0,factor,0,0,0},
                                  new float[]{0,0,factor,0,0},
                                  new float[]{0,0,0,1,0},
                                  new float[]{0,0,0,0,1}});

        }


    }
}
