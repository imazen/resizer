using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using System.Drawing.Drawing2D;
using ImageResizer.Util;
using System.Drawing;

namespace ImageResizer.Plugins.Basic {
    public class SpeedOrQuality:BuilderExtension, IPlugin {

        public SpeedOrQuality() { }

        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        protected override RequestedAction RenderImage(ImageState s) {          
            //Skip this when we are doing simulations
            if (s.destGraphics == null) return RequestedAction.None;

            //Find out what the speed setting is.
            int speed = 0;
            if (string.IsNullOrEmpty(s.settings["speed"]) || !int.TryParse(s.settings["speed"], out speed)) speed = 0;

            if (speed < 1) return RequestedAction.None;

            s.destGraphics.CompositingMode = CompositingMode.SourceCopy;
            s.destGraphics.CompositingQuality = CompositingQuality.HighSpeed;
            if (speed == 1)
                s.destGraphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
            else
                s.destGraphics.InterpolationMode = InterpolationMode.Bilinear;

            s.destGraphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;
            s.destGraphics.SmoothingMode = SmoothingMode.HighSpeed;

            s.copyAttibutes.SetWrapMode(WrapMode.TileFlipXY);

            if (speed < 3) {
                
                s.destGraphics.DrawImage(s.sourceBitmap, PolygonMath.getParallelogram(s.layout["image"]), s.copyRect, GraphicsUnit.Pixel, s.copyAttibutes);
            } else {
                Rectangle midsize = PolygonMath.ToRectangle(PolygonMath.GetBoundingBox(s.layout["image"]));
              
                using (Image thumb = s.sourceBitmap.GetThumbnailImage(midsize.Width, midsize.Height, delegate() { return false; }, IntPtr.Zero)) {
                    s.destGraphics.DrawImage(thumb, PolygonMath.getParallelogram(s.layout["image"]), s.copyRect, GraphicsUnit.Pixel, s.copyAttibutes);
                }
            }


            return RequestedAction.Cancel;
        }
    }
}
