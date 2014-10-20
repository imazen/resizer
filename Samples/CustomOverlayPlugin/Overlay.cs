using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ImageResizer.Plugins.CustomOverlay {
    /// <summary>
    /// Specifies a image to overlay, the overlay paralellogram, the alignemnt, and (optionally) a pair of weird scaling factors. 
    /// </summary>
    public class Overlay {
        public Overlay() {
            RespectOnlyMatchingBound = true;
        }
        /// <summary>
        /// A 4-point array that describes a parallelogram within which to draw the overlay image. Specified in the native coordinate space of the original image (not the overlay).
        /// </summary>
        public PointF[] Poly { get; set; }

        /// <summary>
        /// Defines the width of the parallelogram in the coordinate space of the overlay image. 
        /// If greater than zero, will be used to determine the (maximum) percentage of the width of the poly that the overlay should occupy. 
        /// Ie, MaxWidth = PolyWidth * (OverlayWidth / PolyWidthInLogoPixels)
        /// </summary>
        public float PolyWidthInLogoPixels { get; set; }

        /// <summary>
        /// Defines the height of the parallelogram in the coordinate space of the overlay image. If greater than zero, will be used to determine the (maximum) percentage of the height of the poly that the overlay should occupy.
        /// Ie, MaxHeight = PolyHeight * (OverlayHeight / PolyHeightInLogoPixels)
        /// </summary>
        public float PolyHeightInLogoPixels { get; set; }

        /// <summary>
        /// If true (default), and if PolyWidthInLogoPixels is set, only the polygon's width will be respected. If true, and PolyHeightInLogoPixels is set, only the polygon's height will be respected.
        /// </summary>
        public bool RespectOnlyMatchingBound { get; set; }

        /// <summary>
        /// A virtual path to the overlay image. 
        /// </summary>
        public string OverlayPath { get; set; }

        /// <summary>
        /// How to align the overlay image within the given rectangle
        /// </summary>
        public ContentAlignment Align { get; set; }

        /// <summary>
        /// Hashes all contained data in the effort to keep DiskCache from preventing an update
        /// I'm not positive about this implementation - it probably shouldn't be used for lookup, but it should be good enough for the disk cache
        /// </summary>
        /// <returns></returns>
        public int GetDataHashCode() {
            int hash = RespectOnlyMatchingBound ? 0xbac2d3 : 0xbbac486;
            hash ^= (int)(PolyWidthInLogoPixels * 10) << 5;
            hash ^= (int)(PolyHeightInLogoPixels * 10) << 5;

            if (OverlayPath != null) hash ^= OverlayPath.GetHashCode();
            hash ^= (int)Align;

            int poly = 0;

            int offset = 0;
            foreach (PointF p in Poly) {
                poly ^= (int)(p.X * 10) << (offset % 24); offset += 11;
                poly ^= (int)(p.Y * 10) << (offset % 24);
            }

            hash ^= poly;
            return hash;
        }
    }
}
