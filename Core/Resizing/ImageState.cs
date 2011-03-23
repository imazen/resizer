using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace fbs.ImageResizer.Resizing {
    /// <summary>
    /// Encapsulates the state of an image being resized. 
    /// Can be used to simulate a resize as well as actually perform one.
    /// All code should ignore when Bitmaps and Graphics objects are null, and go about simulating all the mathematical functions as normal.
    /// 
    /// </summary>
    public class ImageState :IDisposable {

        public ImageState(ResizeSettingsCollection settings, Size originalSize, Size maxSize, bool transparencySupported) {
            this.settings = settings;
            this.originalSize = originalSize;
            this.maxSize = maxSize;
            this.supportsTransparency = transparencySupported;
        }
        /// <summary>
        /// The commands to apply to the bitmap
        /// </summary>
        public ResizeSettingsCollection settings;

        /// <summary>
        /// The original size of the source bitmap. Use this instead of accessing the bitmap directly for this information, since the bitmap may not always be available
        /// </summary>
        public Size originalSize;


        /// <summary>
        /// The maximum dimensions permitted by the configuration settings. Only applied to the image itself - borders can have unlimited dimensions.
        /// 2 passes would be nedded to apply these dimensions to the final bitmap - they must be adjusted after layout is calculated. Could introduce bugs.
        /// </summary>
        public Size maxSize;

        /// <summary>
        /// Rendering choices can depend on whether the output format supports transparency.
        /// </summary>
        public bool supportsTransparency = true;


        /// <summary>
        /// The layout object. Used for calculated and flowing the layout of the various rings around the image (padding, border, effect, margin, etc).
        /// </summary>
        public LayoutBuilder layout = new LayoutBuilder();

        /// <summary>
        /// The size of the target bitmap image. Set after all sizing operations have completed.
        /// </summary>
        public Size destSize;
        /// <summary>
        /// The dimensions of the bitmap afer all operations have been applied to it (Calling FlipRotate can change the bitmap dimensions).
        /// </summary>
        public Size finalSize;


        /// <summary>
        /// The rectangular portion of the source image to copy
        /// </summary>
        public RectangleF copyRect;
        /// <summary>
        /// (read-only) Same as copyRect.Size, convenience property.
        /// </summary>
        public SizeF copySize { get { return copyRect.Size; } }
       
        /// <summary>
        /// The source bitmap.  If null, skip drawing commands, but continue layout logic.
        /// </summary>
        public Bitmap sourceBitmap;
        /// <summary>
        /// The destination bitmap.  If null, skip drawing commands, but continue layout logic.
        /// </summary>
        public Bitmap destBitmap;
        /// <summary>
        /// A graphics object to write to the destination bitmap. If null, skip drawing commands, but continue layout logic.
        /// </summary>
        public Graphics destGraphics;
        /// <summary>
        /// Allows color correction/modification during the image copy.
        /// </summary>
        public ImageAttributes copyAttibutes;

        private Dictionary<string, object> data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Allows extensions to store data along with the image state
        /// </summary>
        public Dictionary<string, object> Data {
            get { return data; }
        }

        

        
        /// <summary>
        /// Disposes sourceBitmap, destGraphics, destBitmap, and copyAttributes if they are non-null
        /// </summary>
        public void Dispose() {
            try { if (sourceBitmap != null) sourceBitmap.Dispose(); } finally {
                try {
                    if (destGraphics != null) destGraphics.Dispose();
                } finally {
                    try {
                        if (destBitmap != null) destBitmap.Dispose();
                    } finally {
                        if (copyAttibutes != null) copyAttibutes.Dispose();
                    }
                }

            }
        }
    }
}
