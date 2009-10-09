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
using System.Collections.Generic;
using System.Text;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
namespace fbs.ImageResizer
{
    /// <summary>
    /// Extracts all of the resizing, cropping, stretching, rotation, and flipping settings.
    /// Merges the data in CalculateSizingData and returns the source rect, target poly, and target area poly. Flipping must be done separately.
    /// </summary>
    public class ResizeSettings
    {
        public ResizeSettings() { }
        public ResizeSettings(NameValueCollection q) { parseFromQuerystring(q); }
        //width,height,maxheight,maxwidth
        //crop=none (default) (letterboxes image instead of o)
        //crop=auto (minimally crops to preserve aspect ratio)
        //crop=(x,y,width,height) (crops the source image to the specified rectangle)
        //stretch=proportionally (default)
        //stretch=fill 

        //rotate=degrees
   
        /// <summary>
        /// Parses lists in the form "3,4,5,2,5" and "(3,4,40,50)". If a number cannot be parsed (i.e, number 2 in "5,,2,3") defaultValue is used.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static double[] parseList(string text, double defaultValue){
            text = text.Trim(' ','(',')');
            string[] parts = text.Split(new char[]{','}, StringSplitOptions.None);
            double[] vals = new double[parts.Length];
            for(int i = 0; i < parts.Length;i++)
            {
                if (!double.TryParse(parts[i],out vals[i]))
                    vals[i] = defaultValue;
            }
            return vals;
        }
        public static  int getInt(NameValueCollection q, string name, int defaultValue)
        {
            int temp = defaultValue;
            if (!string.IsNullOrEmpty(q[name]))
                int.TryParse(q[name], out temp);
            return temp;
        }
        public static float getFloat(NameValueCollection q, string name, float defaultValue)
        {
            float temp = defaultValue;
            if (!string.IsNullOrEmpty(q[name]))
                float.TryParse(q[name], out temp);
            return temp;
        }
        //If width and height are used, then upscaling will occur.
        /// Understands width, height, maxwidth, maxheight, rotate=deg, 
        /// stretch=fill, (stretches images)
        /// stretch=proportionally(default) (letterboxes image with background color instead of stretching), 
        /// crop=none (default)
        /// crop=auto (minimally crops to preserve aspect ratio), and
        /// crop=(x,y,x2,y2) (crops the source image to the specified rectangle)
        public void parseFromQuerystring(NameValueCollection q)
        {
            this.width = getFloat(q, "width", this.width);
            this.height = getFloat(q, "height", this.height);
            this.maxwidth = getFloat(q, "maxwidth", this.maxwidth);
            this.maxheight = getFloat(q, "maxheight", this.maxheight);
            this.rotate = (double)getFloat(q, "rotate", (float)this.rotate);

            if (q["stretch"] != null)
            {
                if (q["stretch"].Equals("fill", StringComparison.OrdinalIgnoreCase))
                    this.stretch = StretchMode.Fill;
                else if (q["stretch"].Equals("proportionally", StringComparison.OrdinalIgnoreCase))
                    this.stretch = StretchMode.Proportionally;
                else
                {
                    //invalid value
                }
            }
            if (q["crop"] != null)
            {
                string c = q["crop"];
                if (c.Equals("none", StringComparison.OrdinalIgnoreCase))
                    this.crop = CropMode.None;
                else if (c.Equals("auto", StringComparison.OrdinalIgnoreCase))
                    this.crop = CropMode.Auto;
                else
                {
                    double[] coords = parseList(c, double.NaN);
                    if (coords.Length == 4)
                    {
                        this.crop = CropMode.Custom;
                        this.customCropCoordinates = coords;
                    }
                }
            }
            if (q["scale"] != null)
            {
                if (q["scale"].Equals("both", StringComparison.OrdinalIgnoreCase))
                    this.scale = ScaleMode.Both;
                else if (q["scale"].Equals("upscaleonly", StringComparison.OrdinalIgnoreCase))
                    this.scale = ScaleMode.UpscaleOnly;
                else if (q["scale"].Equals("downscaleonly", StringComparison.OrdinalIgnoreCase))
                    this.scale = ScaleMode.DownscaleOnly;
                else
                {
                    //invalid value
                }
            }

            string sFlip = q["flip"];
            if (!string.IsNullOrEmpty(sFlip))
            {
                flip = parseFlip(sFlip);
            }


            string ssFlip = q["sourceFlip"];
            if (!string.IsNullOrEmpty(ssFlip))
            {
                sourceFlip = parseFlip(ssFlip);
            }
        }
        /// <summary>
        /// Returns RotateNoneFlipNone if not a recognize value.
        /// </summary>
        /// <param name="sFlip"></param>
        /// <returns></returns>
        private static RotateFlipType parseFlip(string sFlip)
        {
            
            if (!string.IsNullOrEmpty(sFlip))
            {
                if ("none".Equals(sFlip, StringComparison.OrdinalIgnoreCase))
                    return RotateFlipType.RotateNoneFlipNone;
                else if (sFlip.Equals("h", StringComparison.OrdinalIgnoreCase))
                    return RotateFlipType.RotateNoneFlipX;
                else if (sFlip.Equals("v", StringComparison.OrdinalIgnoreCase))
                    return RotateFlipType.RotateNoneFlipY;
                else if (sFlip.Equals("both", StringComparison.OrdinalIgnoreCase))
                    return RotateFlipType.RotateNoneFlipXY;
                // else
                // throw new ArgumentOutOfRangeException("flip", "Must be one of the following: none, h, v, or both. Found " + sFlip);
            }
            return RotateFlipType.RotateNoneFlipNone;
        }
        public float width = -1;
        public float height = -1;
        public float maxwidth = -1;
        public float maxheight = -1;
        /// <summary>
        /// Degrees of rotation to apply
        /// </summary>
        public double rotate = 0;

