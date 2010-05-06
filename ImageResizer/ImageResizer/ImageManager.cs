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
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Reflection;

namespace fbs.ImageResizer
{
    /// <summary>
    /// Provides methods for generating resizied images, and for reading and writing them to disk.
    /// </summary>
    public class ImageManager
    {
        private static ImageManager _bestInstance = null;
        /// <summary>
        /// Allow other classes to override.
        /// </summary>
        /// <param name="replacement"></param>
        public static void RegisterUpgrade(ImageManager replacement){
            _bestInstance = replacement;
        }
        public ImageManager()
        {
        }
        /// <summary>
        /// Looks for AnimatedImageManager and returns an instance of that if possible. Otherwise, returns an 
        /// ImageManager instance.
        /// </summary>
        /// <returns></returns>
        public static ImageManager getBestInstance()
        {
            //Allow the copy&paste plugin of a better ImageManager
            if (_bestInstance == null) _bestInstance = new ImageManager();
            return _bestInstance;

        }
        /// <summary>
        /// Takes sourceFile, resizes it, and saves it to targetFile using the querystring values in request.
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="targetFile"></param>
        /// <param name="request"></param>
        public virtual void BuildImage(string sourceFile, string targetFile, NameValueCollection queryString)
        {
            //Allow AnimatedImageManager to be added without changing code - plugin style
            //Resize image 
            using (Bitmap thumb = BuildImage(sourceFile, queryString))
            {
                //Determines output format, includes code for saving in a variety of formats.
                ImageOutputSettings ios = new ImageOutputSettings(ImageOutputSettings.GetImageFormatFromPhysicalPath(sourceFile),queryString);

                //Open stream and save format.
                System.IO.FileStream fs = new FileStream(targetFile, FileMode.Create, FileAccess.Write);
                using (fs)
                {
                    ios.SaveImage(fs,thumb);
                }
            }
        }

        /// <summary>
        /// Takes sourceFile, resizes it, and saves it to targetFile using the querystring values in request.
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="targetFile"></param>
        /// <param name="request"></param>
        public virtual void BuildImage(VirtualFile sourceFile, string targetFile, NameValueCollection queryString)
        {
            //Allow AnimatedImageManager to be added without changing code - plugin style
            //Resize image 
            using (Bitmap thumb = BuildImage(sourceFile, queryString))
            {
                //Determines output format, includes code for saving in a variety of formats.
                ImageOutputSettings ios = new ImageOutputSettings(ImageOutputSettings.GetImageFormatFromPhysicalPath(sourceFile.VirtualPath), queryString);

                //Open stream and save format.
                System.IO.FileStream fs = new FileStream(targetFile, FileMode.Create, FileAccess.Write);
                using (fs)
                {
                    ios.SaveImage(fs, thumb);
                }
            }
        }


        /// <summary>
        /// Takes sourceFile, resizes it, and saves it to targetFile using the querystring values in request.
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="targetFile"></param>
        /// <param name="request"></param>
        public virtual void BuildImage(HttpPostedFile postedFile, string targetFile, NameValueCollection queryString)
        {
            //Allow AnimatedImageManager to be added without changing code - plugin style
            //Resize image 
            using (Bitmap thumb = BuildImage(postedFile, queryString))
            {
                //Determines output format, includes code for saving in a variety of formats.
                ImageOutputSettings ios = new ImageOutputSettings(ImageOutputSettings.GetImageFormatFromPhysicalPath(postedFile.FileName), queryString);

                //Open stream and save format.
                System.IO.FileStream fs = new FileStream(targetFile, FileMode.Create, FileAccess.Write);
                using (fs)
                {
                    ios.SaveImage(fs, thumb);
                }
            }
        }
        /// <summary>
        /// Returns true if the specified querystring collection uses a resizing command
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public virtual bool HasResizingDirective(NameValueCollection q)
        {
            return IsOneSpecified(q["format"],q["dither"], q["thumbnail"], q["maxwidth"], q["maxheight"],
                q["width"], q["height"],
                q["scale"], q["stretch"], q["crop"], q["page"], q["time"], q["quality"], q["colors"], q["bgcolor"],
                q["rotate"], q["flip"], q["sourceFlip"], q["borderWidth"],
                q["borderColor"], q["paddingWidth"], q["paddingColor"], q["ignoreicc"],
                q["shadowColor"],q["shadowOffset"],q["shadowWidth"],q["frame"],q["page"]);
        }

