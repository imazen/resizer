using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using FreeImageAPI;
using System.Drawing.Drawing2D;
using ImageResizer.Util;
using System.Drawing;
using ImageResizer.Plugins.FreeImageResizer;
using ImageResizer.Plugins.FreeImageScaling;

namespace ImageResizer.Plugins.FreeImageResizer { 
    
    /// <summary>
    /// Plugin that Resizes the FreeImage image
    /// </summary>
    public class FreeImageResizerPlugin : FreeImageScalingPlugin { 

        /// <summary>
        /// Empty constructor creates an instance of the FreeImageResizer Plugin
        /// </summary>
        public FreeImageResizerPlugin() { } 
    } 
}


namespace ImageResizer.Plugins.FreeImageScaling {

    /// <summary>
    /// Plugin that does Image Scaling
    /// </summary>
    public class FreeImageScalingPlugin : BuilderExtension, IPlugin, IQuerystringPlugin {

        /// <summary>
        /// Empty constructor creates an instance of the FreeImageScaling Plugin
        /// </summary>
        public FreeImageScalingPlugin() {
        }

        /// <summary>
        /// Install the FreeImageScaling plugin to the given config
        /// </summary>
        /// <param name="c">ImageResizer Configuration to install the plugin</param>
        /// <returns>FreeImageScaling plugin that was installed</returns>
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        /// <summary>
        /// Uninstalls the FreeImageScaling plugin from the given ImageResizer Configuration
        /// </summary>
        /// <param name="c">ImageResizer Configuration</param>
        /// <returns>true if plugin uninstalled</returns>
        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        /// <summary>
        /// Gets a collection of supported query strings
        /// </summary>
        /// <returns>IEnumerable Collection of supported query strings</returns>
        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "fi.scale" };
        }

        /// <summary>
        /// Sets up the filter to use in scaling
        /// </summary>
        /// <param name="sf">string filter value</param>
        /// <param name="defaultValue">FREE_IMAGE_FILTER</param>
        /// <param name="valid">out parameter set denoting if filter is valid</param>
        /// <returns>filters used in scaling</returns>
        public static FREE_IMAGE_FILTER ParseResizeAlgorithm(string sf, FREE_IMAGE_FILTER defaultValue, out bool valid){
            FREE_IMAGE_FILTER filter = defaultValue;

             valid = true;
             if ("bicubic".Equals(sf, StringComparison.OrdinalIgnoreCase)) filter = FREE_IMAGE_FILTER.FILTER_BICUBIC;
             else if ("bilinear".Equals(sf, StringComparison.OrdinalIgnoreCase)) filter = FREE_IMAGE_FILTER.FILTER_BILINEAR;
             else if ("box".Equals(sf, StringComparison.OrdinalIgnoreCase)) filter = FREE_IMAGE_FILTER.FILTER_BOX;
             else if ("bspline".Equals(sf, StringComparison.OrdinalIgnoreCase)) filter = FREE_IMAGE_FILTER.FILTER_BSPLINE;
             else if ("catmullrom".Equals(sf, StringComparison.OrdinalIgnoreCase)) filter = FREE_IMAGE_FILTER.FILTER_CATMULLROM;
             else if ("lanczos".Equals(sf, StringComparison.OrdinalIgnoreCase)) filter = FREE_IMAGE_FILTER.FILTER_LANCZOS3;
             else valid = false;

            return filter;
        }

        /// <summary>
        /// Tries to render an image based on the current settings values
        /// </summary>
        /// <param name="s">Resizer ImageState data to draw</param>
        /// <returns>Requested action, which defaults to "None"</returns>
        protected override RequestedAction PreRenderImage(ImageState s) {
            //Skip this when we are doing simulations
            if (s.destGraphics == null) return RequestedAction.None;

            string sf = s.settings["fi.scale"];
            if (string.IsNullOrEmpty(sf)) return RequestedAction.None;
            bool validAlg = false;
            FREE_IMAGE_FILTER filter = ParseResizeAlgorithm(sf, FREE_IMAGE_FILTER.FILTER_CATMULLROM, out validAlg);
            if (!validAlg) throw new ImageProcessingException("The specified resizing filter '" + sf + "' did not match bicubic, bilinear, box, bspline, catmullrom, or lanczos.");

            //Set copy attributes
            s.copyAttibutes.SetWrapMode(WrapMode.TileFlipXY);


            //The minimum dimensions of the temporary bitmap.
            SizeF targetSize = PolygonMath.getParallelogramSize(s.layout["image"]);
            targetSize = new SizeF((float)Math.Ceiling(targetSize.Width), (float)Math.Ceiling(targetSize.Height));

            s.ApplyCropping();
            s.EnsurePreRenderBitmap();

            //The size of the temporary bitmap. 
            //We want it larger than the size we'll use on the final copy, so we never upscale it
            //- but we also want it as small as possible so processing is fast.
            SizeF tempSize = PolygonMath.ScaleOutside(targetSize, s.copyRect.Size);
            int tempWidth = (int)Math.Ceiling(tempSize.Width);
            int tempHeight = (int)Math.Ceiling(tempSize.Height);
            FIBITMAP src = FIBITMAP.Zero;
            FIBITMAP midway = FIBITMAP.Zero;
            try {
                var oldbit = s.preRenderBitmap ?? s.sourceBitmap;
                //Crop if needed, Convert, scale, then convert back.
                src = FreeImage.CreateFromBitmap(oldbit);
                
                midway = FreeImage.Rescale(src, tempWidth, tempHeight, filter);
                FreeImage.UnloadEx(ref src);
                //Clear the old pre-rendered image if needed
                if (s.preRenderBitmap != null) s.preRenderBitmap.Dispose();
                //Reassign the pre-rendered image
                s.preRenderBitmap = FreeImage.GetBitmap(midway);
                s.copyRect = new RectangleF(0, 0, s.preRenderBitmap.Width, s.preRenderBitmap.Height);
                FreeImage.UnloadEx(ref midway);
                s.preRenderBitmap.MakeTransparent();

            } finally {
                if (!src.IsNull) FreeImage.UnloadEx(ref src);
                if (!midway.IsNull) FreeImage.UnloadEx(ref midway);
            }
            

            return RequestedAction.Cancel;
        }
    }
}
