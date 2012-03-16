using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace ImageResizer.Plugins.CustomOverlay {
    public enum CoordinateSpace {
        UncroppedImage
    }
    public class Overlay {
        /// <summary>
        /// A 4-point array that describes a parallelogram within which to draw the overlay image
        /// </summary>
        public PointF[] Poly { get; set; }

        /// <summary>
        /// The maximum width and height of the overlay. (width = dist(x1,y1,x2,y2), height = dest(x2,y2,x3,y3))
        /// </summary>
        public SizeF MaxOverlaySize { get; set; }

        /// <summary>
        /// A virtual path to the overlay image. 
        /// </summary>
        public string OverlayPath { get; set; }

        /// <summary>
        /// How to align the overlay image within the given rectangle
        /// </summary>
        public ContentAlignment Align { get; set; }

        /// <summary>
        /// What coordinate space the points in parallelogram and distance in MaxOverlaySize are relative to
        /// </summary>
        public CoordinateSpace PointSpace { get; set; }

        /// <summary>
        /// Hashes all contained data in the effort to keep DiskCache from preventing an update
        /// I'm not positive about this implementation - it probably shouldn't be used for lookup, but it should be good enough for the disk cache
        /// </summary>
        /// <returns></returns>
        public int GetDataHashCode() {
            int hash = 0xbac2d3;
            hash ^= (int)(MaxOverlaySize.Width * 10);
            hash ^= (int)(MaxOverlaySize.Height * 10.5);
            if (OverlayPath != null) hash ^= OverlayPath.GetHashCode();
            hash ^= (int)Align ^ (int)PointSpace << 5;

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