        /// <summary>
        /// Returns true if one or more of the arguments has a non-null or non-empty value
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private  bool IsOneSpecified(params String[] args)
        {
            foreach (String s in args) if (!string.IsNullOrEmpty(s)) return true;
            return false;
        }



        /// <summary>
        /// Generates a resized bitmap from the specifed source file and the specified querystring. Understands width/height and maxwidth/maxheight.
        /// Throws either an ArgumentException or IOException if the source image is invalid.
        /// Always use ImageOutputSettings to save images, since Image.Save doesn't work well for GIF or PNGs, and needs custom params for Jpegs.
        /// </summary>
        /// 
        /// <returns></returns>
        public virtual Bitmap BuildImage(string sourceFile, NameValueCollection q)
        {
            bool useICM = true;
            if ("true".Equals(q["ignoreicc"], StringComparison.OrdinalIgnoreCase)) useICM = false;



            System.Drawing.Bitmap b = null;
            try
            {
                b = new System.Drawing.Bitmap(sourceFile,useICM);
            }
            catch (ArgumentException ae)
            {
                ae.Data.Add("path", sourceFile);
                ae.Data.Add("possiblereason",
                    "File may be corrupted, empty, or may contain a PNG image file with a single dimension greater than 65,535 pixels.");
                throw ae;
            }
            if (b == null) throw new IOException("Could not read the specified image! Image invalid or something.");
            
            using (b)
            {
                return BuildImage(b,ImageOutputSettings.GetImageFormatFromPhysicalPath(sourceFile) ,q);
            }
        }
        /// <summary>
        /// Generates a resized bitmap from the specifed source file and the specified querystring. Understands width/height and maxwidth/maxheight.
        /// Throws either an ArgumentException or IOException if the source image is invalid.
        /// Always use ImageOutputSettings to save images, since Image.Save doesn't work well for GIF or PNGs, and needs custom params for Jpegs.
        /// </summary>
        /// 
        /// <returns></returns>
        public virtual Bitmap BuildImage(HttpPostedFile postedFile, NameValueCollection q)
        {
            bool useICM = true;
            if ("true".Equals(q["ignoreicc"], StringComparison.OrdinalIgnoreCase)) useICM = false;

            using (Stream s = postedFile.InputStream){

                System.Drawing.Bitmap b = null;
                try
                {
                    b = new System.Drawing.Bitmap(s, useICM);
                }
                catch (ArgumentException ae)
                {
                    ae.Data.Add("path", postedFile.FileName);
                    ae.Data.Add("possiblereason",
                        "File may be corrupted, empty, or may contain a PNG image file with a single dimension greater than 65,535 pixels.");
                    throw ae;
                }
                if (b == null) throw new IOException("Could not read the specified image! Image invalid or something.");
                
                using (b)
                {
                    return BuildImage(b, ImageOutputSettings.GetImageFormatFromPhysicalPath(postedFile.FileName), q);
                }
            }
        }

