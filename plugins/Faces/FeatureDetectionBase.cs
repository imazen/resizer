// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
﻿using System;
using System.Collections.Generic;
 using System.Diagnostics;
 using System.Linq;
using System.Text;
using System.Drawing;
using OpenCvSharp;
using System.IO;

namespace ImageResizer.Plugins.Faces {
    public static class OpenCvExtensions {
        public static RectangleF ToRectangleF(this CvRect rect) {
            return new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
        }
        public static CvAvgComp[] ToArrayAndDispose(this CvSeq<CvAvgComp> seq) {
            using (seq)
            {
                return seq.ToArray();
            }
        }

        public static CvRect Offset(this CvRect rect, CvPoint offset) {
            return new CvRect(rect.X + offset.X, rect.Y + offset.Y, rect.Width, rect.Height);
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
        
        protected TR BorrowCascade<TR>(string fileNameKey, Func<CvHaarClassifierCascade, TR> operation)
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
            
            IplImage orig = null;
            IplImage gray = null;
            IplImage gray2 = null;
            IplImage small = null;
            try {
                
                orig = OpenCvSharp.Extensions.BitmapConverter.ToIplImage(b);
                
                
                gray = new IplImage(orig.Size, BitDepth.U8, 1);
                //gray2 = new IplImage(orig.Size, BitDepth.U8, 1);

                //Make grayscale version
                Cv.CvtColor(orig, gray, ColorConversion.BgrToGray); //TODO, try a different color space
                //Cv.EqualizeHist(gray, gray2);

                var w = orig.Width;
                var h = orig.Height;
                Cv.ReleaseImage(orig);
                orig = null;


                var ratio =  w /  h;
                double scale = 1;
                if (ratio > 1) scale = w / (double) scaledBounds;
                if (ratio <= 1) scale =  h / (double) scaledBounds;
                scale = Math.Min(1, 1 / scale);


                small = new IplImage(new CvSize(Cv.Round(w * scale), Cv.Round(h * scale)), BitDepth.U8, 1);
                
                //Resize to smaller version
                Cv.Resize(gray, small, Interpolation.Area); //TODO: try a better algorithm
                Cv.ReleaseImage(gray);
                gray = null;

                features = StoragePool.Shared.Borrow("features", s =>
                {
                    s.Clear();
                    watch.Stop();
                    var f =  DetectFeatures(small, s);
                    watch.Start();
                    return f;
                }, 3000);

                //Scale all rectangles by factor to restore to original resolution
                foreach (IFeature e in features) {
                    e.Y = (float) Math.Min(h, e.Y / scale);
                    e.X = (float) Math.Min(w, e.X / scale);
                    e.Y2 = (float) Math.Min(h, e.Y2 / scale);
                    e.X2 = (float) Math.Min(w, e.X2 / scale);
                }
            } finally {
                if (gray != null) Cv.ReleaseImage(gray);
                if (gray2 != null) Cv.ReleaseImage(gray2);
                if (orig != null) Cv.ReleaseImage(orig);
                if (small != null) Cv.ReleaseImage(small);
            }
            watch.Stop();
            Debug.WriteLine($"Face detection prep time: {watch.ElapsedMilliseconds}ms");

            return features;
        }

        protected  abstract List<T> DetectFeatures(IplImage img, CvMemStorage storage);

        protected int CompareByNeighbors(CvAvgComp a, CvAvgComp b) {
            return b.Neighbors.CompareTo(a.Neighbors);
        }

      
    }
}
