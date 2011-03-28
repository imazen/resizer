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
using System.Drawing.Drawing2D;

namespace fbs.ImageResizer.Misc
{

    public class ImageFilter
    {
        

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