        /// <summary>
        /// Generates a resized bitmap from the specifed source file and the specified querystring. Understands width/height and maxwidth/maxheight.
        /// Throws either an ArgumentException or IOException if the source image is invalid.
        /// Always use ImageOutputSettings to save images, since Image.Save doesn't work well for GIF or PNGs, and needs custom params for Jpegs.
        /// </summary>
        /// 
        /// <returns></returns>
        public virtual Bitmap BuildImage(VirtualFile file, NameValueCollection q)
        {
            bool useICM = true;
            if ("true".Equals(q["ignoreicc"], StringComparison.OrdinalIgnoreCase)) useICM = false;

            using (Stream s = file.Open())
            {

                System.Drawing.Bitmap b = null;
                try
                {
                    b = new System.Drawing.Bitmap(s, useICM);
                }
                catch (ArgumentException ae)
                {
                    ae.Data.Add("path", file.VirtualPath);
                    ae.Data.Add("possiblereason",
                        "File may be corrupted, empty, or may contain a PNG image file with a single dimension greater than 65,535 pixels.");
                    throw ae;
                }
                if (b == null) throw new IOException("Could not read the specified image! Image invalid or something.");

                using (b)
                {
                    return BuildImage(b, ImageOutputSettings.GetImageFormatFromPhysicalPath(file.VirtualPath), q);
                }
            }
        }



        /// <summary>
        /// Creates a new bitmap of the required size, and draws the specified image (with border, background, padding, and shadow).
        /// Always use ImageOutputSettings to save images, since Image.Save doesn't work well for GIF or PNGs, and needs custom params for Jpegs.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        public virtual Bitmap BuildImage(Bitmap src, ImageFormat originalFormat, NameValueCollection q)
        {
            int page = 0;
            if (!string.IsNullOrEmpty(q["page"]) && !int.TryParse(q["page"], out page))
                page = 0;

            int frame = 0;
            if (!string.IsNullOrEmpty(q["frame"]) && !int.TryParse(q["frame"], out frame))
                frame = 0;

            page--; frame--; //So users can use 1-based numbers

            /* Fixed GIF transparency*/
            //Support page selection in a .tiff document.
            try
            {
                if (page > 0)
                {
                    //Stay on the last frame if out of bounds
                    page = (page >= src.GetFrameCount(FrameDimension.Page)) ? src.GetFrameCount(FrameDimension.Page) - 1 : page;

                    if (page > 0)
                    {
                        src.SelectActiveFrame(FrameDimension.Page, page);
                       //Causes problems: src.MakeTransparent(); 
                    }
                }
                if (frame > 0)
                {
                    if (frame >= src.GetFrameCount(FrameDimension.Time))

                        //Out of bounds.
                        //Use last index
                        frame = src.GetFrameCount(FrameDimension.Time) - 1;

                    if (frame > 0)
                    {
                        src.SelectActiveFrame(FrameDimension.Time, frame);
                       //Causes problems:  src.MakeTransparent();
                    }

                }
            }
            catch (ExternalException) { } //When somebody tries &frame or &page on a single-frame image
            


            return BuildImage(src, new ResizeSettings(q), new ImageSettings(q), new ImageFilter(q),  new ImageOutputSettings(originalFormat, q),new WatermarkSettings(q));
        }



