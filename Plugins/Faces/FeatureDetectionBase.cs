// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
using System;
using System.Collections.Generic;
 using System.Diagnostics;
 using System.Linq;
using System.Text;
using System.Drawing;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.IO;

namespace ImageResizer.Plugins.Faces {

    /// <summary>
    /// Represents a detected feature with its bounding rectangle and confidence.
    /// Replaces CvAvgComp from OpenCvSharp 2.x.
    /// </summary>
    public struct DetectedObject {
        public Rect Rect;
        public int Neighbors;

        public DetectedObject(Rect rect, int neighbors) {
            Rect = rect;
            Neighbors = neighbors;
        }
    }

    public static class OpenCvExtensions {
        public static RectangleF ToRectangleF(this Rect rect) {
            return new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static Rect OffsetRect(this Rect rect, OpenCvSharp.Point offset) {
            return new Rect(rect.X + offset.X, rect.Y + offset.Y, rect.Width, rect.Height);
        }
    }
    /// <summary>
    /// Represents a detected feature, such as a face, eye, or eye pair
    /// </summary>
    public interface IFeature {
        /// <summary>
        /// First X coordinate of the detected feature.
        /// </summary>
        float X { get; set; }
        /// <summary>
        /// First Y coordinate of the detected feature.
        /// </summary>
        float Y { get; set; }
        /// <summary>
        /// Second X coordinate of the detected feature.
        /// </summary>
        float X2 { get; set; }
        /// <summary>
        /// Fifth Y coordinate of the detected feature (just kidding, it's the second).
        /// </summary>
        float Y2 { get; set; }
    }

    /// <summary>
    /// Not thread safe.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class FeatureDetectionBase<T>  where T : IFeature {
        /// <summary>
        /// Creates new instance of FeatureDetectionBase.
        /// </summary>
        public FeatureDetectionBase()
        {
        }

        protected Dictionary<string, string> fileNames;

        protected TR BorrowCascade<TR>(string fileNameKey, Func<CascadeClassifier, TR> operation)
        {
            string name;
            if (fileNames != null && fileNames.TryGetValue(fileNameKey, out name) == true) {
                if (name != null) {
                    return CascadePool.Shared.Borrow(name, operation, 10000);
                }
            }
            throw new ImageProcessingException(
                "Failed to find a file name associated with key " + fileNameKey);
        }



        /// <summary>
        /// Large images will be scaled down to less than scaledBounds X scaledBounds for feature detection.
        /// Defaults to 1000
        /// </summary>
        protected int scaledBounds = 800;

        public List<T> DetectFeatures(Bitmap b)
        {
            var watch = Stopwatch.StartNew();
            List<T> features;

            //Type Initializer Exception occurs if you reuse an appdomain. Always restart the server.

            using (var orig = BitmapConverter.ToMat(b))
            using (var gray = new Mat())
            {
                //Make grayscale version
                Cv2.CvtColor(orig, gray, ColorConversionCodes.BGR2GRAY);

                var w = orig.Width;
                var h = orig.Height;

                var ratio =  w /  h;
                double scale = 1;
                if (ratio > 1) scale = w / (double) scaledBounds;
                if (ratio <= 1) scale =  h / (double) scaledBounds;
                scale = Math.Min(1, 1 / scale);

                using (var small = new Mat())
                {
                    //Resize to smaller version
                    Cv2.Resize(gray, small, new OpenCvSharp.Size((int)Math.Round(w * scale), (int)Math.Round(h * scale)), 0, 0, InterpolationFlags.Area);

                    watch.Stop();
                    features = DetectFeatures(small);
                    watch.Start();
                }

                //Scale all rectangles by factor to restore to original resolution
                foreach (IFeature e in features) {
                    e.Y = (float) Math.Min(h, e.Y / scale);
                    e.X = (float) Math.Min(w, e.X / scale);
                    e.Y2 = (float) Math.Min(h, e.Y2 / scale);
                    e.X2 = (float) Math.Min(w, e.X2 / scale);
                }
            }
            watch.Stop();
            Debug.WriteLine($"Face detection prep time: {watch.ElapsedMilliseconds}ms");

            return features;
        }

        protected  abstract List<T> DetectFeatures(Mat img);

        protected int CompareByNeighbors(DetectedObject a, DetectedObject b) {
            return b.Neighbors.CompareTo(a.Neighbors);
        }


    }
}
