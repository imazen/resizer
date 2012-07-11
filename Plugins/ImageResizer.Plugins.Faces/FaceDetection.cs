using System;
using System.Collections.Generic;
using System.Text;
using OpenCvSharp;
using System.Drawing;
using System.Diagnostics;
using System.IO;


namespace ImageResizer.Plugins.Faces {

    public class Face:IFeature {
        public Face(RectangleF rect, float accuracy) {
            this.X = rect.X;
            this.Y = rect.Y;
            this.X2 = rect.Right;
            this.Y2 = rect.Bottom;
            this.Accuracy = accuracy;
        }
        public float X { get; set; }
        public float Y { get; set; }
        public float X2 { get; set; }
        public float Y2 { get; set; }
        public float Accuracy { get; set; }
    }

    public class FaceDetection:FeatureDetectionBase<Face>{

        public FaceDetection():base(){
            MinFaces = 1;
            MaxFaces = 10;
            MinSizePercent = 3;
            ConfidenceLevelThreshold = 2;
            MinConfidenceLevel = 1;

            fileNames = new Dictionary<string, string>(){ 
            {"FaceCascade",@"haarcascade_frontalface_default.xml"} };
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
        /// Detects features on a grayscale image.
        /// </summary>
        /// <param name="img"></param>
        /// <param name="storage"></param>
        /// <returns></returns>
        protected override List<Face> DetectFeatures(IplImage img, CvMemStorage storage) {
            //Determine minimum face size
            var minSize = (int)Math.Round((double)MinSizePercent / 100.0 * Math.Min(img.Width, img.Height));

            
            //Detect faces (frontal). TODO: side
            Stopwatch watch = Stopwatch.StartNew();
            CvAvgComp[] faces = Cv.HaarDetectObjects(img, Cascades["FaceCascade"], storage, 1.0850, MinConfidenceLevel, 0, new CvSize(minSize, minSize)).ToArrayAndDispose();
            watch.Stop();
            Debug.WriteLine("Face detection time = " + watch.ElapsedMilliseconds);

            //Sort by accuracy
            Array.Sort<CvAvgComp>(faces, CompareByNeighbors);

            //Convert into feature objects list
            List<Face> features = new List<Face>(faces.Length);
            foreach (CvAvgComp face in faces) features.Add(new Face(face.Rect.ToRectangleF(), face.Neighbors));
            
            //Unless we're below MinFaces, filter out the low confidence matches.
            while (features.Count > MinFaces && features[features.Count - 1].Accuracy < ConfidenceLevelThreshold) features.RemoveAt(features.Count - 1);


            //Never return more than [MaxFaces]
            return (features.Count > MaxFaces) ? features.GetRange(0, MaxFaces) : features;
        }
    }
}
