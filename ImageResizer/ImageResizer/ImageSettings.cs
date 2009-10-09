/**
 * Written by Nathanael Jones 
 * http://nathanaeljones.com
 * nathanael.jones@gmail.com
 * 
 * Although I typically release my components for free, I decided to charge a 
 * 'download fee' for this one to help support my other open-source projects. 
 * Don't worry, this component is still open-source, and the license permits 
 * source redistribution as part of a larger system. However, I'm asking that 
 * people who want to integrate this component purchase the download instead 
 * of ripping it out of another open-source project. My free to non-free LOC 
 * (lines of code) ratio is still over 40 to 1, and I plan on keeping it that 
 * way. I trust this will keep everybody happy.
 * 
 * By purchasing the download, you are permitted to 
 * 
 * 1) Modify and use the component in all of your projects. 
 * 
 * 2) Redistribute the source code as part of another project, provided 
 * the component is less than 5% of the project (in lines of code), 
 * and you keep this information attached.
 * 
 * 3) If you received the source code as part of another open source project, 
 * you cannot extract it (by itself) for use in another project without purchasing a download 
 * from http://nathanaeljones.com/. If nathanaeljones.com is no longer running, and a download
 * cannot be purchased, then you may extract the code.
 * 
 * Disclaimer of warranty and limitation of liability continued at http://nathanaeljones.com/11151_Image_Resizer_License
 **/

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Collections.Specialized;
using System.Drawing.Imaging;
using System.Globalization;

namespace fbs.ImageResizer
{
    /// <summary>
    /// TODO: later implement border, padding, and shadow support
    /// </summary>
    public class ImageSettings
    {
        public ImageSettings() { }

        public Color parseColor(NameValueCollection q, string key, Color defaultValue)
        {
            if (!string.IsNullOrEmpty(q[key]))
            {
                //try hex first
                int val;
                if (int.TryParse(q[key], System.Globalization.NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture, out val))
                {
                    return System.Drawing.ColorTranslator.FromHtml("#" + q[key]);
                }
                else
                {
                    Color c = System.Drawing.ColorTranslator.FromHtml(q[key]);
                    return (c.IsEmpty) ? defaultValue : c;
                }
            }
            return defaultValue;
        }

        public ImageSettings(NameValueCollection q)
        {
            parseFromQuerystring(q);
        }
        public Color bgcolor = Color.Transparent;
        public Color paddingColor = Color.Transparent;
        public Color borderColor = Color.Transparent;
        public Color shadowColor = Color.Transparent;
        public float paddingWidth = 0;
        public float borderWidth = 0;
        public float shadowWidth = 0;
        public PointF shadowOffset = new PointF(0, 0);

        /// <param name="background">The background color to draw against. Color.Transparent is usally fine.</param>
        /// <param name="borderWidth">The width of the border to draw around the image. </param>
        /// <param name="borderColor">The color of the border to draw around the image. Rounded edges are used. Can be partially transparent.</param>
        /// <param name="shadowWidth">The width of the shadow to draw around the image.</param>
        /// <param name="shadowColor">The color to start with at the outer edge of the image. Can be partially transparent.</param>
        /// <param name="shadowOffset">The distance to move the shadow to achieve a 'drop effect'</param>
        /// <param name="paddingColor">The color to paint between the image and the border. Used when both height and width are forced, and aspect ratio is maintained.</param>
        /// <param name="src">The bitmap to copy image data from.</param>
        /// <param name="sourceArea">The rectangle to copy image data from.</param>
        /// <param name="imageTarget">The parallelogram to copy the image to. All 4 points are clockwise, starting with top-left. </param>
        /// <param name="targetSpace">The parallelogram to draw the border and shadow around.</param>

        /// <summary>
        /// bgcolor=(transparent/white)
        /// paddingColor=(transparent/white)
        /// paddingWidth=x
        /// borderWidth=x
        /// borderColor=black
        /// shadowWidth=x
        /// shadowColor=black
        /// shadowOffset(x,y)|both
        /// </summary>
        /// <param name="q"></param>
        public void parseFromQuerystring(NameValueCollection q)
        {
            bgcolor = this.parseColor(q, "bgcolor", bgcolor);
            paddingColor = this.parseColor(q, "paddingColor", paddingColor);
            borderColor = this.parseColor(q, "borderColor", borderColor);
            shadowColor = this.parseColor(q, "shadowColor", shadowColor);
            paddingWidth = ResizeSettings.getFloat(q, "paddingWidth", paddingWidth);
            borderWidth = ResizeSettings.getFloat(q, "borderWidth", borderWidth);
            shadowWidth = ResizeSettings.getFloat(q, "shadowWidth", shadowWidth);

            if (!string.IsNullOrEmpty(q["shadowOffset"]))
            {
                double[] coords = ResizeSettings.parseList(q["shadowOffset"], 0);
                if (coords.Length == 2) shadowOffset = new PointF((float)coords[0], (float)coords[1]);
            }
        }

    }


    /// <summary>
    /// Eventually I will extend this class to allow grayscale, brightness, alpha, and possibly contrast adjustment.
    /// </summary>
    public class ImageFilter
    {
        public ImageFilter() { }
        public ImageFilter(NameValueCollection q)
        {
            parseFromQuerystring(q);
        }
        public void parseFromQuerystring(NameValueCollection q)
        {
        }
        public ImageAttributes getImageAttributes()
        {
            return null;
        }
        

        public static ImageAttributes GetGrayscaleTransform()
        {
            //from http://bobpowell.net/grayscale.htm 
            ImageAttributes ia = new ImageAttributes();
            ia.SetColorMatrix(new ColorMatrix(new float[][]{   new float[]{0.5f,0.5f,0.5f,0,0},
                                  new float[]{0.5f,0.5f,0.5f,0,0},
                                  new float[]{0.5f,0.5f,0.5f,0,0},
                                  new float[]{0,0,0,1,0,0},
                                  new float[]{0,0,0,0,1,0},
                                  new float[]{0,0,0,0,0,1}}));
            return ia;
        }

        public static ImageAttributes GetAlphaTransform(float alpha)
        {
            //http://www.codeproject.com/KB/GDI-plus/CsTranspTutorial2.aspx
            ImageAttributes ia = new ImageAttributes();
            ia.SetColorMatrix(new ColorMatrix(new float[][]{
                                  new float[]{1,0,0,0,0},
                                  new float[]{0,1,0,0,0},
                                  new float[]{0,0,1,0,0},
                                  new float[]{0,0,0,alpha,0},
                                  new float[]{0,0,0,0,1}}));
            ColorMatrix c = new ColorMatrix();
            
            return ia;
        }
        public static ImageAttributes GetBrightnessTransform(float factor)
        {
            //http://www.codeproject.com/KB/GDI-plus/CsTranspTutorial2.aspx
            ImageAttributes ia = new ImageAttributes();
            ia.SetColorMatrix(new ColorMatrix(new float[][]{
                                  new float[]{factor,0,0,0,0},
                                  new float[]{0,factor,0,0,0},
                                  new float[]{0,0,factor,0,0},
                                  new float[]{0,0,0,1,0},
                                  new float[]{0,0,0,0,1}}));
            return ia;
        }

    }

   
}
