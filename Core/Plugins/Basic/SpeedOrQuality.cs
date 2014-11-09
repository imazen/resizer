using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using System.Drawing.Drawing2D;
using ImageResizer.Util;
using System.Drawing;
using System.Globalization;
using System.Drawing.Imaging;

namespace ImageResizer.Plugins.Basic {
    public class SpeedOrQuality:BuilderExtension, IPlugin {

        public SpeedOrQuality() { }

        Configuration.Config c;

        public IPlugin Install(Configuration.Config c) {
            this.c = c;
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            this.c = c;
            c.Plugins.remove_plugin(this);
            return true;
        }

        protected override RequestedAction RenderImage(ImageState s) {          
            //Skip this when we are doing simulations
            if (s.destGraphics == null) return RequestedAction.None;

            //If there's pre-rendering involved this optimization is utterly pointless.
            if (s.preRenderBitmap != null) return RequestedAction.None;

            //Find out what the speed setting is.
            int speed = 0;
            if (string.IsNullOrEmpty(s.settings["speed"]) || !int.TryParse(s.settings["speed"], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out speed)) speed = 0;

            if (speed < 1) return RequestedAction.None;

            s.destGraphics.CompositingMode = CompositingMode.SourceCopy;
            s.destGraphics.CompositingQuality = CompositingQuality.HighSpeed;
            if (speed == 1)
                s.destGraphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
            else
                s.destGraphics.InterpolationMode = InterpolationMode.Bilinear;

            s.destGraphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;
            s.destGraphics.SmoothingMode = SmoothingMode.HighSpeed;

            if (speed < 3) {
                using (var ia = new ImageAttributes())
                {
                    ia.SetWrapMode(WrapMode.TileFlipXY);
                    if (s.colorMatrix != null) ia.SetColorMatrix(new ColorMatrix(s.colorMatrix));
                    s.destGraphics.DrawImage(s.sourceBitmap, PolygonMath.getParallelogram(s.layout["image"]), s.copyRect, GraphicsUnit.Pixel, ia);
                }
                
            } else if (speed < 4) {
                Rectangle midsize = PolygonMath.ToRectangle(PolygonMath.GetBoundingBox(s.layout["image"]));

                using (Image thumb = s.sourceBitmap.GetThumbnailImage(midsize.Width, midsize.Height, delegate() { return false; }, IntPtr.Zero)) {
                    double xfactor = (double)thumb.Width / (double)s.sourceBitmap.Width;
                    double yfactor = (double)thumb.Height / (double)s.sourceBitmap.Height;
                    RectangleF copyPart = new RectangleF((float)(s.copyRect.Left * xfactor),
                                                        (float)(s.copyRect.Top * yfactor),
                                                        (float)(s.copyRect.Width * xfactor),
                                                        (float)(s.copyRect.Height * yfactor));
                    if (Math.Floor(copyPart.Height) == thumb.Height || Math.Ceiling(copyPart.Height) == thumb.Height) copyPart.Height = thumb.Height;
                    if (Math.Floor(copyPart.Width) == thumb.Width || Math.Ceiling(copyPart.Width) == thumb.Width) copyPart.Width = thumb.Width;
                    using (var ia = new ImageAttributes())
                    {
                        ia.SetWrapMode(WrapMode.TileFlipXY);
                        if (s.colorMatrix != null) ia.SetColorMatrix(new ColorMatrix(s.colorMatrix));
                        s.destGraphics.DrawImage(thumb, PolygonMath.getParallelogram(s.layout["image"]), copyPart, GraphicsUnit.Pixel, ia);
                    }
                }
            } else {
                RectangleF box = PolygonMath.GetBoundingBox(PolygonMath.getParallelogram(s.layout["image"]));
                s.destGraphics.CompositingMode = CompositingMode.SourceCopy;
                s.destGraphics.DrawImage(s.sourceBitmap, box.Left, box.Top, box.Width, box.Height);
            }

            return RequestedAction.Cancel;
        }
    }
}
