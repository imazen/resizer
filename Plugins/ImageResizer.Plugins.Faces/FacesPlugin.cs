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

namespace ImageResizer.Plugins.Faces {
    public class FacesPlugin:BuilderExtension,IPlugin,IQuerystringPlugin {
        public FacesPlugin() {
        }


        protected Config c;
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            this.c = c;
            c.Pipeline.PreHandleImage += Pipeline_PreHandleImage;
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            c.Pipeline.PreHandleImage -= Pipeline_PreHandleImage;
            return true;
        }

       
 
        protected override RequestedAction PostRenderImage(ImageState s) {

            if (s.destBitmap == null) return RequestedAction.None;
            string str = null;
            int i = 0;

             if ("true".Equals(s.settings["f.show"], StringComparison.OrdinalIgnoreCase)) {
                 List<Face> eyes = new FaceDetection() { MinFaces = 1, MaxFaces = 1 }.DetectFeatures(s.sourceBitmap);
                 List<PointF> points = new List<PointF>();
                 foreach(Face r in eyes) { points.Add(new PointF(r.X,r.Y)); points.Add(new PointF(r.X2,r.Y2));}
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
                         
                         g.DrawRectangle(Pens.Green,new Rectangle((int)x1,(int)y1,(int)(x2-x1),(int)(y2-y1)));
                     }
                 }
             }

            return RequestedAction.None;
        }

        class RedEyeData {
            public float dx;
            public float dy;
            public float dw;
            public float dh;
            public float ow;
            public float oh;
            public float cropx;
            public float cropy;
            public float cropw;
            public float croph;
            public List<Face> features;
            public string message;
        }


        protected override RequestedAction Render(ImageState s) {
            bool detect = NameValueCollectionExtensions.Get(e.RewrittenQuerystring, "f.detect", false);
            bool getlayout = NameValueCollectionExtensions.Get(e.RewrittenQuerystring, "f.getlayout", false);
            if (!detect && !getlayout) return RequestedAction.None;


            var ex = new ResizingCanceledException("Resizing was canceled as JSON data was requested instead");

            RedEyeData d = new RedEyeData();
            try {
                //Only detect eyes if it was requested.
                if (detect) d.features = ConfigureDetection(s.settings).DetectFeatures(s.sourceBitmap);
            } catch (TypeInitializationException e) {
                throw e;
            } catch (Exception e) {
                d.message = e.Message;
            }
            d.ow = s.originalSize.Width;
            d.oh = s.originalSize.Height;
            d.cropx = s.copyRect.X;
            d.cropy = s.copyRect.Y;
            d.cropw = s.copyRect.Width;
            d.croph = s.copyRect.Height;
            RectangleF dest = PolygonMath.GetBoundingBox(s.layout["image"]);
            d.dx = dest.X;
            d.dy = dest.Y;
            d.dw = dest.Width;
            d.dh = dest.Height;
            ex.ContentType = "application/json; charset=utf-8";
            StringWriter sw = new StringWriter();
            new Newtonsoft.Json.JsonSerializer().Serialize(sw, d);
            string prefix = string.IsNullOrEmpty(s.settings["callback"]) ? "" : s.settings["callback"] + "(";
            string suffix = string.IsNullOrEmpty(prefix) ? "" : ");";
            ex.ResponseData = UTF8Encoding.UTF8.GetBytes(prefix + sw.ToString() + suffix);
            ex.StatusCode = 200;

            throw ex;
        }

        /// <summary>
        /// This is where we hijack the resizing process, interrupt it, and send back the json data we created.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="context"></param>
        /// <param name="e"></param>
        void Pipeline_PreHandleImage(System.Web.IHttpModule sender, System.Web.HttpContext context, Caching.IResponseArgs e) {
            bool detect = NameValueCollectionExtensions.Get(e.RewrittenQuerystring, "f.detect", false);
            bool getlayout = NameValueCollectionExtensions.Get(e.RewrittenQuerystring, "f.getlayout", false);
            if (!detect && !getlayout) return;

            ResponseArgs ra = e as ResponseArgs;
            e.ResponseHeaders.ContentType = "application/json; charset=utf-8";

            var old = ra.ResizeImageToStream;
            ra.ResizeImageToStream = new ResizeImageDelegate(delegate(Stream s) {
                try {
                    old(s);
                } catch (ResizingCanceledException rce) {
                    s.Write(rce.ResponseData, 0, rce.ResponseData.Length);
                }
            });
        }

        public FaceDetection ConfigureDetection(NameValueCollection s) {
            
            var f = new FaceDetection();

            //Parse min/max faces
            int[] count = NameValueCollectionExtensions.GetList<int>(s,"f.faces",null,1,2);
            if (count == null) {
                f.MinFaces = 1;
                f.MaxFaces = 1;
            }else if (count.Length > 0{
                f.MinFaces = f.MaxFaces = count[0];
                if (count.Length > 1) f.MaxFaces = count[1];
            }

            //Parse min/default thresholds
            int[] threshold = NameValueCollectionExtensions.GetList<int>(s,"f.threshold",null,1,2);
            if (threshold != null && threshold.Length > 0){
                f.MinConfidenceLevel = f.ConfidenceLevelThreshold = threshold[0];
                if (threshold.Length > 1) f.ConfidenceLevelThreshold = threshold[1];
            }

            //Parse min size percent
            f.MinSizePercent = NameValueCollectionExtensions.Get<float>(s,"f.minsize",f.MinSizePercent);

            return f;
        }

        public override IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "f.show", "f.detect","f.faces", "f.threshold", "f.minsize"};
        }
    }
}
