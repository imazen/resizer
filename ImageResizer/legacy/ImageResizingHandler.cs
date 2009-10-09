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
namespace fbs.Handlers
{
    /// <summary>
    /// Summary description for ImageResizingHandler
    /// </summary>
    public class ImageResizingHandler : IHttpHandler
    {
        public ImageResizingHandler()
        {
            
        }

        #region IHttpHandler Members

        /// <summary>
        /// True
        /// </summary>
        public bool IsReusable
        {
            get
            {
                return true;
                
            }
        }

        private static void LogWarning(String message)
        {
            HttpContext.Current.Trace.Warn("ImageResizer", message);
        }
        private static void LogException(Exception e)
        {
            HttpContext.Current.Trace.Warn("ImageResizer", e.Message, e);// Event.CreateExceptionEvent(e).SaveAsync();
        }

        public void ProcessRequest(HttpContext context)
        {
            Stopwatch s = new Stopwatch();
            s.Start();
            ProcessRequestInternal(context);
            s.Stop();
            //Should never take this long...
            if (s.ElapsedMilliseconds > 100)
            {
                LogWarning("Resizing request took " + s.ElapsedMilliseconds.ToString() + " milliseconds");
            }
        }

        /// <summary>
        /// Returns a MemoryStream containing the resized image data requested by the specified yrl. Understands width/height and maxwidth/maxheight
        /// </summary>
        /// <param name="current"></param>
        /// <param name="outputType"></param>
        /// <returns></returns>
        public static MemoryStream GenerateImage(yrl current, ImageFormat outputType)
        {
            //TODO. Infer image type. Make width and height maxwidth/maxheight
            //TODO. Handle straight image requests properly, on IIS5-7
            //ADD Jpeg quality setting
            if (!current.FileExists)
            {
                throw new FileNotFoundException("Cannot locate the specified image. Resizing failed.", current.Local);
            }
            System.Drawing.Image b = null;
            try
            {
                b = new System.Drawing.Bitmap(current.Local);
            }
            catch (ArgumentException ae)
            {
                ae.Data.Add("path", current.Local);
                ae.Data.Add("possiblereason",   
                    "File may be corrupted, empty, or may contain a PNG image file with a single dimension greater than 65,535 pixels."
                                                );
                throw ae;

            }

            if (b == null)
            {
                throw new IOException("Could not read the specified image! Image invalid or something.");
            }

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
            if (!string.IsNullOrEmpty(current.QueryString["width"]))
                int.TryParse(current.QueryString["width"], out width);
            if (!string.IsNullOrEmpty(current.QueryString["maxwidth"]))
                int.TryParse(current.QueryString["maxwidth"], out maxwidth);

            
            if (!string.IsNullOrEmpty(current.QueryString["height"]))
                int.TryParse(current.QueryString["height"], out height);
            if (!string.IsNullOrEmpty(current.QueryString["maxheight"]))
                int.TryParse(current.QueryString["maxheight"], out maxheight);


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



            int safetyWidth = GetMaxWidth();
            int safetyHeight = GetMaxHeight();


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
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(thumb);
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
            g.DrawImage(b, new System.Drawing.Rectangle(0, 0, width, height));

            b.Dispose();

            //This can be extended to make better overlay system. 
            //Make sure you don't allow arbitrary paths, since that would bypass the authorization system and security.
            /*if (!string.IsNullOrEmpty(current.QueryString["overlay"]))
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


            //Flush operations
            g.Flush();
            g.Dispose();
            //old approach:
            ///uses the image's built-in thumbnail (not always good quality, but faster)
            ///System.Drawing.Bitmap thumb = (System.Drawing.Bitmap)b.GetThumbnailImage(width, height, delegate() { return true; }, IntPtr.Zero);

            //Clear whatever might have been sent before




            //Save to a seekable memory stream, then write out to the output stream
            System.IO.MemoryStream ms = new System.IO.MemoryStream();

            //Jpegs require special parameters to get a good quality/file size balance. 90 works good
            if (outputType == System.Drawing.Imaging.ImageFormat.Jpeg)
            {
                System.Drawing.Imaging.ImageCodecInfo[] info = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
                System.Drawing.Imaging.EncoderParameters encoderParameters;
                encoderParameters = new System.Drawing.Imaging.EncoderParameters(1);
                encoderParameters.Param[0] = new System.Drawing.Imaging.EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 90L);

                thumb.Save(ms, info[1], encoderParameters);
            }
            else
            {
                thumb.Save(ms, outputType);
            }
            thumb.Dispose();
            return ms;
        }

        internal static void ProcessRequestInternal(HttpContext context)
        {
            yrl current = yrl.Current;


            //Determine requested output type. Default to jpeg.
            ImageFormat outType = GetOutputType(current);


            //Determine content-type string;
            string contentType = "image/jpeg";

            switch (GetOutputExtension(current))
            {
                case "png":
                    contentType = "image/x-png";
                    break;
                case "gif":
                    contentType = "image/gif";
                    break;

            }

            //Clear previous output
            context.Response.Clear();
            context.Response.ContentType = contentType;
            context.Response.AddFileDependency(current.Local);



            //Unique identifier for this image resize request
            string identifier = current.ToString().ToLower().Trim();

            //Disk caching is good for images because they change much less often than the application restarts.

            //If a valid disk-cached item exists, send it back instead. 
            if (CheckDiskCache(identifier, current.Local))
            {
                context.Response.AddFileDependency(GetFilenameFromId(identifier));

                context.Response.Cache.SetExpires(DateTime.Now.AddHours(24));
                //Enables in-memory caching
                context.Response.Cache.SetCacheability(HttpCacheability.Public);
                context.Response.Cache.SetLastModifiedFromFileDependencies();
                context.Response.Cache.SetValidUntilExpires(false);

                //Only return if successful
                if (SendDiskCache(context, identifier))
                {
                    return;
                }

            }
            //Only continue if file must be re-created

            context.Response.Cache.SetExpires(DateTime.Now.AddHours(24));
            //Enables in-memory caching, client, and gateway caching
            context.Response.Cache.SetCacheability(HttpCacheability.Public);

            context.Response.Cache.SetLastModifiedFromFileDependencies();
            context.Response.Cache.SetValidUntilExpires(true);

            //Event.CreateWarningEvent("Resizing on-the-fly: " + current.ToString()).Save();

            MemoryStream ms = GenerateImage(current, outType);
            ms.WriteTo(context.Response.OutputStream);

            //Update the disk cache with the new data.
            UpdateDiskCache(ms, identifier, System.IO.File.GetLastWriteTimeUtc(current.Local));

            ms.Close();

        }

     
        /// <summary>
        /// Returns the appropriate ImageFormat value for the specified request. Looks at ?thumbnail=jpg etc.
        /// </summary>
        /// <param name="requestUrl"></param>
        /// <returns></returns>
        public static System.Drawing.Imaging.ImageFormat GetOutputType(yrl requestUrl)
        {
            System.Drawing.Imaging.ImageFormat outputType = System.Drawing.Imaging.ImageFormat.Jpeg;

            switch (requestUrl.QueryString["thumbnail"].ToLower().Trim())
            {
                case "png":
                    return System.Drawing.Imaging.ImageFormat.Png;
                case "gif":
                    return System.Drawing.Imaging.ImageFormat.Gif;
            }

            return ImageFormat.Jpeg;
        }
        /// <summary>
        /// Returns the appropriate file extension for the specified request. Looks at ?thumbnail=jpg etc. Usually matches the value of 'thumbnail'
        /// </summary>
        /// <param name="requestUrl"></param>
        /// <returns></returns>
        public static string GetOutputExtension(yrl requestUrl)
        {

            switch (requestUrl.QueryString["thumbnail"].ToLower().Trim())
            {
                case "png":
                    return "png";
                case "gif":
                    return "gif";
                default:
                    return "jpg";
            }
        }

        #endregion
    }


}