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
using System.IO;
using ImageResizer.Caching;

namespace ImageResizer.Plugins.RedEye {
    public class DetectionPlugin : BuilderExtension, IPlugin, IQuerystringPlugin {
        public DetectionPlugin() {
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

        class RedEyeData {
            public float dx;
            public float dy;
            public float dw;
            public float dh;
            public float cropx;
            public float cropy;
            public float cropw;
            public float croph;
            public List<ObjRect> features;
        }

        protected override RequestedAction Render(ImageState s) {
            bool detecteyes = Utils.getBool(s.settings, "r.detecteyes", false);
            bool getlayout = Utils.getBool(s.settings, "r.getlayout", false);
            if (!detecteyes && !getlayout) return RequestedAction.None;


            var ex = new ResizingCanceledException("Resizing was canceled as JSON data was requested instead");

            RedEyeData d = new RedEyeData();
            //Only detect eyes if it was requested.
            if (detecteyes) d.features = new FaceDetection().DetectFeatures(s.sourceBitmap);

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
            string suffix = string.IsNullOrEmpty(prefix)  ? "" : ");";
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
            bool detecteyes = Utils.getBool(e.RewrittenQuerystring, "r.detecteyes", false);
            bool getlayout = Utils.getBool(e.RewrittenQuerystring, "r.getlayout", false);
            if (!detecteyes && !getlayout) return;

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

        public virtual IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "r.detecteyes"};
        }
    }
}
