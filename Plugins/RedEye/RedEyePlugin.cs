using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using AForge.Imaging.Filters;
using System.Globalization;
using ImageResizer.Util;
using AForge;
using AForge.Imaging;
using System.Drawing.Imaging;
using System.Drawing;
using ImageResizer.Configuration;

namespace ImageResizer.Plugins.RedEye {
    public class RedEyePlugin:DetectionPlugin {
        public RedEyePlugin() {
        }

 
        protected override RequestedAction PostRenderImage(ImageState s) {

            if (s.destBitmap == null) return RequestedAction.None;
            string str = null;
            int i = 0;

            if (!string.IsNullOrEmpty(s.settings["r.eyes"])) {
                double[] eyes = Utils.parseList(s.settings["r.eyes"], 0);
                // lock source bitmap data
                BitmapData data = s.destBitmap.LockBits(
                    new Rectangle(0, 0, s.destBitmap.Width, s.destBitmap.Height),
                    ImageLockMode.ReadWrite, s.destBitmap.PixelFormat);

                try {
                    UnmanagedImage ui = new UnmanagedImage(data);

                    for (i = 0; i < eyes.Length / 5; i ++) {
                        var x = eyes[i * 5];
                        var y = eyes[i * 5 + 1];
                        var w = eyes[i * 5 + 2];
                        var h = eyes[i * 5 + 3];
                        var a = eyes[i * 5 + 4];
                        var cx = x + w / 2;
                        var cy = y + h / 2;
                        var radius = Math.Sqrt(w * w + h * h) /2;


                        AdaptiveCircleFill.MarkEye(ui, new System.Drawing.Point((int)cx, (int)cy),(int)Math.Ceiling(radius),(float)(a > 6 ? radius /2 : radius ));
                            
                        /*if (eyes[i + 2] > 0) {
                            AdaptiveCircleFill.MarkEye(ui, new System.Drawing.Point((int)eyes[i], (int)eyes[i + 1]),(int)Math.Ceiling(0.025 * Math.Max(ui.Width,ui.Height)),24);
                            //CorrectRedEye(ui, (int)eyes[i], (int)eyes[i + 1], (int)eyes[i + 2]);
                        }else{
                            SemiAutoCorrectRedEye(ui, (int)eyes[i], (int)eyes[i + 1]);
                        }*/
                    }

                } finally {
                    // unlock image
                    s.destBitmap.UnlockBits(data);
                }
            }

             if ("true".Equals(s.settings["r.autoeyes"], StringComparison.OrdinalIgnoreCase)) {
                 List<ObjRect> eyes = new FaceDetection(@"C:\Users\Administrator\Documents\resizer\Plugins\Libs\OpenCV").DetectFeatures(s.sourceBitmap);
                 List<PointF> points = new List<PointF>();
                 foreach(ObjRect r in eyes) { points.Add(new PointF(r.X,r.Y)); points.Add(new PointF(r.X2,r.Y2));}
                 PointF[] newPoints = c.CurrentImageBuilder.TranslatePoints(points.ToArray(),s.originalSize,new ResizeSettings(s.settings));
                 using (Graphics g = Graphics.FromImage(s.destBitmap)){
                     for(i =0; i < newPoints.Length -1; i+=2){
                         float x1 = newPoints[i].X;
                         float y1 = newPoints[i].Y;
                         float x2 = newPoints[i + 1].X;
                         float y2 = newPoints[i + 1].Y;
                         float t; 
                         if (x1 > x2){ t = x2; x2  =x1; x1 = t;}
                         if (y1 > y2){ t = y1; y1 = y2; y2 = t;} 

                         g.DrawRectangle(eyes[i /2].Feature == FeatureType.Eye ? Pens.Green : Pens.Gray,new Rectangle((int)x1,(int)y1,(int)(x2-x1),(int)(y2-y1)));
                     }
                 }
             }

            str = s.settings["r.filter"]; //radius
            if (!string.IsNullOrEmpty(str) && int.TryParse(str, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out i)) {
                using (s.destBitmap) {
                    s.destBitmap = new RedEyeFilter((short)i).Apply(s.destBitmap);
                }

                //Sobel only supports 8bpp grayscale images.
                //true/false
                if ("true".Equals(s.settings["r.sobel"], StringComparison.OrdinalIgnoreCase)) {
                    new SobelEdgeDetector().ApplyInPlace(s.destBitmap);

                    str = s.settings["r.threshold"]; //radius
                    if (!string.IsNullOrEmpty(str) && int.TryParse(str, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out i))
                        new Threshold(i).ApplyInPlace(s.destBitmap);

                }
                //Canny Edge Detector only supports 8bpp grayscale images.
                //true/false
                if ("true".Equals(s.settings["r.canny"], StringComparison.OrdinalIgnoreCase)) {
                    new CannyEdgeDetector().ApplyInPlace(s.destBitmap);
                }

                int sn = 1;
                str = s.settings["r.sn"]; //multiple negative weights
                if (string.IsNullOrEmpty(str) || int.TryParse(str, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out sn)) sn = 1;
                    


                str = s.settings["r.conv"]; //convolution multiplier
                if (!string.IsNullOrEmpty(str) && int.TryParse(str, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out i))
                    new Convolution(EyeKernels.Scale(EyeKernels.GetStandardKernel(i),1,sn)).ApplyInPlace(s.destBitmap);

                str = s.settings["r.econv"]; //convolution multiplier
                if (!string.IsNullOrEmpty(str) && int.TryParse(str, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out i))
                    new Convolution(EyeKernels.Scale(EyeKernels.GetElongatedKernel(i), 1, sn)).ApplyInPlace(s.destBitmap);
                

            }

            //TODO - add grayscale?

            //For adding fax-like thresholding, use BradleyLocalThresholding

            //For trimming solid-color whitespace, use Shrink

            return RequestedAction.None;
        }

        public override IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "r.detecteyes","r.conv","r.econv","r.sn","r.canny","r.threshold","r.sobel","r.filter","r.eyes","r.autoeyes"};
        }
    }
}
