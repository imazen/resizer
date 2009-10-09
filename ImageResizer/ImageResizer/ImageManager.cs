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
        /// Returns true if the specified querystring collection uses a resizing command
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static bool HasResizingDirective(NameValueCollection q)
        {
            return IsOneSpecified(q["format"], q["thumbnail"], q["maxwidth"], q["maxheight"],
                q["width"], q["height"],
                q["scale"], q["stretch"], q["crop"], q["page"], q["quality"], q["colors"], q["bgcolor"],
                q["rotate"], q["flip"], q["sourceFlip"], q["paddingWidth"], q["paddingColor"], q["ignoreicc"]);
        }

        /// <summary>
        /// Returns true if one or more of the arguments has a non-null or non-empty value
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static bool IsOneSpecified(params String[] args)
        {
            foreach (String s in args) if (!string.IsNullOrEmpty(s)) return true;
            return false;
        }



        /// <summary>
        /// Generates a resized bitmap from the specifed source file and the specified querystring. Understands width/height and maxwidth/maxheight.
        /// Throws either an ArgumentException or IOException if the source image is invalid.
        /// 
        /// </summary>
        /// 
        /// <returns></returns>
        public static Bitmap BuildImage(string sourceFile, NameValueCollection q)
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
            if (resize.sourceFlip != RotateFlipType.RotateNoneFlipNone)
                src.RotateFlip(resize.sourceFlip); //Flipping has to be done on the original - it can't be done as part of the DrawImage or later, after the borders are drawn.
            
            /*Color background, float borderWidth, Color borderColor,
            float shadowWidth, Color shadowColor, PointF shadowOffset,
            Color paddingColor, Bitmap src, RectangleF sourceArea, PointF[] imageTarget, PointF[] targetSpace, ImageAttributes imageAdjustments)*/
            

            ResizeSettings.ImageSizingData size = resize.CalculateSizingData(new SizeF(src.Width,src.Height),new SizeF((float)DiskCache.GetMaxWidth(),(float)DiskCache.GetMaxHeight()));
         
            //Calculate required space for everything
            PointF[] all = size.targetArea;


            //Add required space for border and padding
            if (opts.borderWidth > 0) all = PolygonMath.InflatePoly(all, opts.borderWidth);
            if (opts.paddingWidth > 0) all = PolygonMath.InflatePoly(size.targetArea, opts.paddingWidth);
            
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
            insideShadow = PolygonMath.InflatePoly(PolygonMath.MovePoly(insideShadow,shadowBorderOffset), new float[]{
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
                        PolygonMath.InflatePoly(insideShadow,1)); //Inflate 1 for FillPolgyon rounding errors.

                    //Then we can draw the outer gradient
                    DrawOuterGradient(g, insideShadow,
                                    opts.shadowColor, Color.Transparent, opts.shadowWidth);
                }


                //Draw padding around image if needed.
                if (!paddingColor.Equals(opts.bgcolor) && paddingColor != Color.Transparent)
                    g.FillPolygon(new SolidBrush(paddingColor), size.targetArea);

                if (!PolygonMath.GetBoundingBox(size.imageTarget).Size.Equals(box))
                {
                    //Inflate half a pixel to remove white border caused by GDI+ error
                   //Doesn't work! size.imageTarget = PolygonMath.InflatePoly(size.imageTarget, 1F);
                  /* Doesn't work either:
                   * size.sourceRect.X++;
                    size.sourceRect.Y++;
                    size.sourceRect.Width -= 2;
                    size.sourceRect.Height -= 2;
                   */
                }

                //Must use == to compart SizeF and Size ... Equals() failes to compare properly
               /*Doesn't help either: if (PolygonMath.GetBoundingBox(size.imageTarget).Size.Equals(box) && size.sourceRect.Size == src.Size)
                {
                    g.DrawImage(src, PolygonMath.ToRectangle(PolygonMath.GetBoundingBox(size.imageTarget)));
                   
                }
                else
                {*/
                    //Draw image
                    g.DrawImage(src, PolygonMath.getParallelogram(size.imageTarget), size.sourceRect, GraphicsUnit.Pixel);//, adjustments.getImageAttributes()
                //}

                //Draw border
                if (opts.borderWidth > 0)
                {
                    Pen p = new Pen(opts.borderColor, opts.borderWidth);
                    p.Alignment = System.Drawing.Drawing2D.PenAlignment.Right;
                    p.LineJoin = System.Drawing.Drawing2D.LineJoin.Miter;
                    g.DrawPolygon(p, size.targetArea);
                }

                //Commit changes.
                g.Flush(FlushIntention.Flush);

                //The last flipping.
                if (resize.flip != RotateFlipType.RotateNoneFlipNone)
                    b.RotateFlip(resize.flip);


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
        public static void DrawOuterGradient(Graphics g, PointF[] poly, Color inner, Color outer, float width)
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
