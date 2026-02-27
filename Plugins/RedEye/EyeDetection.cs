// Copyright (c) Imazen LLC.
// No part of this project, including this file, may be copied, modified,
// propagated, or distributed except as permitted in COPYRIGHT.txt.
// Licensed under the GNU Affero General Public License, Version 3.0.
// Commercial licenses available at http://imageresizing.net/
using System;
using System.Collections.Generic;
using System.Text;
using OpenCvSharp;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using ImageResizer.Plugins.Faces;


namespace ImageResizer.Plugins.RedEye {

    /// <summary>
    /// Types of facial features detected.
    /// </summary>
    public enum FeatureType {
        /// <summary>
        /// Individual eyes.
        /// </summary>
        Eye,
        /// <summary>
        /// Pairs of eyes.
        /// </summary>
        EyePair,
        /// <summary>
        /// Faces.
        /// </summary>
        Face
    }
    /// <summary>
    /// Creates a rectangle in which facial features are isolated.
    /// </summary>
    public class ObjRect : IFeature {
        /// <summary>
        /// Declares coordinates of rectangle for isolating facial features.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="type"></param>
        public ObjRect(RectangleF rect, FeatureType type) {
            this.X = rect.X;
            this.Y = rect.Y;
            this.X2 = rect.Right;
            this.Y2 = rect.Bottom;
            this.Feature = type;
            this.Accuracy = 0;
        }
        /// <summary>
        /// X coordinate of top left point of the facial features recognition rectangle.
        /// </summary>
        public float X { get; set; }
        /// <summary>
        /// Y coordinate of the facial features recognition rectangle.
        /// </summary>
        public float Y { get; set; }
        /// <summary>
        /// X coordinate of bottom right point of the facial features recognition rectangle.
        /// </summary>
        public float X2 { get; set; }
        /// <summary>
        /// Y coordinate of bottom right point of the facial features recognition rectangle.
        /// </summary>
        public float Y2 { get; set; }
        /// <summary>
        /// Confidence level for facial recognition rectangle
        /// </summary>
        public float Accuracy { get; set; }
        /// <summary>
        /// Which feature is being isolated.
        /// </summary>
        public FeatureType Feature { get; set; }
    }
    /// <summary>
    /// Eye detection feature.
    /// </summary>
    public class EyeDetection : FeatureDetectionBase<ObjRect> {
        /// <summary>
        /// Eye detection feature.
        /// </summary>
        public EyeDetection()
            : base() {
                // Register the RedEye assembly for cascade resource extraction
                FileLocator.ResourceAssemblies.Add(typeof(EyeDetection).Assembly);

                this.fileNames = new Dictionary<string, string>(){
                        {"FaceCascade",@"haarcascade_frontalface_default.xml"},
            {"LeftEyeCascade" , @"haarcascade_mcs_lefteye.xml"},
            {"RightEyeCascade" , @"haarcascade_mcs_righteye.xml"},
            {"EyePair45" , @"haarcascade_mcs_eyepair_big.xml"},
            {"EyePair22" , @"haarcascade_mcs_eyepair_small.xml"},
            {"Eye" , @"haarcascade_eye.xml"}};
        }


        /// <summary>
        /// Detects features on a grayscale image.
        /// </summary>
        /// <param name="img"></param>
        /// <returns></returns>
        protected override List<ObjRect> DetectFeatures(Mat img) {
            List<ObjRect> eyes = new List<ObjRect>();

            //Detect faces
            Stopwatch watch = Stopwatch.StartNew();
            Rect[] faces = BorrowCascade("FaceCascade", c => c.DetectMultiScale(img, 1.0850, 2, 0, new OpenCvSharp.Size(30, 30), new OpenCvSharp.Size(0, 0)));
            watch.Stop();
            Debug.WriteLine("face detection time = " + watch.ElapsedMilliseconds);

            watch.Reset(); watch.Start();

            //If there are no faces, look for large eye pairs
            if (faces.Length == 0) {
                Rect[] pairs = BorrowCascade("EyePair45", c => c.DetectMultiScale(img, 1.0850, 2, 0, new OpenCvSharp.Size(img.Width / 4, img.Width / 20), new OpenCvSharp.Size(0, 0)));
                if (pairs.Length > 0) {
                    foreach (Rect pair in pairs) {
                        var results = DetectFeaturesInPair(img, new DetectedObject(pair, 0));
                        eyes.AddRange(results);
                        if (results.Count > 0) break;
                    }
                }
            }

            //For each face...
            foreach (Rect face in faces) {
                eyes.AddRange(DetectFeaturesInFace(img, new DetectedObject(face, 0)));
            }

            watch.Stop();
            Debug.WriteLine("eye detection time = " + watch.ElapsedMilliseconds);
            return eyes;
        }

