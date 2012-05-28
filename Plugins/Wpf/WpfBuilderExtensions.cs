using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImageResizer.Resizing;
using System.Drawing;
using ImageResizer.Util;

namespace ImageResizer.Plugins.WpfBuilder
{
    public static class WpfBuilderExtensions
    {
        public static WpfImageSettings WpfDestinationImageSettings(this ImageState imageState, ResizeSettings settings) 
        {
            WpfImageSettings wpfImageSettings = new WpfImageSettings();
            Rectangle imageDest = PolygonMath.ToRectangle(PolygonMath.GetBoundingBox(imageState.layout["image"]));


            /* test - provo a ricavare i dati di resize che mi servono direttamente dagli oggetti Drawing sottostanti */
            SizeF imageAreaSizes = PolygonMath.getParallelogramSize(imageState.layout["image"]);


            wpfImageSettings.DestinationImageWidth = imageAreaSizes.Width;
            wpfImageSettings.DestinationImageHeight = imageAreaSizes.Height;








            // Correct the settings.Mode according to the documentation
            if (settings.Mode == FitMode.None && (settings.Width != -1 && settings.Height != -1))
                settings.Mode = FitMode.Pad;
            else if (settings.Mode == FitMode.None && (settings.MaxWidth != -1 && settings.MaxHeight != -1))
                settings.Mode = FitMode.Max;


            #region -- Manage the image dimensions --

            //double widthToApply = (settings.Width == -1 ? (double)settings.MaxWidth : (double)settings.Width);
            //double heightToApply = (settings.Height == -1 ? (double)settings.MaxHeight : (double)settings.Height);
            //var proportionWidth = (double)imageState.originalSize.Width / widthToApply;
            //var proportionHeight = (double)imageState.originalSize.Height / heightToApply;

            switch (settings.Mode)
            {
                case FitMode.None:
                    break;

                case FitMode.Carve:
                    // TODO
                //case FitMode.Pad:
                case FitMode.Max:
                    //if (proportionWidth > proportionHeight)
                    //{
                    //    wpfImageSettings.DestinationImageHeight = Convert.ToInt32(imageState.originalSize.Height / proportionWidth);
                    //    wpfImageSettings.DestinationImageWidth = Convert.ToInt32(widthToApply);
                    //}
                    //else 
                    //{
                    //    wpfImageSettings.DestinationImageWidth = Convert.ToInt32(imageState.originalSize.Width / proportionHeight);
                    //    wpfImageSettings.DestinationImageHeight = Convert.ToInt32(heightToApply);
                    //}

                    /*
                     
                     
                     
                     TODO:
                     * 
                     * verificare la necessità di calcolare gli offset per il PAD
                     
                     
                     
                     
                     */














                    break;

                case FitMode.Crop:
                case FitMode.Pad:
                    //int scaleWidth, scaleHeight;
                    //scaleWidth = scaleHeight = 0;

                    //wpfImageSettings.DestinationImageCanvasWidth = scaleWidth = imageDest.Width;
                    //wpfImageSettings.DestinationImageCanvasHeight = scaleHeight = imageDest.Height;

                    //// If only a dimension is missing make it square
                    //if (wpfImageSettings.DestinationImageCanvasWidth == 0 || wpfImageSettings.DestinationImageCanvasHeight == 0)
                    //{
                    //    wpfImageSettings.DestinationImageCanvasWidth = wpfImageSettings.DestinationImageCanvasHeight = Math.Max(wpfImageSettings.DestinationImageCanvasWidth, wpfImageSettings.DestinationImageCanvasHeight);
                    //}

                    //double originalProportions = (double)imageState.originalSize.Width / (double)imageState.originalSize.Height;
                    //double viewportProportions = (double)wpfImageSettings.DestinationImageCanvasWidth / (double)wpfImageSettings.DestinationImageCanvasHeight;

                    //// Calculates the new scale proportions to make touche-from-inside crop
                    //if ((originalProportions > 1 && viewportProportions <= 1) || (originalProportions < 1 && viewportProportions > 1))
                    //{
                    //    scaleHeight = Math.Max(wpfImageSettings.DestinationImageCanvasHeight, wpfImageSettings.DestinationImageCanvasWidth);
                    //    scaleWidth = Convert.ToInt32(((float)(scaleHeight) / (float)(imageState.originalSize.Height)) * imageState.originalSize.Width);
                    //}
                    //else
                    //{
                    //    scaleWidth = Math.Max(wpfImageSettings.DestinationImageCanvasHeight, wpfImageSettings.DestinationImageCanvasWidth);
                    //    scaleHeight = Convert.ToInt32(((float)(scaleWidth) / (float)(imageState.originalSize.Width)) * imageState.originalSize.Height);
                    //}

                    //wpfImageSettings.DestinationImageWidth = scaleWidth;
                    //wpfImageSettings.DestinationImageHeight = scaleHeight;

                    //if ((imageState.copyRect.Y == 0) && (imageState.copyRect.X != 0))
                    if ((imageState.originalSize.Width / imageState.originalSize.Height) >= (imageDest.Width / imageDest.Height))
                    {
                        wpfImageSettings.DestinationImageWidth = (imageState.originalSize.Width * imageDest.Height) / imageState.copyRect.Height;

                        if (settings.Mode == FitMode.Pad)
                            wpfImageSettings.DestinationImageHeight = imageState.originalSize.Height;
                        else
                            wpfImageSettings.DestinationImageHeight = imageDest.Height;

                        wpfImageSettings.OffsetX = -(wpfImageSettings.DestinationImageWidth * imageState.copyRect.X) / imageState.originalSize.Width;
                        wpfImageSettings.OffsetY = 0;
                    }
                    else // if ((imageState.copyRect.X == 0) && (imageState.copyRect.Y != 0))
                    {
                        if (settings.Mode == FitMode.Pad)
                            wpfImageSettings.DestinationImageWidth = imageState.originalSize.Width;
                        else
                            wpfImageSettings.DestinationImageWidth = imageDest.Width;

                        wpfImageSettings.DestinationImageHeight = (imageState.originalSize.Height * imageDest.Width) / imageState.copyRect.Width;
                        wpfImageSettings.OffsetX = 0;
                        wpfImageSettings.OffsetY = -(wpfImageSettings.DestinationImageHeight * imageState.copyRect.Y) / imageState.originalSize.Height;
                    }
                    /*else 
                    {
                        
                    }*/

                    break;

                //case FitMode.Pad:
                //    wpfImageSettings.DestinationImageHeight = Convert.ToInt32(imageState.layout["image"][3].Y - imageState.layout["image"][0].Y);
                //    wpfImageSettings.DestinationImageWidth = Convert.ToInt32(imageState.layout["image"][1].X - imageState.layout["image"][3].X);
                //    break;

                case FitMode.Stretch:
                    //wpfImageSettings.DestinationImageWidth = Convert.ToInt32(widthToApply);
                    //wpfImageSettings.DestinationImageHeight = Convert.ToInt32(heightToApply);
                    break;
                
                default:
                    wpfImageSettings.DestinationImageWidth = imageState.originalSize.Width;
                    wpfImageSettings.DestinationImageHeight = imageState.originalSize.Height;
                    break;
            }

            #endregion

            #region -- Manage the allignments --

            switch (settings.Mode)
            {
                case FitMode.None:
                case FitMode.Crop:
                case FitMode.Pad:
                    RectangleF croppedSize = settings.getCustomCropSourceRect(imageState.originalSize);

                    if ((croppedSize.X != 0) || (croppedSize.Y != 0))
                    {
                        wpfImageSettings.OffsetX = -Convert.ToInt32(croppedSize.X);
                        wpfImageSettings.OffsetY = -Convert.ToInt32(croppedSize.Y);

                        wpfImageSettings.DestinationImageCanvasWidth = croppedSize.Right - croppedSize.Left;
                        wpfImageSettings.DestinationImageCanvasHeight = croppedSize.Bottom - croppedSize.Top;
                    }
                    else 
                    {
                        wpfImageSettings.OffsetX = imageState.layout["image"][0].X;
                        wpfImageSettings.OffsetY = imageState.layout["image"][0].Y;
                    }
                    

                        
                    

                    //wpfImageSettings.DestinationImageCanvasWidth = imageDest.Width;
                    //wpfImageSettings.DestinationImageCanvasHeight = imageDest.Height;

                    //// In crop or pad I've to calculate the Offsets
                    //switch (settings.Anchor)
                    //{
                    //    case ContentAlignment.BottomCenter:
                    //        wpfImageSettings.OffsetX = (int)Math.Floor((double)(imageState.finalSize.Width - wpfImageSettings.DestinationImageWidth) / 2);
                    //        wpfImageSettings.OffsetY = imageState.finalSize.Height - wpfImageSettings.DestinationImageHeight;
                    //        break;
                    //    case ContentAlignment.BottomLeft:
                    //        wpfImageSettings.OffsetX = 0;
                    //        wpfImageSettings.OffsetY = imageState.finalSize.Height - wpfImageSettings.DestinationImageHeight;
                    //        break;
                    //    case ContentAlignment.BottomRight:
                    //        wpfImageSettings.OffsetX = imageState.finalSize.Width - wpfImageSettings.DestinationImageWidth;
                    //        wpfImageSettings.OffsetY = imageState.finalSize.Height - wpfImageSettings.DestinationImageHeight;
                    //        break;
                    //    case ContentAlignment.MiddleCenter:
                    //        wpfImageSettings.OffsetX = (int)Math.Floor((double)(imageState.finalSize.Width - wpfImageSettings.DestinationImageWidth) / 2);
                    //        wpfImageSettings.OffsetY = (int)Math.Floor((double)(imageState.finalSize.Height - wpfImageSettings.DestinationImageHeight) / 2);
                    //        break;
                    //    case ContentAlignment.MiddleLeft:
                    //        wpfImageSettings.OffsetX = 0;
                    //        wpfImageSettings.OffsetY = (int)Math.Floor((double)(imageState.finalSize.Height - wpfImageSettings.DestinationImageHeight) / 2);
                    //        break;
                    //    case ContentAlignment.MiddleRight:
                    //        wpfImageSettings.OffsetX = imageState.finalSize.Width - wpfImageSettings.DestinationImageWidth;
                    //        wpfImageSettings.OffsetY = (int)Math.Floor((double)(imageState.finalSize.Height - wpfImageSettings.DestinationImageHeight) / 2);
                    //        break;
                    //    case ContentAlignment.TopCenter:
                    //        wpfImageSettings.OffsetX = (int)Math.Floor((double)(imageState.finalSize.Width - wpfImageSettings.DestinationImageWidth) / 2);
                    //        wpfImageSettings.OffsetY = 0;
                    //        break;
                    //    case ContentAlignment.TopLeft:
                    //        wpfImageSettings.OffsetX = 0;
                    //        wpfImageSettings.OffsetY = 0;
                    //        break;
                    //    case ContentAlignment.TopRight:
                    //        wpfImageSettings.OffsetX = imageState.finalSize.Width - wpfImageSettings.DestinationImageWidth;
                    //        wpfImageSettings.OffsetY = 0;
                    //        break;
                    //    default:
                    //        break;
                    //}
                    break;

                //case FitMode.Crop:

                //    break;
                default:
                    /*
                     
                     
                     TODO: risistemare!!!
                     
                     
                     */

                    // Supposing I'm on manual cropping, I'll use the underlying calculations
                    //wpfImageSettings.DestinationImageWidth = imageState.originalSize.Width;
                    //wpfImageSettings.DestinationImageHeight = imageState.originalSize.Height;

                    //RectangleF croppedSize = settings.getCustomCropSourceRect(imageState.originalSize);

                    //wpfImageSettings.OffsetX = -Convert.ToInt32(croppedSize.X);
                    //wpfImageSettings.OffsetY = -Convert.ToInt32(croppedSize.Y);

                    //wpfImageSettings.DestinationImageCanvasWidth = croppedSize.Right - croppedSize.Left;
                    //wpfImageSettings.DestinationImageCanvasHeight = croppedSize.Bottom - croppedSize.Top;
                    break;
            }

            #endregion

            if ((settings.Rotate % 360) != 0)
            {
                wpfImageSettings.OffsetX = (imageState.finalSize.Width - wpfImageSettings.DestinationImageWidth) / 2;
                wpfImageSettings.OffsetY = (imageState.finalSize.Height - wpfImageSettings.DestinationImageHeight) / 2;
            }

            return wpfImageSettings;
        }

    }
}
