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
        /// Installs the plugin to the input ImageResizer configuration
        /// </summary>
        /// <param name="c">ImageResizer configuration to install plugin to </param>
        /// <returns>ImageResizer plugin that was installed </returns>
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        /// <summary>
        /// Removes the Plugin from the input configuration
        /// </summary>
        /// <param name="c">ImageResizer with the plugin to remove</param>
        /// <returns>true if plugin uninstalled</returns>
        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        /// <summary>
        /// Gets strings that can be used to query on
        /// </summary>
        /// <returns>An IEnumerable collection of supported Query strings</returns>
        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "c.focus", "c.zoom", "c.finalmode" };
        }

        /// <summary>
        /// Perform the crop and fitting of the image in the Layout
        /// </summary>
        /// <param name="s">state of an Image being resized </param>
        /// <returns>Default action, which defaults to none</returns>
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
            
            ///What is the vertical and horizontal aspect ratio different in result pixels?
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
