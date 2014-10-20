using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Resizing;
using System.Globalization;
using ImageResizer.Util;
using System.Drawing.Imaging;
using System.Drawing;
using ImageResizer.Configuration;
using ImageResizer.ExtensionMethods;
using ImageResizer.Caching;
using System.IO;
using System.Collections.Specialized;
using ImageResizer.Plugins.CropAround;

namespace ImageResizer.Plugins.Faces {
    /// <summary>
    /// Human face detection plugin. Provides automatic face detection, as well as the CropAround plugin, which can even be combined in a single request (using &amp;c.focus=faces) to provide face-focused/face-preserving cropping.
    /// </summary>
    public class FacesPlugin:BuilderExtension,IPlugin,IQuerystringPlugin {
        /// <summary>
        /// Creates a new instance of the Faces plugin.
        /// </summary>
        public FacesPlugin() {
        }
        protected Config c;
        /// <summary>
        /// Adds the plugin to the given configuration container.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            this.c = c;
            c.Pipeline.PreHandleImage += Pipeline_PreHandleImage;
            return this;
        }
        /// <summary>
        /// Removes the plugin from the given configuration container.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            c.Pipeline.PreHandleImage -= Pipeline_PreHandleImage;
            return true;
        }

        /// <summary>
        /// Returns a comma-delimited list of face coordinates (x,y,x2,y2,accuracy) for the given image (path, stream, Bitmap, etc).
        /// Note that the face coordinates are relative to the unrotated, unflipped source image.
        /// ImageResizer.js can *keep* these coordinates synced during rotations/flipping if they are stored in the 'f.rects' querystring key before the 'srotate' or 'sflip' commands are applied.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public string GetFacesFromImageAsString(object image, NameValueCollection settings) {
            var faces = GetFacesFromImage(image, settings);
            StringBuilder sb = new StringBuilder();
            foreach (Face f in faces)
                sb.Append(f.X + "," + f.Y + "," + f.X2 + "," + f.Y2 + "," + f.Accuracy + ",");

            return sb.ToString().TrimEnd(',');
        }

        /// <summary>
        /// Returns a list of face objects for the given image (path, stream, Bitmap, etc).
        /// Note that the face coordinates are relative to the unrotated, unflipped source image.
        /// ImageResizer.js can *keep* these coordinates synced during rotations/flipping if they are stored in the 'f.rects' querystring key before the 'srotate' or 'sflip' commands are applied.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public List<Face> GetFacesFromImage(object image, NameValueCollection settings) {
            using (var b = c.CurrentImageBuilder.LoadImage(image, new ResizeSettings(settings))) {
                using (var detector = ConfigureDetection(settings)) {
                    return detector.DetectFeatures(b);
                }
            }
        }

        protected override RequestedAction PostPrepareSourceBitmap(ImageState s) {
            if (s.sourceBitmap == null) return RequestedAction.None;

            bool focusFaces = ("faces".Equals(s.settings["c.focus"], StringComparison.OrdinalIgnoreCase));
            bool showFaces = "true".Equals(s.settings["f.show"], StringComparison.OrdinalIgnoreCase);

            List<Face> faces = null;

            //Perform face detection for either (or both) situations
            if (showFaces || focusFaces) {
                using (var detector = ConfigureDetection(s.settings)) {
                    //Store faces
                    s.Data["faces"] = faces = detector.DetectFeatures(s.sourceBitmap);
                }
                //Store points
                List<PointF> points = new List<PointF>();
                foreach (Face r in faces) { points.Add(new PointF(r.X, r.Y)); points.Add(new PointF(r.X2, r.Y2)); }
                s.layout.AddInvisiblePolygon("faces", points.ToArray());
            }

            //Capture and rewrite requests with &c.focus=faces
            if (focusFaces) {
                //Write the face points as focus values
                List<double> focusPoints = new List<double>();
                foreach (Face r in faces) { focusPoints.Add(r.X); focusPoints.Add(r.Y); focusPoints.Add(r.X2); focusPoints.Add(r.Y2); }
                s.settings.SetList<double>("c.focus", focusPoints.ToArray(), false);
            }
            return RequestedAction.None;
        }


        protected override RequestedAction PostRenderImage(ImageState s) {
            if (!"true".Equals(s.settings["f.show"], StringComparison.OrdinalIgnoreCase) ||
                !s.layout.ContainsRing("faces") ||
                s.destBitmap == null) return RequestedAction.None;


            var newPoints = s.layout["faces"];

            using (Graphics g = Graphics.FromImage(s.destBitmap)) {

                for (var i = 0; i < newPoints.Length - 1; i += 2) {
                    float x1 = newPoints[i].X;
                    float y1 = newPoints[i].Y;
                    float x2 = newPoints[i + 1].X;
                    float y2 = newPoints[i + 1].Y;
                    float t;
                    if (x1 > x2) { t = x2; x2 = x1; x1 = t; }
                    if (y1 > y2) { t = y1; y1 = y2; y2 = t; }

                    g.DrawRectangle(Pens.Green, new Rectangle((int)x1, (int)y1, (int)(x2 - x1), (int)(y2 - y1)));
                }
            }


            return RequestedAction.None;
        }


        protected override RequestedAction Render(ImageState s) {
            bool detect = s.settings.Get("f.detect", false);
            bool getlayout = s.settings.Get("f.getlayout", false);
            if (!detect && !getlayout) return RequestedAction.None;


           
            var d = new DetectionResponse<Face>();
            try {
                //Only detect faces if it was requested.
                if (detect) using (var detector =ConfigureDetection(s.settings)) d.features = detector.DetectFeatures(s.sourceBitmap);
            } catch (TypeInitializationException e) {
                throw e;
            } catch (Exception e) {
                d.message = e.Message;
            }
            d.PopulateFrom(s);
            throw d.GetResponseException(s.settings["callback"]);
        }

        /// <summary>
        /// This is where we hijack the resizing process, interrupt it, and send back the json data we created.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="context"></param>
        /// <param name="e"></param>
        void Pipeline_PreHandleImage(System.Web.IHttpModule sender, System.Web.HttpContext context, Caching.IResponseArgs e) {
            bool detect = e.RewrittenQuerystring.Get("f.detect", false);
            bool getlayout = e.RewrittenQuerystring.Get("f.getlayout", false);
            if (!detect && !getlayout) return;

            DetectionResponse<Face>.InjectExceptionHandler(e as ResponseArgs);
        }

        public FaceDetection ConfigureDetection(NameValueCollection s) {
            
            var f = new FaceDetection();

            //Parse min/max faces
            int[] count = s.GetList<int>("f.faces",null,1,2);
            if (count == null) {
                f.MinFaces = 1;
                f.MaxFaces = 8;
            }else if (count.Length > 0){
                f.MinFaces = f.MaxFaces = count[0];
                if (count.Length > 1) f.MaxFaces = count[1];
            }

            //Parse min/default thresholds
            int[] threshold = s.GetList<int>("f.threshold",null,1,2);
            if (threshold != null && threshold.Length > 0){
                f.MinConfidenceLevel = f.ConfidenceLevelThreshold = threshold[0];
                if (threshold.Length > 1) f.ConfidenceLevelThreshold = threshold[1];
            }

            //Parse min size percent
            f.MinSizePercent = s.Get<float>("f.minsize",f.MinSizePercent);

            //Parse expandsion rules
            double[] expand = s.GetList<double>("f.expand", null, 1, 2);

            //Exapnd bounding box by requested percentage
            if (expand != null) {
                f.ExpandX = expand[0];
                f.ExpandY = expand.Length > 1 ? expand[1] : expand[0];
            }
            

            return f;
        }
        /// <summary>
        /// Returns the querystrings command keys supported by this plugin. 
        /// </summary>
        /// <returns></returns>
        public  IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "f.show", "f.detect","f.faces", "f.threshold", "f.minsize", "f.expand"};
        }
    }
}
