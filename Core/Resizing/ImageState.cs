using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace fbs.ImageResizer.Resizing {
    /// <summary>
    /// Encapsulates the state of an image being resized. 
    /// Can be used to simulate a resize as well as actually perform one.
    /// All code should be ignore when Bitmaps are null, and go about simulating all the mathematical functions as normal.
    /// </summary>
    public class ImageState :IDisposable {
        /// <summary>
        /// The original size of the source bitmap. Use this instead of accessing the bitmap directly for this information, since the bitmap may not always be available
        /// </summary>
        public SizeF originalSize;
        /// <summary>
        /// The maximum dimensions permitted by the configuration settings. (
        /// </summary>
        public SizeF maxSize;
        /// <summary>
        /// The size of the final bitmap image
        /// </summary>
        public SizeF destSize;

        /// <summary>
        /// The rectangular portion of the source image to copy
        /// </summary>
        public RectangleF copyRect;

        /// <summary>
        /// (read-only) Same as copyRect.Size, convenience property.
        /// </summary>
        public SizeF copySize { get { return copyRect.Size; } }
        /// <summary>
        /// The polygon on the new image to draw the image to. All 4 points are clockwise.
        /// </summary>
        public PointF[] imageOuterEdge;
        /// <summary>
        /// The polygon space that will be required (includes letterboxing space). All 4 points are clockwise.
        /// </summary>
        public PointF[] imageAreaOuterEdge;


        /// <summary>
        /// The polygon that contains the padding, imageAreaOuterEdge, and imageOuterEdge
        /// </summary>
        public PointF[] paddingOuterEdge;

        /// <summary>
        /// The polygon space that contains the border, paddingOuterEdge, imageAreaOuterEdge, and imageOuterEdge
        /// </summary>
        public PointF[] borderOuterEdge;

        /// <summary>
        /// The polygon space that contains the effect, border, padding, image area, and image.
        /// </summary>
        public PointF[] effectOuterEdge;

        /// <summary>
        /// The polygon space that contains the margin, effect, border, padding, image area, and image.
        /// </summary>
        public PointF[] marginOuterEdge;

     

        /// <summary>
        /// Coordinates in the plane of the original image to translate into the destination bitmap plane. Used for translating image maps.
        /// </summary>
        public PointF[] pointsToTranslate;

        protected PointTranslationBehavior pointBehavior= PointTranslationBehavior.ClosestVisiblePoint;
        /// <summary>
        /// How the pointsToTranslate should be translated if they are cropped out of the destination image.
        /// </summary>
        public PointTranslationBehavior PointBehavior{
            get{ return pointBehavior;}}
        
        public enum PointTranslationBehavior{ Exact, ClosestVisiblePoint, Empty}

        /// <summary>
        /// The source bitmap. May be null, always check. If null, only skip bitmap modification, not math.
        /// </summary>
        public Bitmap sourceBitmap;
        /// <summary>
        /// The destination bitmap. Assume it may be null at any time.
        /// </summary>
        public Bitmap destBitmap;
        /// <summary>
        /// A graphics object to write to the destination bitmap
        /// </summary>
        public Graphics destGraphics;
        /// <summary>
        /// The commands to apply to the bitmap
        /// </summary>
        public ResizeSettingsCollection settings;

        public ImageState( ResizeSettingsCollection settings, SizeF originalSize, SizeF maxSize) {
            this.settings = settings;
            
        }

        
        /// <summary>
        /// Disposes sourceBitmap, destGraphics, and destBitmap
        /// </summary>
        public void Dispose() {
            try { if (sourceBitmap != null) sourceBitmap.Dispose(); } finally {
                try {
                    if (destGraphics != null) destGraphics.Dispose();
                } finally {
                    if (destBitmap != null) destBitmap.Dispose();
                }
            }
        }
    }
}
