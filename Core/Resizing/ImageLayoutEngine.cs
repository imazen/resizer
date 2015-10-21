using ImageResizer.ExtensionMethods;
using ImageResizer.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageResizer.Resizing
{
    /// <summary>
    /// Provides a subset of layout logic; specifically determining the crop window, target image size, and (initial) target canvas size.
    /// </summary>
    public class ImageLayoutEngine
    {
        //Input: settings, copyrect, originalsize
        //output: copyrect, imagesize, imagearea

        Size originalSize;
        RectangleF copyRect;
        SizeF targetSize;
        SizeF areaSize;

        public ImageLayoutEngine(Size originalSize, RectangleF cropWindow)
        {
            this.originalSize = originalSize;
            this.copyRect = cropWindow;
        }


        public void ApplyInstructions(Instructions i)
        {
            ApplySettings(new ResizeSettings(i));
        }

        private RectangleF determineManualCropWindow(ResizeSettings settings)
        {
            RectangleF cropWindow = copyRect;
            if (cropWindow.IsEmpty)
            {
                //Use the crop size if present.
                cropWindow = new RectangleF(new PointF(0, 0), originalSize);
                if (settings.GetList<double>("crop", 0, 4) != null)
                {
                    cropWindow = PolygonMath.ToRectangle(settings.getCustomCropSourceRect(originalSize)); //Round the custom crop rectangle coordinates
                    if (cropWindow.Size.IsEmpty) throw new Exception("You must specify a custom crop rectange if crop=custom");
                }
            }
            return cropWindow;
        }

        private FitMode determineFitMode(ResizeSettings settings)
        {
            FitMode fit = settings.Mode;
            //Determine fit mode to use if both vertical and horizontal limits are used.
            if (fit == FitMode.None)
            {
                if (settings.Width != -1 || settings.Height != -1)
                {

                    if ("fill".Equals(settings["stretch"], StringComparison.OrdinalIgnoreCase)) fit = FitMode.Stretch;
                    else if ("auto".Equals(settings["crop"], StringComparison.OrdinalIgnoreCase)) fit = FitMode.Crop;
                    else if (!string.IsNullOrEmpty(settings["carve"])
                        && !"false".Equals(settings["carve"], StringComparison.OrdinalIgnoreCase)
                        && !"none".Equals(settings["carve"], StringComparison.OrdinalIgnoreCase))
                        fit = FitMode.Carve;
                    else fit = FitMode.Pad;
                }
                else
                {
                    fit = FitMode.Max;
                }

            }
            return fit;
        }
        public void ApplySettings(ResizeSettings settings)
        {
            copyRect = determineManualCropWindow(settings);

            //Save the manual crop size.
            SizeF manualCropSize = copyRect.Size;
            RectangleF manualCropRect = copyRect;

            FitMode fit = determineFitMode(settings);

            //Aspect ratio of the image
            double imageRatio = copyRect.Width / copyRect.Height;

            //Zoom factor
            double zoom = settings.Get<double>("zoom", 1);

            //The target size for the image 
            targetSize = new SizeF(-1, -1);
            //Target area for the image
            areaSize = new SizeF(-1, -1);
            //If any dimensions are specified, calculate. Otherwise, use original image dimensions
            if (settings.Width != -1 || settings.Height != -1 || settings.MaxHeight != -1 || settings.MaxWidth != -1)
            {
                //A dimension was specified. 
                //We first calculate the largest size the image can be under the width/height/maxwidth/maxheight restriction
                //- pretending stretch=fill and scale=both

                //Temp vars - results stored in targetSize and areaSize
                double width = settings.Width;
                double height = settings.Height;
                double maxwidth = settings.MaxWidth;
                double maxheight = settings.MaxHeight;

                //Eliminate cases where both a value and a max value are specified: use the smaller value for the width/height 
                if (maxwidth > 0 && width > 0) { width = Math.Min(maxwidth, width); maxwidth = -1; }
                if (maxheight > 0 && height > 0) { height = Math.Min(maxheight, height); maxheight = -1; }

                //Handle cases of width/maxheight and height/maxwidth as in legacy version 
                if (width != -1 && maxheight != -1) maxheight = Math.Min(maxheight, (width / imageRatio));
                if (height != -1 && maxwidth != -1) maxwidth = Math.Min(maxwidth, (height * imageRatio));


                //Move max values to width/height. FitMode should already reflect the mode we are using, and we've already resolved mixed modes above.
                width = Math.Max(width, maxwidth);
                height = Math.Max(height, maxheight);

                //Calculate missing value (a missing value is handled the same everywhere). 
                if (width > 0 && height <= 0) height = width / imageRatio;
                else if (height > 0 && width <= 0) width = height * imageRatio;

                //We now have width & height, our target size. It will only be a different aspect ratio from the image if both 'width' and 'height' are specified.

                //FitMode.Max
                if (fit == FitMode.Max)
                {
                    areaSize = targetSize = PolygonMath.ScaleInside(manualCropSize, new SizeF((float)width, (float)height));
                    //FitMode.Pad
                }
                else if (fit == FitMode.Pad)
                {
                    areaSize = new SizeF((float)width, (float)height);
                    targetSize = PolygonMath.ScaleInside(manualCropSize, areaSize);
                    //FitMode.crop
                }
                else if (fit == FitMode.Crop)
                {
                    //We autocrop - so both target and area match the requested size
                    areaSize = targetSize = new SizeF((float)width, (float)height);
                    RectangleF copyRect;

                    ScaleMode scale = settings.Scale;
                    bool cropWidthSmaller = manualCropSize.Width <= (float)width;
                    bool cropHeightSmaller = manualCropSize.Height <= (float)height;

                    //TODO: consider mode=crop;fit=upscale

                    // With both DownscaleOnly (where only one dimension is smaller than
                    // requested) and UpscaleCanvas, we will have a targetSize based on the
                    // minWidth & minHeight.
                    // TODO: what happens if mode=crop;scale=down but the target is larger than the source?
                  
                    if ((scale == ScaleMode.DownscaleOnly && (cropWidthSmaller != cropHeightSmaller)) ||
                          (scale == ScaleMode.UpscaleCanvas && (cropHeightSmaller || cropWidthSmaller)))
                    {
                        var minWidth = Math.Min(manualCropSize.Width, (float)width);
                        var minHeight = Math.Min(manualCropSize.Height, (float)height);

                        targetSize = new SizeF(minWidth, minHeight);

                        copyRect = manualCropRect = new RectangleF(0, 0, minWidth, minHeight);

                        // For DownscaleOnly, the areaSize is adjusted to the new targetSize as well.
                        if (scale == ScaleMode.DownscaleOnly)
                        {
                            areaSize = targetSize;
                        }
                    }
                    else
                    {
                        //Determine the size of the area we are copying
                        Size sourceSize = PolygonMath.RoundPoints(PolygonMath.ScaleInside(areaSize, manualCropSize));
                        //Center the portion we are copying within the manualCropSize
                        copyRect = new RectangleF(0, 0, sourceSize.Width, sourceSize.Height);
                    }
                

                    // Align the actual source-copy rectangle inside the available
                    // space based on the anchor.
                    this.copyRect = PolygonMath.ToRectangle(PolygonMath.AlignWith(copyRect, this.copyRect, settings.Anchor));

                }
                else
                { //Stretch and carve both act like stretching, so do that:
                    areaSize = targetSize = new SizeF((float)width, (float)height);
                }


            }
            else
            {
                //No dimensions specified, no fit mode needed. Use manual crop dimensions
                areaSize = targetSize = manualCropSize;
            }

            //Multiply both areaSize and targetSize by zoom. 
            areaSize.Width *= (float)zoom;
            areaSize.Height *= (float)zoom;
            targetSize.Width *= (float)zoom;
            targetSize.Height *= (float)zoom;

            //Todo: automatic crop is permitted to break the scaling rule Fix!!

            //Now do upscale/downscale check If they take effect, set targetSize to imageSize
            if (settings.Scale == ScaleMode.DownscaleOnly)
            {
                if (PolygonMath.FitsInside(manualCropSize, targetSize))
                {
                    //The image is smaller or equal to its target polygon. Use original image coordinates instead.
                    areaSize = targetSize = manualCropSize;
                    copyRect = manualCropRect;
                }
            }
            else if (settings.Scale == ScaleMode.UpscaleOnly)
            {
                if (!PolygonMath.FitsInside(manualCropSize, targetSize))
                {
                    //The image is larger than its target. Use original image coordintes instead
                    areaSize = targetSize = manualCropSize;
                    copyRect = manualCropRect;
                }
            }
            else if (settings.Scale == ScaleMode.UpscaleCanvas)
            {
                //Same as downscaleonly, except areaSize isn't changed.
                if (PolygonMath.FitsInside(manualCropSize, targetSize))
                {
                    //The image is smaller or equal to its target polygon. 

                    //Use manual copy rect/size instead.

                    targetSize = manualCropSize;
                    copyRect = manualCropRect;
                }
            }


            //May 12: require max dimension and round values to minimize rounding differences later.
            areaSize.Width = Math.Max(1, (float)Math.Round(areaSize.Width));
            areaSize.Height = Math.Max(1, (float)Math.Round(areaSize.Height));
            targetSize.Width = Math.Max(1, (float)Math.Round(targetSize.Width));
            targetSize.Height = Math.Max(1, (float)Math.Round(targetSize.Height));


        }


        public RectangleF CopyFrom { get { return copyRect;  } }
        public SizeF CopyToSize { get { return targetSize; } }
        public SizeF CanvasSize { get { return areaSize;  } }

    }
}
