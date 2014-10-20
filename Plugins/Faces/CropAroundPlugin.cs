using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImageResizer.Resizing;
using System.Drawing;
using ImageResizer.ExtensionMethods;
using ImageResizer.Util;

namespace ImageResizer.Plugins.CropAround {
    /// <summary>
    /// Enables cropping based on a set of rectangles to preserve
    /// </summary>
    public class CropAroundPlugin:BuilderExtension, IPlugin,IQuerystringPlugin  {
     
        /// <summary>
        /// Adds the plugin to the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        /// <summary>
        /// Removes the plugin from the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        /// <summary>
        /// Returns the querystrings command keys supported by this plugin. 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "c.focus", "c.zoom", "c.finalmode" };
        }

        protected override RequestedAction LayoutImage(ImageState s) {
            //Only activated if both width and height are specified, and mode=crop.
            if (s.settings.Mode != FitMode.Crop || s.settings.Width < 0 || s.settings.Height < 0) return RequestedAction.None;

            //Calculate bounding box for all coordinates specified.
            double[] focus = s.settings.GetList<double>("c.focus", null, 2, 4, 8, 12, 16, 20, 24, 28, 32, 36, 40, 44, 48, 52, 56, 60, 64, 68, 72);
            if (focus == null) return RequestedAction.None;
            RectangleF box = PolygonMath.GetBoundingBox(focus);

            var bounds = new RectangleF(new PointF(0,0),s.originalSize);
            //Clip box to original image bounds
            box = PolygonMath.ClipRectangle(box, bounds);

            var targetSize = new SizeF(s.settings.Width,s.settings.Height);

            SizeF copySize;

            //Now, we can either crop as closely as possible or as loosely as possible. 
            if (s.settings.Get<bool>("c.zoom", false) && box.Width > 0 && box.Height > 0) {
                //Crop close
                copySize = PolygonMath.ScaleOutside(box.Size, targetSize);
            } else {
                //Crop minimally
                copySize = PolygonMath.ScaleInside(targetSize, bounds.Size);
                //Ensure it's outside the box
                if (!PolygonMath.FitsInside(box.Size,copySize)) copySize = PolygonMath.ScaleOutside(box.Size, copySize);

            }
            //Clip to bounds.
            box = PolygonMath.ClipRectangle(PolygonMath.ExpandTo(box, copySize), bounds);

            s.copyRect = box;
            
            //What is the vertical and horizontal aspect ratio different in result pixels?
            var padding = PolygonMath.ScaleInside(box.Size, targetSize);
            padding = new SizeF(targetSize.Width - padding.Width, targetSize.Height - padding.Height);


            //So, if we haven't met the aspect ratio yet, what mode will we pass on?
            var finalmode = s.settings.Get<FitMode>("c.finalmode", FitMode.Pad);

            //Crop off 1 or 2 pixels instead of padding without worrying too much
            if (finalmode == FitMode.Pad && padding.Width + padding.Height < 3) finalmode = FitMode.Crop;

            s.settings.Mode = finalmode;

            return RequestedAction.None;
        }
        
    }
}