        /// <summary>
        /// Applied last, after all effects. 
        /// </summary>
        public RotateFlipType flip = RotateFlipType.RotateNoneFlipNone;
        /// <summary>
        /// Flips the source image prior to processing. 
        /// </summary>
        public RotateFlipType sourceFlip = RotateFlipType.RotateNoneFlipNone;


        public enum CropMode
        {
            /// <summary>
            /// Default. No cropping - uses letterboxing if strecth=proportionally and both width and height are specified.
            /// </summary>
            None,
            /// <summary>
            /// Minimally crops to preserve aspect ratio if stretch=proportionally.
            /// </summary>
            Auto,
            /// <summary>
            /// Crops using the custom crop rectangle. Letterboxes if stretch=proportionally and both widht and height are specified.
            /// </summary>
            Custom
        }
        /// <summary>
        /// Crop settings. Defaults to None - letterboxing is used if stretch=p. 
        /// </summary>
        public CropMode crop = CropMode.None;
        public double[] customCropCoordinates = null;
        public RectangleF getCustomCropSourceRect(SizeF imageSize)
        {
            double[] c = customCropCoordinates;
            double x1 = c[0],  y1 = c[1],  x2 = c[2],  y2 = c[3];

            //allow negative offsets 
            if (x1 < 0) x1 += imageSize.Width;
            if (y1 < 0) y1 += imageSize.Height;
            if (x2 <= 0) x2 += imageSize.Width;
            if (y2 <= 0) y2 += imageSize.Height;
            

            //Require box stay in bounds.
            if (x1 < 0) x1 = 0; if (x2 < 0) x2 = 0;
            if (y1 < 0) y1 = 0; if (y2 < 0) y2 = 0;
            if (x1 > imageSize.Width) x1 = imageSize.Width;
            if (x2 > imageSize.Width) x2 = imageSize.Width;
            if (y1 > imageSize.Height) y1 = imageSize.Height;
            if (y2 > imageSize.Height) y2 = imageSize.Height;

            //Require positive width and height.
            if (x2 <= x1 || y2 <= y1)
            {
                //Use original dimensions - can't recover from negative width or height in cropping rectangle
                return new RectangleF(new PointF(0, 0), imageSize);
            }

            return new RectangleF((float)x1, (float)y1, (float)(x2 - x1), (float)(y2 - y1));
        }
       
        public enum StretchMode
        {
            /// <summary>
            /// Maintains aspect ratio. Default.
            /// </summary>
            Proportionally,
            /// <summary>
            /// Skews image to fit the new aspect ratio defined by 'width' and 'height'
            /// </summary>
            Fill
        }
        /// <summary>
        /// Whether to preserve aspect ratio or stretch.
        /// </summary>
        public StretchMode stretch = StretchMode.Proportionally;

