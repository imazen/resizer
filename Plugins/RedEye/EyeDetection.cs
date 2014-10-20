using System;
using System.Collections.Generic;
using System.Text;
using OpenCvSharp;
using System.Drawing;
using System.Diagnostics;
using System.IO;
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
        /// <param name="storage"></param>
        /// <returns></returns>
        protected override List<ObjRect> DetectFeatures(IplImage img, CvMemStorage storage) {
            List<ObjRect> eyes = new List<ObjRect>();

            //Detect faces
            Stopwatch watch = Stopwatch.StartNew();
            CvAvgComp[] faces = Cv.HaarDetectObjects(img, Cascades["FaceCascade"], storage, 1.0850, 2, 0, new CvSize(30, 30)).ToArrayAndDispose();
            watch.Stop();
            Debug.WriteLine("face detection time = " + watch.ElapsedMilliseconds);

            watch.Reset(); watch.Start();

            //If there are no faces, look for large eye pairs
            if (faces.Length == 0) {
                CvAvgComp[] pairs = Cv.HaarDetectObjects(img, Cascades["EyePair45"], storage, 1.0850, 2, 0, new CvSize(img.Width / 4, img.Width / 20)).ToArrayAndDispose();
                if (pairs.Length > 0) {
                    //TODO!!! Uncomment and test now that CompareByNeighbors sorts correctly
                    //Array.Sort<CvAvgComp>(pairs, CompareByNeighbors); 
                    //Take the 1st most likely that actually contains eyes. We don't want to evaluate multiple eye pairs when there are no faces.
                    //If there are pairs, evalutate them all. Finding eyes within multiple pairs is unlikely
                    foreach (CvAvgComp pair in pairs) {
                        var results = DetectFeaturesInPair(img, storage, pair);
                        eyes.AddRange(results);
                        if (results.Count > 0) break;
                    }
                }
            }

            //For each face...
            foreach (CvAvgComp face in faces) {
                eyes.AddRange(DetectFeaturesInFace(img, storage, face));
            }

            watch.Stop();
            Debug.WriteLine("eye detection time = " + watch.ElapsedMilliseconds);
            return eyes;
        }

        private List<ObjRect> DetectFeaturesInFace(IplImage img, CvMemStorage storage, CvAvgComp face) {
            List<ObjRect> eyes = new List<ObjRect>();
            storage.Clear();
            //Take the top 4/8ths of the face as the region of interest
            CvRect r = face.Rect;
            r.Height = Cv.Round((double)r.Height / 2);
            img.SetROI(r);

            //Look for pairs there
            CvAvgComp[] pairs = Cv.HaarDetectObjects(img, Cascades["EyePair22"], storage, 1.0850, 2, 0, new CvSize(r.Width < 50 ? 11 : 22, r.Width < 50 ? 3 : 5)).ToArrayAndDispose();
            //Array.Sort<CvAvgComp>(pairs, CompareByNeighbors);

            //Look for individual eyes if no pairs were found
            if (pairs.Length == 0) {

                //Drop 1/2.75th off the top, leaving us with a full-width rectangle starting at 1/5.5th and stopping at 1/2th of face height.
                int aFifth = Cv.Round((double)r.Height * 2 / 5.5);
                r.Y += aFifth;
                r.Height -= aFifth;

                eyes.AddRange(DetectEyesInRegion(img, storage, r));
            }
            //If there are pairs, evalutate them all. Finding eyes within multiple pairs is unlikely
            for (var i = 0; i < pairs.Length; i++) {
                CvAvgComp pair = pairs[i]; //Adjust for ROI
                pair.Rect.X += r.X;
                pair.Rect.Y += r.Y;
                eyes.AddRange(DetectFeaturesInPair(img, storage, pair));
            }
            if (eyes.Count > 0) eyes.Add(new ObjRect(face.Rect.ToRectangleF(), FeatureType.Face));
            return eyes;

        }

        private List<ObjRect> DetectFeaturesInPair(IplImage img, CvMemStorage storage, CvAvgComp eyePair) {
            List<ObjRect> eyes = new List<ObjRect>();
            CvRect pair = eyePair.Rect;
            //Inflate 100% vertically, centering
            pair.Top -= pair.Height / 2;
            pair.Height *= 2;
            if (pair.Top < 0) { pair.Height += pair.Top; pair.Top = 0; }
            if (pair.Height >= img.Height) pair.Height = img.Height;
            if (pair.Bottom >= img.Height) pair.Top = img.Height - pair.Height;

            //Inflate 20% on each side, centering
            pair.Left -= pair.Width / 5;
            pair.Width += pair.Width / 5 * 2;
            pair.Left = Math.Max(0, pair.Left);
            pair.Width = Math.Min(img.Width - pair.Left, pair.Width);

            eyes.AddRange(DetectEyesInRegion(img, storage, pair));

            if (eyes.Count > 0) eyes.Add(new ObjRect(eyePair.Rect.ToRectangleF(), FeatureType.EyePair));
            return eyes;
        }

        private List<ObjRect> DetectEyesInRegion(IplImage img, CvMemStorage storage, CvRect region) {
            List<ObjRect> eyes = new List<ObjRect>();

            //Split the region into two overlapping rectangles
            CvRect leftEye = region;
            leftEye.Width = (int)(leftEye.Width * 0.6);

            CvRect rightEye = region;
            rightEye.Width = (int)(rightEye.Width * 0.6);
            rightEye.X += (int)(region.Width * 0.4);

            //If the eye pair or face is small enough, use 3 instead of 5
            int minEyeLength = region.Width < 80 ? 3 : 5;
            CvSize minEyeSize = new CvSize(minEyeLength, minEyeLength);

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
                CvRect left = leftEye;
                left.Y += (int)((float)left.Height * (float)vals[2] / 2.0);
                left.Height = (int)((float)left.Height * (float)vals[2]);
                CvRect right = rightEye;
                right.Height = left.Height;
                right.Y = left.Y;

                if (!foundLeft) {
                    //Search for eyes
                    storage.Clear();
                    img.SetROI(left);
                    CvAvgComp[] leyes = Cv.HaarDetectObjects(img, (int)vals[0] == 0 ? Cascades["RightEyeCascade"] : Cascades["Eye"], storage, 1.0850, (int)vals[1], 0, minEyeSize).ToArrayAndDispose();
                    //Array.Sort<CvAvgComp>(leyes, CompareByNeighbors);

                    if (leyes.Length > 0) {
                        eyes.Add(new ObjRect(leyes[0].Rect.Offset(left.Location).ToRectangleF(), FeatureType.Eye));
                        minEyeSize = new CvSize(leyes[0].Rect.Width / 4, leyes[0].Rect.Width / 4);
                        foundLeft = true;
                    }

                }

                if (!foundRight) {
                    storage.Clear();
                    img.SetROI(right);
                    CvAvgComp[] reyes = Cv.HaarDetectObjects(img, (int)vals[0] == 0 ? Cascades["LeftEyeCascade"] : Cascades["Eye"], storage, 1.0850, (int)vals[1], 0, minEyeSize).ToArrayAndDispose();
                    //Array.Sort<CvAvgComp>(reyes, CompareByNeighbors);

                    if (reyes.Length > 0) {
                        eyes.Add(new ObjRect(reyes[0].Rect.Offset(right.Location).ToRectangleF(), FeatureType.Eye));
                        minEyeSize = new CvSize(reyes[0].Rect.Width / 4, reyes[0].Rect.Width / 4);
                        foundRight = true;
                    }
                }
                if (foundLeft && foundRight) break;

            }
            return eyes;

        }



    }
}
