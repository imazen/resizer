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
using ImageResizer.ExtensionMethods;
using ImageResizer.Plugins.Faces;
using ImageResizer.Caching;

namespace ImageResizer.Plugins.RedEye {
    /// <summary>
    /// Provides automatic and manual red-eye detection and correction. 
    /// </summary>
    public class RedEyePlugin : BuilderExtension, IPlugin, IQuerystringPlugin {
        /// <summary>
        /// Creates a new instance of RedEyePlugin
        /// </summary>
        public RedEyePlugin() {
        }

        protected Config c;
        /// <summary>
        /// Adds the plugin to the given configuration container
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
        /// Removes the plugin from the given configuration container
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            c.Pipeline.PreHandleImage -= Pipeline_PreHandleImage;
            return true;
        }

        protected override RequestedAction Render(ImageState s) {
            if (base.Render(s) == RequestedAction.Cancel) return RequestedAction.Cancel;

            bool detecteyes = s.settings.Get<bool>("r.detecteyes", false);
            bool getlayout = s.settings.Get<bool>("r.getlayout", false);
            if (detecteyes || getlayout) {


                var d = new DetectionResponse<ObjRect>();
                try {
                    //Only detect eyes if it was requested.
                    if (detecteyes) using (var ed = new EyeDetection()) { d.features = ed.DetectFeatures(s.sourceBitmap); }
                } catch (TypeInitializationException e) {
                    throw e;
                } catch (Exception e) {
                    d.message = e.Message;
                }
                d.PopulateFrom(s);
                throw d.GetResponseException(s.settings["callback"]);
            }


            if (s.sourceBitmap == null) return RequestedAction.None;

            if (!string.IsNullOrEmpty(s.settings["r.eyes"])) {
                double[] eyes = s.settings.GetList<double>("r.eyes", 0);
                // lock source bitmap data
                BitmapData data = s.sourceBitmap.LockBits(
                    new Rectangle(0, 0, s.sourceBitmap.Width, s.sourceBitmap.Height),
                    ImageLockMode.ReadWrite, s.sourceBitmap.PixelFormat);

                try {
                    UnmanagedImage ui = new UnmanagedImage(data);

                    for (var i = 0; i < eyes.Length / 5; i++) {
                        var x = eyes[i * 5];
                        var y = eyes[i * 5 + 1];
                        var w = eyes[i * 5 + 2];
                        var h = eyes[i * 5 + 3];
                        var a = eyes[i * 5 + 4];
                        var cx = x + w / 2;
                        var cy = y + h / 2;
                        var radius = Math.Sqrt(w * w + h * h) / 2;


                        AdaptiveCircleFill.MarkEye(ui, new System.Drawing.Point((int)cx, (int)cy), (int)Math.Ceiling(radius), (float)(a > 6 ? radius / 4 : radius));
                    }

                } finally {
                    // unlock image
                    s.sourceBitmap.UnlockBits(data);
                }
            }
            return RequestedAction.None;

        }

 
        protected override RequestedAction PostRenderImage(ImageState s) {

            if (s.destBitmap == null) return RequestedAction.None;
            string str = null;
            int i = 0;



             if ("true".Equals(s.settings["r.autoeyes"], StringComparison.OrdinalIgnoreCase)) {
                 List<ObjRect> eyes;
                 using (var ed = new EyeDetection()) eyes = ed.DetectFeatures(s.sourceBitmap);

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
                //Note to future self: sobel/canny/eye kernel convolutions were not helpful; they were dead ends.
             }
            return RequestedAction.None;
        }


        /// <summary>
        /// This is where we hijack the resizing process, interrupt it, and send back the json data we created.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="context"></param>
        /// <param name="e"></param>
        void Pipeline_PreHandleImage(System.Web.IHttpModule sender, System.Web.HttpContext context, Caching.IResponseArgs e) {
            if (e.RewrittenQuerystring.Get("r.detecteyes", false) ||
                e.RewrittenQuerystring.Get("r.getlayout", false))
            {
                DetectionResponse<ObjRect>.InjectExceptionHandler(e as ResponseArgs);
            }
        }

        /// <summary>
        /// Returns the querystrings command keys supported by this plugin. 
        /// </summary>
        /// <returns></returns>
        public  IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "r.detecteyes", "r.getlayout", "r.conv","r.econv","r.sn","r.canny","r.threshold","r.sobel","r.filter","r.eyes","r.autoeyes"};
        }
    }
}