        public enum ScaleMode
        {
            /// <summary>
            /// The default. Only downsamples images - never enlarges. If an image is smaller than 'width' and 'height', the image coordinates are used instead.
            /// </summary>
            DownscaleOnly,
            /// <summary>
            /// Only upscales (zooms) images - never downsamples except to meet web.config restrictions. If an image is larger than 'width' and 'height', the image coordinates are used instead.
            /// </summary>
            UpscaleOnly,
            /// <summary>
            /// Upscales and downscales images according to 'width' and 'height', within web.config restrictions.
            /// </summary>
            Both
        }
        /// <summary>
        /// Whether to downscale, upscale, or allow both on images
        /// </summary>
        public ScaleMode scale = ScaleMode.DownscaleOnly;


        public struct ImageSizingData
        {
            /// <summary>
            /// The rectangular area of the original image to use
            /// </summary>
            public RectangleF sourceRect;
            /// <summary>
            /// The polygon on the new image to draw the image to. All 4 points are clockwise.
            /// </summary>
            public PointF[] imageTarget;
            /// <summary>
            /// The polygon space that will be required (includes letterboxing space). All 4 points are clockwise.
            /// </summary>
            public PointF[] targetArea;
        }

     
        /// <summary>
        /// Calculates the source rectangle, target poly, and target space poly from the querystring instructions.
       
