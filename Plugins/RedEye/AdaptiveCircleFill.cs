using System;
using System.Collections.Generic;
using System.Text;
using AForge.Imaging;
using System.Drawing;
using AForge.Imaging.Filters;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

namespace ImageResizer.Plugins.RedEye {

    /// <summary>
    /// Adaptive thresholding flood fill optimized for round objects. 
    /// </summary>
    public class AdaptiveCircleFill {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="red">A grayscale image filtered using a redness algorithm</param>
        /// <param name="startAt">The maximum brightness point, the place to start filling</param>
        /// <param name="origin">The origin of the search, usually the click</param>
        /// <param name="maxRadius">The maximum distance from 'startAt' to consider filling</param>
        public AdaptiveCircleFill(UnmanagedImage red, System.Drawing.Point startAt, PointF origin, float maxRadius) {
            this.red = red; 
            this.StartAt = startAt;
            MaxValue = red.Collect8bppPixelValues(new List<AForge.IntPoint>(new AForge.IntPoint[] { new AForge.IntPoint(startAt.X, startAt.Y) }))[0];
            //Set the min threshold to 4/10ths the starting point's value
            MinValue = (byte)Math.Round(0.4 * (double)MaxValue);
            MinValue = Math.Max((byte)50, MinValue);
            //Apply some arbitrary values...
            

            this.Origin = origin;
            this.MaxRadius = maxRadius;

            OriginStartOffset = Math.Sqrt((origin.X - StartAt.X) * (origin.X - StartAt.X) + (origin.Y - StartAt.Y) * (origin.Y - StartAt.Y));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="start"></param>
        /// <param name="maxEyeRadius">Should be 2-3 percent of max(width/height)</param>
        /// <param name="maxPointSearchDistance">In source pixels, the max distance from 'start' from which to look for the starting point. Good default: roughly 24 display pixels.</param>
        public static void MarkEye(UnmanagedImage image, Point start, int maxEyeRadius, float maxPointSearchDistance = 0) {
            int maxRadius = maxEyeRadius * 2 + (int)Math.Ceiling(maxPointSearchDistance);
            //Find subset
            Rectangle subset = new Rectangle(start.X - maxRadius,start.Y - maxRadius,maxRadius * 2, maxRadius * 2);
            if (subset.X < 0) { subset.Width += subset.X; subset.X = 0; }
            if (subset.Y < 0) { subset.Height += subset.Y; subset.Y = 0; }
            if (subset.Right >= image.Width) subset.Width -= (subset.Right - image.Width + 1);
            if (subset.Bottom >= image.Height) subset.Height -= (subset.Bottom - image.Height + 1);

            start.X -= subset.X;
            start.Y -= subset.Y;

            //Skip processing if we're slightly out of bounds
            if (subset.X < 0 || subset.Y < 0 || subset.Width < 0 || subset.Height < 0 || subset.Right >= image.Width || subset.Bottom >= image.Height) return;

            UnmanagedImage red = null;
            try{
                Point startAt = start;
                using (UnmanagedImage c = new Crop(subset).Apply(image)) {
                    red = new RedEyeFilter(2).Apply(c);
                    if (maxPointSearchDistance > 0) startAt = new ManualSearcher().FindMaxPixel(c,start,maxPointSearchDistance);
                }

                var fill = new AdaptiveCircleFill(red, startAt, start, maxEyeRadius * 2);
                fill.FirstPass();
                fill.SecondPass();
                fill.CorrectRedEye(image, subset.X, subset.Y);
                //fill.MarkFilledPixels(image, subset.X, subset.Y, new byte[] { 0, 255, 0, 0 });

                //fill.SetPixel(image, startAt.X + subset.X, startAt.Y + subset.Y, new byte[] { 255, 255, 0, 0 });
            }finally{
                if (red != null) red.Dispose();
            }
        }

        /// <summary>
        /// An 8-bit image with a redness algorithm applied.
        /// </summary>
        public UnmanagedImage red;

        /// <summary>
        /// The pixel at which to begin filling
        /// </summary>
        public System.Drawing.Point StartAt;
        /// <summary>
        /// The absolute minimum brightness to include. Should probably be set to 3/8ths the maximum pixel brightness.
        /// </summary>
        public byte MinValue = 80;
        /// <summary>
        /// The maximum brightness expected. Used to provide a range for threshold expectations
        /// </summary>
        public byte MaxValue = 255;


        /// <summary>
        /// The user-specified origin - used to adjust local thresholding.
        /// </summary>
        public PointF Origin;
        /// <summary>
        /// The distance between origin and startat
        /// </summary>
        public double OriginStartOffset;

        /// <summary>
        /// The maximum radius around StartAt in which to fill
        /// </summary>
        public float MaxRadius;

        /// <summary>
        /// Sum of the X values of all filled pixels.
        /// </summary>
        public long SumX = 0;
        /// <summary>
        /// Sum of the Y values of all filled pixels
        /// </summary>
        public long SumY = 0;
        /// <summary>
        /// Sum of the values of all filled pixels
        /// </summary>
        public long SumV = 0;
        /// <summary>
        /// The number of filled pixels. 
        /// </summary>
        public long FilledCount = 0;

        /// <summary>
        /// Set after the first pass, once we know the appropriate center
        /// </summary>
        public PointF WeightedCenter;
        /// <summary>
        /// Set after the first pass, once we know the farthest outlying point.
        /// </summary>
        public float FilledRadius;

        /// <summary>
        /// The average value of the filled area
        /// </summary>
        public byte FillAverage;

        /// <summary>
        /// An array matching the bitmap data, marking which pixels are considered filled.
        /// </summary>
        public bool[,] filledArray;

        /// <summary>
        /// A queue of points which have been filled, but their neighbors not yet evalutated.
        /// </summary>
        Queue<System.Drawing.Point> q;
        /// <summary>
        /// Initial fill to determine actual center and radius.
        /// </summary>
        public void FirstPass() {
            filledArray = new bool[red.Height, red.Width];
            q = new Queue<System.Drawing.Point>();
            SumX = 0;
            SumY = 0;
            FilledCount = 0;
            AddPoint(StartAt,MaxValue);
            FillPoints(false);
            WeightedCenter = new PointF((float)SumX / (float)FilledCount, (float)SumY / (float)FilledCount);
            Rectangle fillRect = GetEnclosingRect(filledArray);

            FilledRadius = (float) Math.Max(fillRect.Width, fillRect.Height) /2.0F; //(float)Math.Sqrt((double)FilledCount * 1.25  / Math.PI);
            FillAverage = (byte)(SumV / FilledCount);
        }

        private Rectangle GetEnclosingRect(bool[,] d) {
            int top = d.GetUpperBound(0);
            int left = d.GetUpperBound(1);
            int bottom = 0;
            int right = 0;
            for (int y = 0; y < d.GetUpperBound(0);y++)
                for (int x = 0; x < d.GetUpperBound(1); x++) {
                    if (d[y, x]) {
                        if (y < top) top = y;
                        if (y > bottom) bottom = y;
                        if (x < left) left = x;
                        if (x > right) right = x;
                    }
                }

            return new Rectangle(left, top, Math.Max(0,right - left), Math.Max(0,bottom - top));
        }

        private void ClearArray() {
            for (int i = 0; i < filledArray.GetLength(0); i++) {
                for (int j = 0; j < filledArray.GetLength(1); j++) {
                    filledArray[i, j] = false;
                }
            }
        }
        /// <summary>
        /// Recalculate pixels to modify based on first pass.
        /// </summary>
        public void SecondPass() {
            ClearArray();
            SumX = 0;
            SumY = 0;
            FilledCount = 0;
            AddPoint(StartAt, MaxValue);
            FillPoints(true);
        }
        /// <summary>
        /// Dequeues points from 'q',  enqueues their eligible neighbors, and loops until empty.
        /// </summary>
        private void FillPoints(bool useExistingMass) {
            
            Point p;
            byte pval;
            //double pdist;
            while (q.Count > 0) {
                //Dequeue the parent point 
                p = q.Dequeue();
                //Find its value
                pval = red.Collect8bppPixelValues(new List<AForge.IntPoint>(new AForge.IntPoint[] { new AForge.IntPoint(p.X, p.Y) }))[0];
                //Calculate its distance from the start point
                //pdist = Math.Sqrt((p.X - StartAt.X) * (p.X - StartAt.X) + (p.Y - StartAt.Y) * (p.Y - StartAt.Y));
                //Calculate the current center of balance. 
                PointF center;

                //Calculate the min est. radius based on filled pixels
                double minRadius = Math.Sqrt(FilledCount / Math.PI);
                double maxRadius;
                byte avg;

                if (useExistingMass) {
                    center = this.WeightedCenter;
                    maxRadius = this.FilledRadius * 1.5;
                    avg = this.FillAverage;
                } else {
                    center = new PointF((float)SumX / (float)FilledCount, (float)SumY / (float)FilledCount);
                    maxRadius = Math.Sqrt(FilledCount * 2.2 / Math.PI);
                    maxRadius = Math.Sqrt(FilledCount * (2.2 * Math.Cos(Math.PI / 2 * maxRadius / MaxRadius)) / Math.PI); //Adjust maxRadius to use a smaller multiplier as the size approaches the hard outer limit
                    maxRadius = Math.Max(maxRadius, minRadius); //Make sure it isn't smaller - it is possible. 

                    //What's the current average value of the filled area?
                    avg = (byte)(SumV / FilledCount);
                }
                Consider(p, pval, -1, -1, center, minRadius,maxRadius,avg);
                Consider(p, pval, 0, -1, center, minRadius, maxRadius, avg);
                Consider(p, pval, 1, -1, center, minRadius, maxRadius, avg);
                Consider(p, pval, -1, 0, center, minRadius, maxRadius, avg);
                Consider(p, pval, 1, 0, center, minRadius, maxRadius, avg);
                Consider(p, pval, -1, 1, center, minRadius, maxRadius, avg);
                Consider(p, pval, 0, 1, center, minRadius, maxRadius, avg);
                Consider(p, pval, 1, 1, center, minRadius, maxRadius, avg);
            }

        }
        private void Consider(Point parent, byte parentValue, int xoffset, int yoffset, PointF massCenter, double minRadius, double maxRadius, byte massAvgValue) {   
            Point n = new Point(parent.X + xoffset, parent.Y + yoffset);
            //Prevent out-of-bounds
            if (n.X < 0 || n.X >= red.Width || n.Y < 0 || n.Y >= red.Height) return;
            //Don't duplicate work
            if (filledArray[n.Y, n.X]) return;

            //Prevent out-of-radius
            double distance = Math.Sqrt((n.X - StartAt.X) * (n.X - StartAt.X) + (n.Y - StartAt.Y) * (n.Y - StartAt.Y));
            if (distance > MaxRadius) return; //This can be easily optimized by calculating once for the parent, and only doing exact measurments if within 2px. 

            //Get value
            byte val = red.Collect8bppPixelValues(new List<AForge.IntPoint>(new AForge.IntPoint[] { new AForge.IntPoint(n.X, n.Y) }))[0];

            //Prevent out-of-threshold
            if (val < this.MinValue) return;


            //What's the maximum variation from the average?
            double maxVar = massAvgValue - MinValue;

            //What's the maximum possible difference from the previous?
            double maxDiffVar = parentValue - MinValue;

            //Establish threshold
            double threshold = 0; 
            double mdist = Math.Sqrt((n.X - massCenter.X) * (n.X - massCenter.X) + (n.Y - massCenter.Y) * (n.Y - massCenter.Y));
            threshold = Math.Cos(Math.PI / 2 * Math.Min(1,(mdist / Math.Max(3,maxRadius)))); 


            //If either value is greater than the threshold, drop the pixel. 
            if (Math.Max(0, massAvgValue - val) / maxVar > threshold) return; //Average threshold
            if (Math.Max(0, parentValue - val) / maxDiffVar > threshold) return; //Delta threshold


            AddPoint(n, val);
            ////What's the difference from the previous?
            //int diff = parentValue - val;


            ////And never allow more than a 20%  relative drop
            //if (parentValue - val > (255 - MinValue) / 5) return;



            ////Now, let's do thresholding using (a) massCenter and radiuses, and (b) proximity to click point (using same radiuses). 
            //double mdist = Math.Sqrt((n.X - massCenter.X) * (n.X - massCenter.X) + (n.Y - massCenter.Y) * (n.Y - massCenter.Y));
            //double cdist = Math.Sqrt((n.X - Origin.X) * (n.X - Origin.X) + (n.Y - Origin.Y) * (n.Y - Origin.Y));
            ////Normalize these distances so the fade region translates to between 0 and 1
            //mdist = (mdist - minRadius) / (maxRadius - minRadius);
            //cdist = (cdist - minRadius) / (maxRadius - minRadius + OriginStartOffset);
            //if (mdist < 0) mdist = 0; //No fading
            //if (cdist < 0) cdist = 0; //No fading
            ////Average them
            ////double factor = 1 - Math.Min(1, (mdist + cdist) / 2); //Using linear scaling, translating to 0-1 multiplier


            ////If we're going out-of-bounds, we need to evalutate total as well as local thresholding.
            //double total = (double)(val - MinValue) / (double)(MaxValue - MinValue) * 0.25; // 0 to ~1
            //double diff = (double)(val - parentValue) / (double)(MaxValue - MinValue); //~-1 to ~1.
            ////Compare average distance factors to average value factors
            //if ((total + diff) / 2 > Math.Min(1, (mdist + cdist) / 2)) {
            //    //Success!
            //    AddPoint(n,val);
            //}

        }

        private void AddPoint(System.Drawing.Point p, byte val){
            filledArray[p.Y,p.X] = true;
            SumX += p.X;
            SumY += p.Y;
            SumV += val;
            FilledCount++;
            q.Enqueue(p);
        }

        public unsafe void SetPixel(UnmanagedImage target, int x, int y, byte[] color) {

            int pixelSize = System.Drawing.Image.GetPixelFormatSize(target.PixelFormat) / 8;
            if (pixelSize > 4) throw new NotSupportedException();

            Marshal.Copy(color, 0, (IntPtr)((long)target.ImageData + y * target.Stride + (x) * pixelSize), pixelSize);
        }

        public unsafe void MarkFilledPixels(UnmanagedImage target, int xoffset, int yoffset, byte[] color) {
            
            int pixelSize =   System.Drawing.Image.GetPixelFormatSize(target.PixelFormat) / 8;
            if (pixelSize > 4) throw new NotSupportedException();

            int stride = target.Stride;
            long scan0 = (long)target.ImageData;

            for (int i =0; i < filledArray.GetLength(0); i++){
                for (int j =0; j < filledArray.GetLength(1); j++){
                    if (filledArray[i,j]){
                        //byte* t = (byte*)(scan0 + (yoffset + i) * stride + (xoffset + j) * pixelSize);
                        Marshal.Copy(color,0,(IntPtr)(scan0 + (yoffset + i) * stride + (xoffset + j) * pixelSize),pixelSize);
                    }
                }
            }


        }
        /// <summary>
        /// Bulrs edges of filled red-eye.
        /// </summary>
        /// <returns></returns>
        public UnmanagedImage GetBlurredMask() {
            using (UnmanagedImage ui = UnmanagedImage.Create(filledArray.GetLength(1),filledArray.GetLength(0), PixelFormat.Format8bppIndexed)) {
                MarkFilledPixels(ui, 0, 0, new byte[] { 255 });
                return new GaussianBlur(1.25,5).Apply(ui);
            }
        }

        /// <summary>
        /// Using the fill array calculated in passes 1 and 2, apply red-eye correction to the specified image. 
        /// </summary>
        /// <param name="image"></param>
        /// <param name="ox"></param>
        /// <param name="oy"></param>
        public unsafe void CorrectRedEye(UnmanagedImage image, int ox, int oy) {
            int pixelSize = System.Drawing.Image.GetPixelFormatSize(image.PixelFormat) / 8;
            if (pixelSize > 4) throw new Exception("Invalid pixel depth");
            UnmanagedImage mask = GetBlurredMask(); ;
            try {
                long scan0 = (long)image.ImageData;
                long stride = image.Stride;
                // do the job
                byte* src;
                //Establish bounds
                int top = oy;
                int bottom = oy + mask.Height;
                int left = ox;
                int right = ox + mask.Width;

                byte* fade;
                float fadeVal;
                float gray;

                //Scan region
                for (int y = top; y < bottom; y++) {
                    src = (byte*)(scan0 + y * stride + (left * pixelSize));
                    for (int x = left; x < right; x++, src += pixelSize) {
                        if (src[RGB.R] == 0) continue; //Because 0 will crash the formula

                        //Get ptr to mask pixel
                        fade = (byte*)((long)mask.ImageData + (y - top) * mask.Stride + x - left);
                        if (*fade == 0) continue; 

                        fadeVal = (float)*fade / 255.0F;
                        //Calculate monochrome alternative
                        gray = (byte)((float)src[RGB.G] * 0.5f + (float)src[RGB.B] * 0.5f);

                        //Apply monochrome alternative using mask
                        src[RGB.R] = (byte)((fadeVal * gray) + (1.0 - fadeVal) * src[RGB.R]);
                        src[RGB.G] = (byte)((fadeVal * gray) + (1.0 - fadeVal) * src[RGB.G]);
                        src[RGB.B] = (byte)((fadeVal * gray) + (1.0 - fadeVal) * src[RGB.B]);
                        
                    }
                }
            } finally {
                if (mask != null) mask.Dispose();
            }
        }


    }
}
