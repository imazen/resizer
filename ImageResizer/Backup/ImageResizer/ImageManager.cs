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
 **/

using System;
using System.Data;
using System.Configuration;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.Drawing;
using fbs;
using System.IO;
using System.Web.Hosting;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.Specialized;

namespace fbs.ImageResizer
{
    class ImageManager
    {
        /// <summary>
        /// Takes sourceFile, resizes it, and saves it to targetFile using the querystring values in request.
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="targetFile"></param>
        /// <param name="request"></param>
        public static void ResizeImage(string sourceFile, string targetFile, NameValueCollection queryString)
        {
            //Resize image 
            using (Bitmap thumb = GenerateImage(sourceFile, queryString))
            {
                //Determine desired output format
                System.Drawing.Imaging.ImageFormat outputType = GetOutputType(System.IO.Path.GetExtension(targetFile));

                //Open stream and save format.
                System.IO.FileStream fs = new FileStream(targetFile, FileMode.Create, FileAccess.Write);
                using (fs)
                {
                    //Jpegs require special parameters to get a good quality/file size balance. 90 works good
                    if (outputType == System.Drawing.Imaging.ImageFormat.Jpeg)
                    {
                        //Allow quality to be specified in the querystring.
                        int quality = 90;
                        if (!string.IsNullOrEmpty(queryString["quality"]))
                            if (!int.TryParse(queryString["quality"], out quality))
                                quality = 90;

                        if (quality < 0) quality = 90; //90 is a very good default to stick with.
                        if (quality > 100) quality = 100;

                        System.Drawing.Imaging.EncoderParameters encoderParameters;
                        encoderParameters = new System.Drawing.Imaging.EncoderParameters(1);
                        encoderParameters.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality);
                        thumb.Save(fs, GetImageCodeInfo("image/jpeg"), encoderParameters);
                    }
                    else
                    {
                        thumb.Save(fs, outputType);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the first ImageCodeInfo instance with the specified mime type.
        /// </summary>
        /// <param name="mimeType"></param>
        /// <returns></returns>
        public static ImageCodecInfo GetImageCodeInfo(string mimeType)
        {
            ImageCodecInfo[] info = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo ici in info)
                if (ici.MimeType.Equals(mimeType, StringComparison.OrdinalIgnoreCase)) return ici;
            return null;
        }

        /// <summary>
        /// Returns the appropriate ImageFormat value for the file extension. Unknowns default to jpeg.
        /// </summary>
        /// <param name="requestUrl"></param>
        /// <returns></returns>
        public static System.Drawing.Imaging.ImageFormat GetOutputType(string extension)
        {
            switch (extension.ToLower().Trim('.').Trim())
            {
                case "png":
                    return System.Drawing.Imaging.ImageFormat.Png;
                case "gif":
                    return System.Drawing.Imaging.ImageFormat.Gif;
            }
            return ImageFormat.Jpeg;
        }


        /// <summary>
        /// Generates a resized bitmap from the specifed source file and the specified querystring. Understands width/height and maxwidth/maxheight.
        /// Throws either an ArgumentException or IOException if the source image is invalid.
        /// </summary>
        /// <returns></returns>
        public static Bitmap GenerateImage(string sourceFile,  NameValueCollection q)
        {
            System.Drawing.Image b = null;
            try
            {
                b = new System.Drawing.Bitmap(sourceFile);
            }
            catch (ArgumentException ae)
            {
                ae.Data.Add("path", sourceFile);
                ae.Data.Add("possiblereason",
                    "File may be corrupted, empty, or may contain a PNG image file with a single dimension greater than 65,535 pixels."
                                                );
                throw ae;

            }

            if (b == null)
            {
                throw new IOException("Could not read the specified image! Image invalid or something.");
            }
            using (b)
            {
                //Aspect ratio
                double woh = 1;
                if (b.Height > 0 && b.Width > 0) woh = (double)b.Width / (double)b.Height;

                //These are used to absolutely specify the bounds. If one is missing, the other is calculated from the aspect ratio
                int width = -1;
                int height = -1;
                //
                int maxwidth = -1;
                int maxheight = -1;

                //fill in vars via querystring
                if (!string.IsNullOrEmpty(q["width"]))
                    int.TryParse(q["width"], out width);
                if (!string.IsNullOrEmpty(q["maxwidth"]))
                    int.TryParse(q["maxwidth"], out maxwidth);


                if (!string.IsNullOrEmpty(q["height"]))
                    int.TryParse(q["height"], out height);
                if (!string.IsNullOrEmpty(q["maxheight"]))
                    int.TryParse(q["maxheight"], out maxheight);


                //Maintain aspect ratio if only one parameter is specified

                if (width > 0)
                {
                    if (height < 0)
                        height = (int)(width / woh);
                }
                else if (height > 0)
                {
                    if (width < 0)
                        width = (int)(height * woh);
                }
                else
                {
                    //Neither specified. infer from maxwidth/maxheight values, using least to maintain aspect ratio.
                    if (maxheight > 0)
                        height = maxheight;
                    if (maxwidth > 0)
                        width = maxwidth;

                    if (width < 0 && height < 0)
                    {
                        //Neither specified. Use original image specs.
                        width = b.Width;
                        height = b.Height;
                    }
                    else
                    {

                        //Constrain to aspect ratio
                        if (height > 0)
                            if (width > (height * woh) || width < 0) // don't worry about the width < 0 loophole. If width < 0, no maxwidth was specified anyway
                                width = (int)(height * woh);
                        if (width > 0)
                            if (height > width / woh || height < 0) // ' ' ' 
                                height = (int)(width / woh);
                    }
                }

                //Apply maxheight, maxwidth bounds. Loses aspect ratio, but this only applies if the user uses width and height, or a combination of width, height, maxwidth, and maxheight.
                if (maxheight > 0)
                    if (height > maxheight)
                        height = maxheight;
                if (maxwidth > 0)
                    if (width > maxwidth)
                        width = maxwidth;



                int safetyWidth = DiskCache.GetMaxWidth();
                int safetyHeight = DiskCache.GetMaxHeight();


                //Upscale check - no point in making images larger (maybe?)
                if (width > b.Width && height > b.Height)
                {
                    width = b.Width;
                    height = b.Height;
                }

                //Safety check. We don't want to allow CPU DOS attacks, or allow negative sizes.
                width = (width > safetyWidth) ? safetyWidth : width;
                height = (height > safetyHeight) ? safetyHeight : height;

                //Too small check
                width = (width < 4) ? 4 : width;
                height = (height < 4) ? 4 : height;


                //New thumbnail image
                System.Drawing.Bitmap thumb = new System.Drawing.Bitmap(width, height);
                //graphics object for new image
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(thumb))
                {
                    //High quality
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                    //Draw and scale the image
                    //if (b.Width == width && b.Height == height)
                    //    g.DrawImageUnscaled(b, 0, 0); //Don't use anymore - actually draws image scaled according to physical dimensions (dpi). Bad naming.
                    //    g.DrawImage(b,0,0) has the same issue.
                    //else
                    //old approach:
                    ///uses the image's built-in thumbnail (not always good quality, but faster)
                    ///System.Drawing.Bitmap thumb = (System.Drawing.Bitmap)b.GetThumbnailImage(width, height, delegate() { return true; }, IntPtr.Zero);
                    

                    g.DrawImage(b, new System.Drawing.Rectangle(0, 0, width, height));

                    //This can be extended to make better overlay system. 
                    //Make sure you don't allow arbitrary paths, since that would bypass the authorization system and security.
                    /*if (!string.IsNullOrEmpty(q["overlay"]))
                    {
                        string overlayFilename = new yrl("~/img/thumbnailOverlay.png").Local;
                        if (new yrl("~/img/thumbnailOverlay.png").FileExists)
                        {
                            System.Drawing.Bitmap overlay = new System.Drawing.Bitmap(overlayFilename);

                            //Scale to take less than half the width and height.
                            int oWidth = overlay.Width;
                            int oHeight = overlay.Height;
                            double oWoh = (double)oWidth / (double)oHeight;
                            int oX = width - oWidth;
                            int oY = height - oHeight;
                            if (oWidth > (width / 2))
                            {
                                oWidth = width / 2;
                                oHeight = (int)(oWidth / oWoh);
                            }
                            if (oHeight > (height / 2))
                            {
                                oHeight = height / 2;
                                oWidth = (int)(oHeight * oWoh);
                            }

                            oY = height - oHeight;
                            oX = width - oWidth;

                            g.DrawImage(overlay, new System.Drawing.Rectangle(oX, oY, oWidth, oHeight));
                            overlay.Dispose();
                        }
                    }
                     */

                    g.Flush();
                    
                    return thumb;
                }
            }
            
        }



    }
}
