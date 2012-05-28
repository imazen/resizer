using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using FreeImageAPI;
using System.Drawing.Drawing2D;
using ImageResizer.Util;
using System.Drawing;

namespace ImageResizer.Plugins.FreeImageScaling {
    public class FreeImageScalingPlugin : BuilderExtension, IPlugin, IQuerystringPlugin {
        public FreeImageScalingPlugin() {
        }
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }

        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "fi.scale" };
        }

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


            //The size of the temporary bitmap. 
            //We want it larger than the size we'll use on the final copy, so we never upscale it
            //- but we also want it as small as possible so processing is fast.
            SizeF tempSize = PolygonMath.ScaleOutside(targetSize, s.copyRect.Size);
            int tempWidth = (int)Math.Ceiling(tempSize.Width);
            int tempHeight = (int)Math.Ceiling(tempSize.Height);
            FIBITMAP src = FIBITMAP.Zero;
            FIBITMAP midway = FIBITMAP.Zero;
            try {
                //Crop if needed, Convert, scale, then convert back.
                if (s.preRenderBitmap != null || (s.copyRect.Width == s.originalSize.Width && s.copyRect.Height == s.originalSize.Height && s.copyRect.X == 0 && s.copyRect.Y == 0)){
                    src = FreeImage.CreateFromBitmap(s.preRenderBitmap != null ? s.preRenderBitmap : s.sourceBitmap);
                }else{

                    using (Bitmap c = s.sourceBitmap.Clone(s.copyRect, System.Drawing.Imaging.PixelFormat.Format32bppArgb)) {
                        src = FreeImage.CreateFromBitmap(c);
                    }
                }
                midway = FreeImage.Rescale(src, tempWidth, tempHeight, filter);
                FreeImage.UnloadEx(ref src);
                //Clear the old pre-rendered image if needed
                if (s.preRenderBitmap != null) s.preRenderBitmap.Dispose();
                //Reassign the pre-rendered image
                s.preRenderBitmap = FreeImage.GetBitmap(midway);
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