        private List<ObjRect> DetectFeaturesInFace(Mat img, DetectedObject face) {
            List<ObjRect> eyes = new List<ObjRect>();
            //Take the top 4/8ths of the face as the region of interest
            Rect r = face.Rect;
            r.Height = (int)Math.Round((double)r.Height / 2);

            var clampedR = ClampRect(r, img.Width, img.Height);
            if (clampedR.Width <= 0 || clampedR.Height <= 0) return eyes;

            //Look for pairs there
            Rect[] pairs;
            using (var roi = new Mat(img, clampedR)) {
                pairs = BorrowCascade("EyePair22", c => c.DetectMultiScale(roi, 1.0850, 2, 0, new OpenCvSharp.Size(r.Width < 50 ? 11 : 22, r.Width < 50 ? 3 : 5), new OpenCvSharp.Size(0, 0)));
            }

            //Look for individual eyes if no pairs were found
            if (pairs.Length == 0) {

                //Drop 1/2.75th off the top, leaving us with a full-width rectangle starting at 1/5.5th and stopping at 1/2th of face height.
                int aFifth = (int)Math.Round((double)r.Height * 2 / 5.5);
                r.Y += aFifth;
                r.Height -= aFifth;

                eyes.AddRange(DetectEyesInRegion(img, r));
            }
            //If there are pairs, evaluate them all. Finding eyes within multiple pairs is unlikely
            for (var i = 0; i < pairs.Length; i++) {
                Rect pairRect = pairs[i];
                //Adjust for ROI offset
                pairRect.X += r.X;
                pairRect.Y += r.Y;
                eyes.AddRange(DetectFeaturesInPair(img, new DetectedObject(pairRect, 0)));
            }
            if (eyes.Count > 0) eyes.Add(new ObjRect(face.Rect.ToRectangleF(), FeatureType.Face));
            return eyes;

        }

        private List<ObjRect> DetectFeaturesInPair(Mat img, DetectedObject eyePair) {
            List<ObjRect> eyes = new List<ObjRect>();
            Rect pair = eyePair.Rect;
            //Inflate 100% vertically, centering
            int origTop = pair.Y;
            pair.Y -= pair.Height / 2;
            pair.Height *= 2;
            if (pair.Y < 0) { pair.Height += pair.Y; pair.Y = 0; }
            if (pair.Height >= img.Height) pair.Height = img.Height;
            if (pair.Y + pair.Height > img.Height) pair.Y = img.Height - pair.Height;

            //Inflate 20% on each side, centering
            int widthExpand = pair.Width / 5;
            pair.X -= widthExpand;
            pair.Width += widthExpand * 2;
            pair.X = Math.Max(0, pair.X);
            pair.Width = Math.Min(img.Width - pair.X, pair.Width);

            eyes.AddRange(DetectEyesInRegion(img, pair));

            if (eyes.Count > 0) eyes.Add(new ObjRect(eyePair.Rect.ToRectangleF(), FeatureType.EyePair));
            return eyes;
        }