        /// <summary>
        /// Creates a new bitmap of the required size, and draws the specified image (with border, background, padding, and shadow).
        /// Accepts parallelagrams, so rotation and skew is permitted.
        /// Use SelectActiveFrame() to select the right frame prior to calling BuildImage
        /// </summary>
        /// <param name="page">The page or frame. Use 0 for default.</param>
        /// <param name="watermark">Optional, can be null. Plugin for watermarking code</param>
        /// <returns></returns>
        public virtual Bitmap BuildImage(Bitmap src, ResizeSettings resize, ImageSettings opts, ImageFilter adjustments, ImageOutputSettings output, WatermarkSettings watermark)
        {
            if (watermark != null) watermark.ModifySettings(resize, opts,adjustments,output);

            if (resize.sourceFlip != RotateFlipType.RotateNoneFlipNone)
                src.RotateFlip(resize.sourceFlip); //Flipping has to be done on the original - it can't be done as part of the DrawImage or later, after the borders are drawn.

            /*Color background, float borderWidth, Color borderColor,
            float shadowWidth, Color shadowColor, PointF shadowOffset,
            Color paddingColor, Bitmap src, RectangleF sourceArea, PointF[] imageTarget, PointF[] targetSpace, ImageAttributes imageAdjustments)*/


            ResizeSettings.ImageSizingData size = resize.CalculateSizingData(new SizeF(src.Width, src.Height), new SizeF((float)DiskCache.GetMaxWidth(), (float)DiskCache.GetMaxHeight()));

            //Calculate required space for everything
            PointF[] all = size.targetArea;


            //Add required space for border and padding
            if (opts.borderWidth > 0) all = PolygonMath.InflatePoly(all, opts.borderWidth);
            if (opts.paddingWidth > 0) all = PolygonMath.InflatePoly(all, opts.paddingWidth);

            //For later use. The inside of the shadow
            PointF[] insideShadow = all;

            //shadow is trickier
            if (opts.shadowWidth > 0) all = PolygonMath.InflatePoly(all, new float[]{
                Math.Max(0, opts.shadowWidth - opts.shadowOffset.Y),
                Math.Max(0, opts.shadowWidth + opts.shadowOffset.X),
                Math.Max(0, opts.shadowWidth + opts.shadowOffset.Y),
                Math.Max(0, opts.shadowWidth - opts.shadowOffset.X)
            });

            //Find how much we need to move imageArea (and imageTarget) so that all is at 0,0.
            PointF shadowBorderOffset = PolygonMath.GetBoundingBox(all).Location;
            shadowBorderOffset.X *= -1;
            shadowBorderOffset.Y *= -1;

            //Adjust insideShadow
            insideShadow = PolygonMath.InflatePoly(PolygonMath.MovePoly(insideShadow, shadowBorderOffset), new float[]{
                 - opts.shadowOffset.Y,
                 + opts.shadowOffset.X,
                 + opts.shadowOffset.Y,
                 - opts.shadowOffset.X
            });



            //Rebase things so we are starting at 0,0;
            size.imageTarget = PolygonMath.MovePoly(size.imageTarget, shadowBorderOffset);
            size.targetArea = PolygonMath.MovePoly(size.targetArea, shadowBorderOffset);
            //Inflate for padding
            if (opts.paddingWidth > 0) size.targetArea = PolygonMath.InflatePoly(size.targetArea, opts.paddingWidth);




            //Find the size
            SizeF box = PolygonMath.GetBoundingBox(all).Size;

            Bitmap b;
            //Create new bitmap using calculated size. 
            //OLD idea: Potential problem, rounding max crop or leave space. However, floating-point values should only occur on rotations.
            //4-23-09, FALSE, floating point values can occur anytime scaling takes place. 
            //Fixed by using Floor instead of Round
            if (PolygonMath.GetBoundingBox(size.imageTarget).Size.Equals(box))
            {
                //The image is taking the entire space. Round down, as System.Drawing does.
                //June 3: added Math.Max(1,) to prevent Invalid Parameter error on < 1px images. Also need to fix in ResizeSettings.. otherwise
                //the image data won't be put in the right place either.
                b = new Bitmap((int)Math.Max(1,Math.Floor(box.Width)), (int)Math.Max(1,Math.Floor(box.Height)), PixelFormat.Format32bppArgb);

            }
            else
            {
                //June 3: added Math.Max(1,) to prevent Invalid Parameter error on < 1px images.
                //It isn't taking up all the space - Space around the image is expected. Leaving normal rounding in place - flooring would do as much harm as good on average.
                b = new Bitmap((int)Math.Max(1,Math.Round(box.Width)), (int)Math.Max(1,Math.Round(box.Height)), PixelFormat.Format32bppArgb);
            }

            //Create graphics handle
            Graphics g = Graphics.FromImage(b);
            using (g)
            {

                //High quality everthing
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                g.CompositingMode = CompositingMode.SourceOver;

                //If the image doesn't support transparency, we need to fill the background color now.
                Color background = opts.bgcolor;
                if (background == Color.Transparent)
                    //Only set the bgcolor if the image isn't taking the whole area.
                    if (!output.SupportsTransparency && !PolygonMath.GetBoundingBox(size.imageTarget).Size.Equals(box))
                        background = Color.White;
                //Fill background
                if (background != Color.Transparent) //This causes increased aliasing at the edges - i.e., a faint white border that is even more pronounced than usual.
                    g.Clear(background); //Does this work for Color.Transparent? -- 

                Color paddingColor = opts.paddingColor;
                //Inherit color
                if (paddingColor.Equals(Color.Transparent)) paddingColor = opts.bgcolor;


                //Draw shadow
                if (opts.shadowWidth > 0 && opts.shadowColor != Color.Transparent)
                {
                    //Offsets may show inside the shadow - so we have to fix that
                    g.FillPolygon(new SolidBrush(opts.shadowColor),
                        PolygonMath.InflatePoly(insideShadow, 1)); //Inflate 1 for FillPolgyon rounding errors.

                    //Then we can draw the outer gradient
                    DrawOuterGradient(g, insideShadow,
                                    opts.shadowColor, Color.Transparent, opts.shadowWidth);
                }


                //Draw padding around image if needed.
                if (!paddingColor.Equals(opts.bgcolor) && paddingColor != Color.Transparent)
                    g.FillPolygon(new SolidBrush(paddingColor), size.targetArea);


                g.DrawImage(src, PolygonMath.getParallelogram(size.imageTarget), size.sourceRect, GraphicsUnit.Pixel);//, adjustments.getImageAttributes()
               

                //Draw border
                if (opts.borderWidth > 0)
                {
                    Pen p = new Pen(opts.borderColor, opts.borderWidth);
                    p.Alignment = System.Drawing.Drawing2D.PenAlignment.Center; //PenAlignment.Center is the only supported mode.
                    p.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
                    g.DrawPolygon(p, PolygonMath.InflatePoly(size.targetArea,(float)(opts.borderWidth / 2.0))); //I hope GDI rounds the same way as .NET.. Otherwise there may be an off-by-one error..
                }

                //Commit changes.
                g.Flush(FlushIntention.Flush);

                //The last flipping.
                if (resize.flip != RotateFlipType.RotateNoneFlipNone)
                    b.RotateFlip(resize.flip);

                if (watermark != null) watermark.Process(b, g);

                return b;
            }

        }

