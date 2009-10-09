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
using System.Drawing.Drawing2D;

namespace fbs.ImageResizer
{
    /// <summary>
    /// Provides methods for generating resizied images, and for reading and writing them to disk.
    /// </summary>
    public class ImageManager
    {
        /// <summary>
        /// Takes sourceFile, resizes it, and saves it to targetFile using the querystring values in request.
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="targetFile"></param>
        /// <param name="request"></param>
        public static void BuildImage(string sourceFile, string targetFile, NameValueCollection queryString)
        {
            //Resize image 
            using (Bitmap thumb = BuildImage(sourceFile, queryString))
            {
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
        /// Generates a resized bitmap from the specifed source file and the specified querystring. Understands width/height and maxwidth/maxheight.
        /// Throws either an ArgumentException or IOException if the source image is invalid.
        /// </summary>
        /// <returns></returns>
        public static Bitmap BuildImage(string sourceFile, NameValueCollection q)
        {
            System.Drawing.Bitmap b = null;
            try
            {
                b = new System.Drawing.Bitmap(sourceFile);
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
        /// Creates a new bitmap of the required size, and draws the specified image (with border, background, padding, and shadow).
        /// </summary>
        /// <param name="src"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        public static Bitmap BuildImage(Bitmap src, ImageFormat originalFormat, NameValueCollection q)
        {
            int page = 0;
            if (!string.IsNullOrEmpty(q["page"]) && !int.TryParse(q["page"], out page))
                page = 0;

            return BuildImage(src, page, new ResizeSettings(q), new ImageSettings(q), new ImageFilter(q), new ImageOutputSettings(originalFormat,q));
        }



        /// <summary>
        /// Creates a new bitmap of the required size, and draws the specified image (with border, background, padding, and shadow).
        /// Accepts parallelagrams, so rotation and skew is permitted.
        /// </summary>
        /// <param name="pageIndex">The page or frame. Use 0 for default.</param>
        /// <returns></returns>
        public static Bitmap BuildImage(Bitmap src, int pageIndex, ResizeSettings resize, ImageSettings opts, ImageFilter adjustments, ImageOutputSettings output)
        {
            //Support page selection in a .tiff document.
            if (pageIndex > 0)
            {
                if (pageIndex >= src.GetFrameCount(FrameDimension.Page))
                {
                    //Out of bounds.
                    //Use last index
                    pageIndex = src.GetFrameCount(FrameDimension.Page) - 1;
                }
                if (pageIndex > 0)
                {
                    src.SelectActiveFrame(FrameDimension.Page, pageIndex);
                }
            }
            if (resize.flip != RotateFlipType.RotateNoneFlipNone)
                src.RotateFlip(resize.flip); //Flipping has to be done on the original - it can't be done as part of the DrawImage or later, after the borders are drawn.
            
            /*Color background, float borderWidth, Color borderColor,
            float shadowWidth, Color shadowColor, PointF shadowOffset,
            Color paddingColor, Bitmap src, RectangleF sourceArea, PointF[] imageTarget, PointF[] targetSpace, ImageAttributes imageAdjustments)*/
            
            ResizeSettings.ImageSizingData size = resize.CalculateSizingData(new SizeF(src.Width,src.Height),new SizeF((float)DiskCache.GetMaxWidth(),(float)DiskCache.GetMaxHeight()));
            //Inflate for padding
            size.targetArea = PolygonMath.InflatePoly(size.targetArea, opts.paddingWidth);

            //Calculate required space:
            RectangleF box = PolygonMath.GetBoundingBox(size.targetArea);
            //Add required space for border and padding
            box.X -= opts.borderWidth;
            box.Y -=  opts.borderWidth;
            box.Width += opts.borderWidth;
            box.Height += opts.borderWidth;
            //And shadow
            if (opts.shadowWidth > 0)
            {
                box.X += Math.Max(0, opts.shadowWidth - opts.shadowOffset.X);
                box.Y += Math.Max(0, opts.shadowWidth - opts.shadowOffset.Y);
                box.Width += Math.Max(0, opts.shadowWidth + opts.shadowOffset.X);
                box.Height += Math.Max(0, opts.shadowWidth + opts.shadowOffset.Y);
            }


            //Rebase things so we are starting at 0,0;
            size.imageTarget = PolygonMath.MovePoly(size.imageTarget, new PointF(-box.X, -box.Y));
            size.targetArea = PolygonMath.MovePoly(size.targetArea, new PointF(-box.X, -box.Y));
            box.X = 0; box.Y = 0;

            //Create new bitmap using calculated size
            Bitmap b = new Bitmap((int)Math.Round(box.Width), (int)Math.Round(box.Height), PixelFormat.Format32bppArgb);

            //Create graphics handle
            Graphics g = Graphics.FromImage(b);
            using (g)
            {

                //High quality everthing
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;

                //If the image doesn't support transparency, we need to fill the background color now.
                Color background = opts.bgcolor;
                if (background == Color.Transparent && !output.SupportsTransparency) background = Color.White;
                //Fill background
                g.Clear(background); //Does this work for Color.Transparent?

                Color paddingColor = opts.paddingColor;
                //Inherit color
                if (paddingColor.Equals(Color.Transparent)) paddingColor = opts.bgcolor;


                //Draw shadow
                if (opts.shadowWidth > 0)
                {
                    DrawOuterGradient(g, PolygonMath.MovePoly(PolygonMath.InflatePoly(size.targetArea, opts.borderWidth + opts.paddingWidth), opts.shadowOffset),
                                    opts.shadowColor, Color.Transparent, opts.shadowWidth);
                }


                //Draw padding around image if needed.
                if (!paddingColor.Equals(opts.bgcolor) && paddingColor != Color.Transparent)
                    g.FillPolygon(new SolidBrush(paddingColor), size.targetArea);


                //Draw image
                g.DrawImage(src, PolygonMath.getParallelogram(size.imageTarget), size.sourceRect, GraphicsUnit.Pixel, adjustments.getImageAttributes());


                //Draw border
                if (opts.borderWidth > 0)
                {
                    Pen p = new Pen(opts.borderColor, opts.borderWidth);
                    p.Alignment = System.Drawing.Drawing2D.PenAlignment.Outset;
                    p.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                    g.DrawPolygon(p, size.targetArea);
                }
                //Commit changes.
                g.Flush(FlushIntention.Flush);
            }

            return b;
        }

        /// <summary>
        /// Draws a gradient around the specified polygon. Fades from 'inner' to 'outer' over a distance of 'width' pixels. 
        /// </summary>
        /// <param name="g"></param>
        /// <param name="poly"></param>
        /// <param name="inner"></param>
        /// <param name="outer"></param>
        /// <param name="width"></param>
        public static void DrawOuterGradient(Graphics g, PointF[] poly, Color inner, Color outer, float width)
        {
            PointF[,] corners = PolygonMath.GetCorners(poly, width);
            PointF[,] sides = PolygonMath.GetSides(poly, width);

            //Paint corners
            for (int i = 0; i <= corners.GetUpperBound(0); i++)
            {
                Brush b = PolygonMath.GenerateRadialBrush(inner, outer, corners[i, 0], width);
                g.FillPolygon(b, PolygonMath.GetSubArray(corners, i));
            }
            //Paint sides
            for (int i = 0; i <= sides.GetUpperBound(0); i++)
            {
                LinearGradientBrush b = new LinearGradientBrush(sides[i, 3], sides[i, 0], inner, outer);
                b.SetSigmaBellShape(1);
                b.WrapMode = WrapMode.TileFlipXY;
                g.FillPolygon(b,PolygonMath.GetSubArray(sides, i));
            }
        }


    }
}
