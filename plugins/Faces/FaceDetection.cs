// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
﻿using System;
using System.Collections.Generic;
using System.Text;
using OpenCvSharp;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using ImageResizer.Util;


namespace ImageResizer.Plugins.Faces {
    /// <summary>
    /// Creates a rectangle to frame the face in face recognition.
    /// </summary>
    public class Face:IFeature {
        /// <summary>
        /// Creates a new instance of Face
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="accuracy"></param>
        public Face(RectangleF rect, float accuracy) {
            this.X = rect.X;
            this.Y = rect.Y;
            this.X2 = rect.Right;
            this.Y2 = rect.Bottom;
            this.Accuracy = accuracy;
        }
        /// <summary>
        /// Sets the upper left hand corner X coordinates.
        /// </summary>
        public float X { get; set; }
        /// <summary>
        /// Sets the upper left hand corner Y coordinates
        /// </summary>
        public float Y { get; set; }
        /// <summary>
        /// Sets the lower right hand corner X coordinates
        /// </summary>
        public float X2 { get; set; }
        /// <summary>
        /// Sets the lower right hand corner Y coordinates.
        /// </summary>
        public float Y2 { get; set; }
        /// <summary>
        /// Confidence of accuracy in setting X and Y coordinates.
        /// </summary>
        public float Accuracy { get; set; }
    }
    /// <summary>
    /// Provides a simple API for face detection
    /// </summary>
    public class FaceDetection:FeatureDetectionBase<Face>{

        /// <summary>
        /// Creates a new instance of FaceDetection
        /// </summary>
        public FaceDetection():base(){
            MinFaces = 1;
            MaxFaces = 10;
            MinSizePercent = 4;
            ConfidenceLevelThreshold = 5;
            MinConfidenceLevel = 3;

            ExpandX = 0;
            ExpandY = 0;
            fileNames = new Dictionary<string, string>(){ 
            {"FaceCascade",@"haarcascade_frontalface_default.xml"},
                { "FaceCascadeAlt",@"haarcascade_frontalface_alt.xml"},
                { "FaceCascadeAlt2",@"haarcascade_frontalface_alt2.xml"},
                {"FaceCascadeAltTree",@"haarcascade_frontalface_alt_tree.xml"},
                {"FaceProfile",@"haarcascade_profileface.xml"},
                {"Eye",@"haarcascade_eye.xml"},
            };
        }
        /// <summary>
        /// The minimum number of faces expected
        /// </summary>
        public int MinFaces { get; set; }
        /// <summary>
        /// The maximum number of faces wanted
        /// </summary>
        public int MaxFaces { get; set; }

        /// <summary>
        /// The smallest face that will be detected, represented in a 1..100 percentage of the (smaller of width and height).
        /// Defaults to 3 percent (3.0f)
        /// </summary>
        public float MinSizePercent { get; set; }

        /// <summary>
        /// The minimum number of agreeing matches required for a face rectangle to be returned. 
        /// This rule isn't applied if we don't have [MinFaces] number of faces.
        /// </summary>
        public int ConfidenceLevelThreshold { get; set; }

        /// <summary>
        /// The minimum number of agreeing matches required for a face rectangle to be evaluated
        /// </summary>
        public int MinConfidenceLevel { get; set; }

        /// <summary>
        /// The percentage by which to expand each face rectangle horizontally after detection. To expand 5% each side, set to 0.1
        /// </summary>
        public double ExpandX { get; set; }

        /// <summary>
        /// The percentage by which to expand each face rectangle vertically after detection. To expand 20% on the top and bottom, set to 0.4
        /// </summary>
        public double ExpandY { get; set; }

        static long totalTime = 0;
        static long count = 0;
        /// <summary>
        /// Detects features on a grayscale image.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="storage"></param>
        /// <returns></returns>
        protected override List<Face> DetectFeatures(IplImage img, CvMemStorage storage) {
            
            //Determine minimum face size
            var minSize = Math.Max(12, (int)Math.Round((double)MinSizePercent / 100.0 * Math.Min(img.Width, img.Height)));

            
            //Detect faces (frontal). 
            Stopwatch watch = Stopwatch.StartNew();
            

            CvAvgComp[] faces = BorrowCascade("FaceCascadeAlt", c => Cv.HaarDetectObjects(img, c, storage, 1.0850, MinConfidenceLevel, HaarDetectionType.DoCannyPruning, new CvSize(minSize, minSize), new CvSize(0,0)).ToArrayAndDispose());
            
            //Sort by accuracy
            Array.Sort<CvAvgComp>(faces, CompareByNeighbors);

            //Convert into feature objects list
            List<Face> features = new List<Face>(faces.Length);
            foreach (CvAvgComp face in faces) features.Add(new Face(PolygonMath.ScaleRect(face.Rect.ToRectangleF(),ExpandX,ExpandY), face.Neighbors));

            // Doesn't add much, and would have to be deduplicated.
            //CvAvgComp[] profiles = BorrowCascade("FaceProfile", c => Cv.HaarDetectObjects(img, c, storage, 1.2, MinConfidenceLevel + 2, HaarDetectionType.FindBiggestObject | HaarDetectionType.DoRoughSearch | HaarDetectionType.DoCannyPruning, new CvSize(img.Width / 8, img.Height / 8), new CvSize(0, 0)).ToArrayAndDispose());
            //foreach (CvAvgComp face in profiles) features.Add(new Face(PolygonMath.ScaleRect(face.Rect.ToRectangleF(), ExpandX, ExpandY), face.Neighbors));


            // Test for eyes, if faces > 20 pixels
            foreach (var face in features) {
                var w = (int) (face.X2 - face.X);
                var h = (int) ((face.Y2 - face.Y) * 0.6);
                if (w > 20) {
                    img.SetROI((int) face.X, (int) face.Y, w, h);
                    storage.Clear(); 
                    CvAvgComp[] eyes = BorrowCascade("Eye",
                        c => Cv.HaarDetectObjects(img, c, storage, 1.0850, 4, HaarDetectionType.FindBiggestObject | HaarDetectionType.DoRoughSearch,
                                   new CvSize(4, 4), new CvSize(img.Width / 2, img.Height / 2))
                               .ToArrayAndDispose());
                    if (eyes.Length == 0) {
                        // Halve the estimated accuracy if there are no eyes detected
                        face.Accuracy = face.Accuracy / 2;
                        // We never want to boost accuracy, because the walls have eyes
                    }
                }
            }

            


            //Unless we're below MinFaces, filter out the low confidence matches.
            while (features.Count > MinFaces && features[features.Count - 1].Accuracy < ConfidenceLevelThreshold) features.RemoveAt(features.Count - 1);


            watch.Stop();
            totalTime += watch.ElapsedMilliseconds;
            count++;
            Debug.WriteLine($"Face detection time: {watch.ElapsedMilliseconds}ms  (avg {totalTime / count}ms)");


            //Never return more than [MaxFaces]
            return (features.Count > MaxFaces) ? features.GetRange(0, MaxFaces) : features;
        }
    }
}