        /// <summary>
        /// Draws a gradient around the specified polygon. Fades from 'inner' to 'outer' over a distance of 'width' pixels. 
        /// </summary>
        /// <param name="g"></param>
        /// <param name="poly"></param>
        /// <param name="inner"></param>
        /// <param name="outer"></param>
        /// <param name="width"></param>
        public virtual void DrawOuterGradient(Graphics g, PointF[] poly, Color inner, Color outer, float width)
        {
            
            PointF[,] corners = PolygonMath.RoundPoints(PolygonMath.GetCorners(poly, width));
            PointF[,] sides = PolygonMath.RoundPoints(PolygonMath.GetSides(poly, width));
            //Overlapping these causes darker areas... Dont use InflatePoly

            //Paint corners
            for (int i = 0; i <= corners.GetUpperBound(0); i++)
            {
                PointF[] pts = PolygonMath.GetSubArray(corners, i);
                Brush b = PolygonMath.GenerateRadialBrush(inner, outer, pts[0], width + 1);
                
                g.FillPolygon(b, pts);
            }
            //Paint sides
            for (int i = 0; i <= sides.GetUpperBound(0); i++)
            {
                PointF[] pts = PolygonMath.GetSubArray(sides, i);
                LinearGradientBrush b = new LinearGradientBrush(pts[3], pts[0], inner, outer);
                b.SetSigmaBellShape(1);
                b.WrapMode = WrapMode.TileFlipXY;
                g.FillPolygon(b,pts);
            }
        }


        
    }
}
