using System;
using System.Collections.Generic;
using System.Text;
using OpenCvSharp;
using System.Drawing;
using System.Diagnostics;

namespace System.Runtime.CompilerServices {
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class ExtensionAttribute : Attribute {
        public ExtensionAttribute() { }
    }
}


namespace ImageResizer.Plugins.RedEye {

    public static class OpenCvExtensions{
        public static RectangleF ToRectangleF(this CvRect rect) {
            return new RectangleF(rect.X, rect.Y, rect.Width, rect.Height);
        }
        public static CvAvgComp[] ToArrayAndDispose(this CvSeq<CvAvgComp> seq) {
            CvAvgComp[] arr = seq.ToArray();
            seq.Dispose();
            return arr;
        }

        public static CvRect Offset(this CvRect rect, CvPoint offset ) {
            return new CvRect(rect.X + offset.X, rect.Y +offset.Y, rect.Width, rect.Height);
        }
}
    public class FaceDetection {

        public FaceDetection(string xmlFolder = @"..\..\Plugins\Libs\OpenCV\") {
            this.xmlFolder = xmlFolder.TrimEnd('\\') + '\\';
        }

        public string xmlFolder;

        public string FaceCascade = @"haarcascade_frontalface_default.xml";
        
        public string LeftEyeCascade = @"haarcascade_mcs_lefteye.xml";
        
        public string RightEyeCascade = @"haarcascade_mcs_righteye.xml";

        
        public string EyePair45 = @"haarcascade_mcs_eyepair_big.xml";

        
        public string EyePair22 = @"haarcascade_mcs_eyepair_small.xml";




        public List<RectangleF> DetectEyes(Bitmap b) {
            List<RectangleF> eyes = new List<RectangleF>();
            //Type Intializer Exception occurs if you reuse an appdomain. Always restart the server.
            using (IplImage orig = OpenCvSharp.BitmapConverter.ToIplImage(b))
            using (IplImage gray = new IplImage(orig.Size, BitDepth.U8, 1)) {

                //Make grayscale version
                Cv.CvtColor(orig, gray, ColorConversion.BgrToGray);

                int w = orig.Width; int h = orig.Height;
                double ratio = (double)w / (double)h;
                double scale = 1;
                if (ratio > 1) scale = (double)w / 1000;
                if (ratio <= 1) scale = (double)h / 1000;
                scale = Math.Min(1, 1 / scale);


                using (IplImage small = new IplImage(new CvSize(Cv.Round(w * scale), Cv.Round(h * scale)), BitDepth.U8, 1)) {
                    //Resize to smaller version
                    Cv.Resize(gray, small, Interpolation.Area);
                    //Equalize histogram
                    Cv.EqualizeHist(gray, gray);

                    using (CvHaarClassifierCascade cascade = Cv.Load<CvHaarClassifierCascade>(xmlFolder + FaceCascade))
                    using (CvMemStorage storage = new CvMemStorage()) {
                        storage.Clear();

                        Stopwatch watch = Stopwatch.StartNew();
                        //TODO: CvSeq must be disposed...
                        CvAvgComp[] faces = Cv.HaarDetectObjects(small, cascade, storage, 1.0850, 2, 0, new CvSize(30, 30)).ToArrayAndDispose();
                        watch.Stop();
                        Debug.WriteLine("face detection time = " + watch.ElapsedMilliseconds);

                        using (CvHaarClassifierCascade cPair = Cv.Load<CvHaarClassifierCascade>(xmlFolder + EyePair45))
                        using (CvHaarClassifierCascade cPairSmall = Cv.Load<CvHaarClassifierCascade>(xmlFolder + EyePair22))
                        using (CvHaarClassifierCascade cLeft = Cv.Load<CvHaarClassifierCascade>(xmlFolder + LeftEyeCascade))
                        using (CvHaarClassifierCascade cRight = Cv.Load<CvHaarClassifierCascade>(xmlFolder + RightEyeCascade)) {
                            watch.Reset(); watch.Start();
                            foreach (CvAvgComp face in faces) {
                                eyes.Add(face.Rect.ToRectangleF());
                                storage.Clear();
                                //Take the top 4/8ths of the face as the region of interest
                                CvRect r = face.Rect;
                                r.Height = Cv.Round((double)r.Height / 2);
                                small.SetROI(r);

                                CvAvgComp[] pairs = Cv.HaarDetectObjects(small, cPairSmall, storage, 1.0850, 2).ToArrayAndDispose();
                                foreach (CvAvgComp pair in pairs) {
                                    eyes.Add(pair.Rect.Offset(r.Location).ToRectangleF());
                                }

                                int aFifth = Cv.Round((double)r.Height * 2 / 5.5);
                                r.Y += aFifth;
                                r.Height -= aFifth;

                                CvRect leftEye = r;
                                leftEye.Width = leftEye.Width / 2;
                                leftEye.X -= leftEye.Width / 10;

                                CvRect rightEye = r;
                                rightEye.Width = rightEye.Width / 2;
                                rightEye.X += rightEye.Width + rightEye.Width / 10;


                                storage.Clear();
                                small.SetROI(leftEye);
                                CvAvgComp[] leyes = Cv.HaarDetectObjects(small, cLeft, storage, 1.0850,2,0,new CvSize(5,5)).ToArrayAndDispose(); //TODO dispose seq.
                                storage.Clear();
                                small.SetROI(rightEye);
                                CvAvgComp[] reyes = Cv.HaarDetectObjects(small, cRight, storage, 1.0850, 2, 0, new CvSize(5, 5)).ToArrayAndDispose();//TODO dispose seq.
                                if (leyes.Length > 0 && reyes.Length > 0) {
                                    eyes.Add(leyes[0].Rect.Offset(leftEye.Location).ToRectangleF());
                                    eyes.Add(reyes[0].Rect.Offset(rightEye.Location).ToRectangleF());
                                }

                            }

                            watch.Stop();
                            Debug.WriteLine("eye detection time = " + watch.ElapsedMilliseconds);

                        }


                    }
                }
                //Scale all rectangles by factor to restore to original resolution
                for (int i = 0; i < eyes.Count; i++) {
                    RectangleF e = eyes[i];
                    e.Y = (float)Math.Min(h, e.Y / scale);
                    e.X = (float)Math.Min(w, e.X / scale);
                    e.Height = (float)Math.Min(h - e.Y, e.Height / scale);
                    e.Width = (float)Math.Min(w -e.X, e.Width / scale);
                    eyes[i] = e;
                }
            }
            return eyes;
        }
    }
}
