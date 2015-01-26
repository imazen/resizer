using System;
using System.Collections.Generic;
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
            CvAvgComp[] arr = seq.ToArray();
            seq.Dispose();
            return arr;
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
    public abstract class FeatureDetectionBase<T> : IDisposable where T : IFeature {
        /// <summary>
        /// Creates new instance of FeatureDetectionBase.
        /// </summary>
        public FeatureDetectionBase() {
            var a = this.GetType().Assembly;
            //Use CodeBase if it is physical; this means we don't re-download each time we recycle. 
            //If it's a URL, we fall back to Location, which is often the shadow-copied version.
            var searchFolder = a.CodeBase.StartsWith("file:///", StringComparison.OrdinalIgnoreCase)
                                ? a.CodeBase
                                : a.Location;
            //Convert UNC paths 
            searchFolder = Path.GetDirectoryName(searchFolder.Replace("file:///", "").Replace("/", "\\"));

            searchFolders.Add(searchFolder);
            //searchFolders.Add(@"C:\Users\Administrator\Documents\resizer\Plugins\Libs\OpenCV");
        }
        public FeatureDetectionBase(string xmlFolder):this() {
            if (xmlFolder != null) searchFolders.Insert(0,xmlFolder);
        }

        protected List<string> searchFolders = new List<string>() { };

        protected Dictionary<string, string> fileNames = new Dictionary<string,string>(){ };

        protected Dictionary<string, string> Files = null;
        protected Dictionary<string, CvHaarClassifierCascade> Cascades = null;
        private void LoadFiles() {
            if (Files != null) return;

            var f = new Dictionary<string, string>();
            Cascades = new Dictionary<string, CvHaarClassifierCascade>();
            foreach (string key in fileNames.Keys) {
                string resolvedPath = null;
                foreach (string basePath in searchFolders) {
                    string full = basePath.TrimEnd('\\') + '\\' + fileNames[key];
                    if (File.Exists(Path.GetFullPath(full))) {
                        resolvedPath = Path.GetFullPath(full);
                        //An ExecutionException will occur here if multiple OpenCv instances are loaded
                        Cascades[key] = Cv.Load<CvHaarClassifierCascade>(resolvedPath);
                        break;
                    }
                }
                if (resolvedPath == null) throw new ImageResizer.ImageProcessingException("Failed to find " + fileNames[key] + " in any of the search directories. Verify the XML files have been copied to the same folder as ImageResizer.dll.");
                f[key] = resolvedPath;
            }

            Files = f;
        }

        /// <summary>
        /// Large images will be scaled down to less than scaledBounds X scaledBounds for feature detection.
        /// Defaults to 1000
        /// </summary>
        protected int scaledBounds = 1000;

        public List<T> DetectFeatures(Bitmap b) {
            LoadFiles();
            List<T> features;
            //Type Intializer Exception occurs if you reuse an appdomain. Always restart the server.
            using (IplImage orig = OpenCvSharp.Extensions.BitmapConverter.ToIplImage(b))
            using (IplImage gray = new IplImage(orig.Size, BitDepth.U8, 1)) {

                //Make grayscale version
                Cv.CvtColor(orig, gray, ColorConversion.BgrToGray);

                int w = orig.Width; int h = orig.Height;
                double ratio = (double)w / (double)h;
                double scale = 1;
                if (ratio > 1) scale = (double)w / (double)scaledBounds;
                if (ratio <= 1) scale = (double)h / (double)scaledBounds;
                scale = Math.Min(1, 1 / scale);


                using (IplImage small = new IplImage(new CvSize(Cv.Round(w * scale), Cv.Round(h * scale)), BitDepth.U8, 1)) {
                    //Resize to smaller version
                    Cv.Resize(gray, small, Interpolation.Area);
                    //Equalize histogram
                    Cv.EqualizeHist(gray, gray);

                    using (CvMemStorage storage = new CvMemStorage()) {
                        storage.Clear();
                        features = DetectFeatures(small, storage);
                    }
                }
                //Scale all rectangles by factor to restore to original resolution
                for (int i = 0; i < features.Count; i++) {
                    IFeature e = features[i];
                    e.Y = (float)Math.Min(h, e.Y / scale);
                    e.X = (float)Math.Min(w, e.X / scale);
                    e.Y2 = (float)Math.Min(h, e.Y2 / scale);
                    e.X2 = (float)Math.Min(w, e.X2 / scale);
                    
                }
            }
            return features;
        }

        protected  abstract List<T> DetectFeatures(IplImage img, CvMemStorage storage);

        protected int CompareByNeighbors(CvAvgComp a, CvAvgComp b) {
            return b.Neighbors.CompareTo(a.Neighbors);
        }

        /// <summary>
        /// Disposes all loaded cascades
        /// </summary>
        public void Dispose() {
            foreach (string s in Cascades.Keys.ToArray()) {
                if (Cascades[s] != null) {
                    Cascades[s].Dispose();
                    Cascades[s] = null;
                }
            }
        }
    }
}
