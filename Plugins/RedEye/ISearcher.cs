using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using AForge.Imaging;

namespace ImageResizer.Plugins.RedEye {
    /// <summary>
    /// Like I could resist this name...
    /// </summary>
    public interface ISearcher {

        RectangleF LocateEye(Bitmap img, Rectangle bounds, PointF estimatedPosition, float positionalWeight);
    }
}