        /// </summary>
        /// <param name="imageSize">The dimensions of the source image</param>
        /// <param name="q">The maximum (unrotated) bounds of the image.</param>
        /// <returns></returns>
        public  ImageSizingData CalculateSizingData(SizeF imageSize, SizeF finalSizeBounds)
        {
            //Use the crop size if present.
            SizeF originalImageSize = imageSize;
            if (crop == CropMode.Custom)
            {
                imageSize = this.getCustomCropSourceRect(imageSize).Size;
                if (imageSize.IsEmpty) throw new Exception("You must specify a custom crop rectange if crop=custom");
            }

            //Aspect ratio of the image
            double imageRatio = imageSize.Width / imageSize.Height;

            //Was any dimension specified?
            bool dimensionSpecified = (this.width > 0 || this.height > 0 || this.maxheight > 0 || this.maxwidth > 0);


            //The target size for the image 
            SizeF targetSize = new SizeF(-1,-1);
            SizeF areaSize = new SizeF(-1, -1);
            if (!dimensionSpecified)
            {
                areaSize = targetSize = imageSize; //No dimension - use original size if possible - within web.config bounds.
                //Checks against the web.config bounds ('finalSizeBounds')
                if (!PolygonMath.FitsInside(targetSize, finalSizeBounds))
                {
                    //Scale down to fit. Doesn't matter what the scale setting is... No dimensions were specified.
                    areaSize = targetSize = PolygonMath.ScaleInside(targetSize, finalSizeBounds);
                }

            }else{
                //A dimension was specified. 
                //We first calculate the largest size the image can be under the width/height/maxwidth/maxheight restrictions.
                //- pretending stretch=fill and scale=both

                //Temp vars - results stored in targetSize and areaSize
                double width = this.width; 
                double height = this.height;
                double maxwidth = this.maxwidth;
                double maxheight = this.maxheight;

                //Eliminate cases where both a value and a max value are specified.
                if (maxwidth > 0 && width > 0)
                {
                    width = Math.Min(maxwidth, width); 
                    maxwidth = -1;
                }
                if (maxheight > 0 && height > 0)
                {
                    height = Math.Min(maxheight, height);
                    maxheight = -1;
                }
                //Do sizing logic

                if (width > 0 || height > 0)
                {   
                    //If only one is specified, calculate the other from 
                    if (width > 0)
                    {
                        if (height < 0) height = (float)(width / imageRatio);
                    }
                    else if (height > 0)
                    {
                        if (width < 0) width = (float)(height * imageRatio);
                    }
                    //Store result
                    targetSize = new SizeF((float)width,(float) height);
                    //Apply maxwidth/maxheight to result if present. Uses aspect ratio from width and height if only one is present. 
                    //A maxwidth and height or maxheight and width values will behave like maxwidth and maxheight always.
                    if (maxwidth > 0 || maxheight > 0)
                    {
                        double userWoh = width / height;
                        //Calculate the missing max bounds (if one *is* missing), using aspect ratio from 'width' and 'height'
                        if (maxheight > 0 && maxwidth <= 0)
                            maxwidth = maxheight * userWoh;
                        else if (maxwidth > 0 && maxheight <= 0)
                            maxheight = maxwidth / userWoh;
                        //Scale to fit inside the bounds
                        targetSize = PolygonMath.ScaleInside(targetSize, new SizeF((float)maxwidth, (float)maxheight));

                    }
                }
                else
                {
                    //Calculate the missing max bounds (if one *is* missing), using aspect ratio of the image
                    if (maxheight > 0 && maxwidth <= 0)
                        maxwidth = maxheight * imageRatio;
                    else if (maxwidth > 0 && maxheight <= 0)
                        maxheight = maxwidth / imageRatio;

                    //Scale image coords to fit.
                    targetSize = PolygonMath.ScaleInside(imageSize, new SizeF((float)maxwidth, (float)maxheight));

                }

                //We now have targetSize. targetSize will only be a different aspect ratio if both 'width' and 'height' are specified.

                //Checks against the web.config bounds ('finalSizeBounds')
                if (!PolygonMath.FitsInside(targetSize, finalSizeBounds))
                {
                    //Scale down to fit using existing aspect ratio.
                    targetSize = PolygonMath.ScaleInside(targetSize, finalSizeBounds);
                }
                //This will be the area size also
                areaSize = targetSize; 

                //Now do scale=proportionally check. Set targetSize=imageSize and make it fit within areaSize using ScaleInside.
                if (stretch == StretchMode.Proportionally)
                {
                    targetSize = PolygonMath.ScaleInside(imageSize, areaSize);
                }

                //Now do upscale/downscale checks. If they take effect, set targetSize to imageSize
                if (scale == ScaleMode.DownscaleOnly)
                {
                    if (PolygonMath.FitsInside(imageSize, targetSize))
                    {
                        //The image is smaller or equal to its target polygon. Use original image coordinates instead.

                        if (!PolygonMath.FitsInside(imageSize, finalSizeBounds)) //Check web.config 'finalSizeBounds' and scale to fit.
                            areaSize = targetSize = PolygonMath.ScaleInside(imageSize, finalSizeBounds);
                        else
                            areaSize = targetSize = imageSize;
                    }
                }
                else if (scale == ScaleMode.UpscaleOnly)
                {
                    if (!PolygonMath.FitsInside(imageSize, targetSize))
                    {
                        //The image is larger than its target. Use original image coordintes instead
                        if (!PolygonMath.FitsInside(imageSize, finalSizeBounds)) //Check web.config 'finalSizeBounds' and scale to fit.
                            areaSize = targetSize = PolygonMath.ScaleInside(imageSize, finalSizeBounds);
                        else
                            areaSize = targetSize = imageSize;

                    }
                }
                
            }
            
            ImageSizingData isd = new ImageSizingData();
            //Determine the source rectangle
            isd.sourceRect = (this.crop == CropMode.Custom) ? this.getCustomCropSourceRect(originalImageSize) : new RectangleF(new PointF(0,0),imageSize);
            //Autocrop
            if (this.crop == CropMode.Auto && this.stretch == StretchMode.Proportionally)
            {
                //Determine the source size
                SizeF sourceSize = PolygonMath.ScaleInside(areaSize, imageSize);
                //Center
                isd.sourceRect = new RectangleF((imageSize.Width - sourceSize.Width) / 2, (imageSize.Height - sourceSize.Height) / 2, sourceSize.Width, sourceSize.Height);
                //Restore targetSize to match areaSize
                targetSize = areaSize;
            }
            isd.imageTarget = PolygonMath.NormalizePoly(PolygonMath.RotatePoly(PolygonMath.ToPoly(new RectangleF(new PointF(0, 0), targetSize)), this.rotate));
            isd.targetArea = PolygonMath.NormalizePoly(PolygonMath.RotatePoly(PolygonMath.ToPoly(new RectangleF(new PointF(0, 0), areaSize)), this.rotate));
            isd.imageTarget = PolygonMath.CenterInside(isd.imageTarget, isd.targetArea);
            return isd;
        }


    }
}