        private List<ObjRect> DetectEyesInRegion(Mat img, Rect region) {
            List<ObjRect> eyes = new List<ObjRect>();

            //Split the region into two overlapping rectangles
            Rect leftEye = region;
            leftEye.Width = (int)(leftEye.Width * 0.6);

            Rect rightEye = region;
            rightEye.Width = (int)(rightEye.Width * 0.6);
            rightEye.X += (int)(region.Width * 0.4);

            //If the eye pair or face is small enough, use 3 instead of 5
            int minEyeLength = region.Width < 80 ? 3 : 5;
            OpenCvSharp.Size minEyeSize = new OpenCvSharp.Size(minEyeLength, minEyeLength);

            List<object[]> vars = new List<object[]>();
            vars.Add(new object[] { 0, 3, 0.5f });
            vars.Add(new object[] { 0, 3, 0.7f });
            vars.Add(new object[] { 0, 3, 1.0f });
            vars.Add(new object[] { 0, 2, 0.5f });
            vars.Add(new object[] { 0, 2, 0.7f });
            vars.Add(new object[] { 0, 2, 1.0f });
            vars.Add(new object[] { 0, 1, 0.5f });
            vars.Add(new object[] { 0, 1, 0.7f });
            vars.Add(new object[] { 1, 1, 1.0f });
            vars.Add(new object[] { 1, 1, 0.5f });
            vars.Add(new object[] { 1, 1, 0.7f });
            vars.Add(new object[] { 1, 1, 1.0f });

            bool foundLeft = false, foundRight = false;

            foreach (object[] vals in vars) {
                Rect left = leftEye;
                left.Y += (int)((float)left.Height * (float)vals[2] / 2.0);
                left.Height = (int)((float)left.Height * (float)vals[2]);
                Rect right = rightEye;
                right.Height = left.Height;
                right.Y = left.Y;

                if (!foundLeft) {
                    //Search for eyes
                    var clampedLeft = ClampRect(left, img.Width, img.Height);
                    if (clampedLeft.Width > 0 && clampedLeft.Height > 0) {
                        using (var leftRoi = new Mat(img, clampedLeft)) {
                            Rect[] leyes = BorrowCascade((int)vals[0] == 0 ? ("RightEyeCascade") : ("Eye"), c => c.DetectMultiScale(leftRoi, 1.0850, (int)vals[1], 0, minEyeSize, new OpenCvSharp.Size(0, 0)));

                            if (leyes.Length > 0) {
                                eyes.Add(new ObjRect(leyes[0].OffsetRect(new OpenCvSharp.Point(clampedLeft.X, clampedLeft.Y)).ToRectangleF(), FeatureType.Eye));
                                minEyeSize = new OpenCvSharp.Size(leyes[0].Width / 4, leyes[0].Width / 4);
                                foundLeft = true;
                            }
                        }
                    }
                }

                if (!foundRight) {
                    var clampedRight = ClampRect(right, img.Width, img.Height);
                    if (clampedRight.Width > 0 && clampedRight.Height > 0) {
                        using (var rightRoi = new Mat(img, clampedRight)) {
                            Rect[] reyes = BorrowCascade((int)vals[0] == 0 ? ("LeftEyeCascade") : ("Eye"), c => c.DetectMultiScale(rightRoi, 1.0850, (int)vals[1], 0, minEyeSize, new OpenCvSharp.Size(0, 0)));

                            if (reyes.Length > 0) {
                                eyes.Add(new ObjRect(reyes[0].OffsetRect(new OpenCvSharp.Point(clampedRight.X, clampedRight.Y)).ToRectangleF(), FeatureType.Eye));
                                minEyeSize = new OpenCvSharp.Size(reyes[0].Width / 4, reyes[0].Width / 4);
                                foundRight = true;
                            }
                        }
                    }
                }
                if (foundLeft && foundRight) break;

            }
            return eyes;

        }

        static Rect ClampRect(Rect r, int imgWidth, int imgHeight) {
            int x = Math.Max(0, r.X);
            int y = Math.Max(0, r.Y);
            int right = Math.Min(imgWidth, r.X + r.Width);
            int bottom = Math.Min(imgHeight, r.Y + r.Height);
            return new Rect(x, y, Math.Max(0, right - x), Math.Max(0, bottom - y));
        }


    }
}
