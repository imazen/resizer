using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImageResizer.Resizing;
using System.Drawing;
using ImageResizer.Util;
using System.IO;
using ImageResizer.Caching;

namespace ImageResizer.Plugins.Faces {
    public class DetectionResponse<T> {
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
        public List<T> features;
        public string message;

        /// <summary>
        /// Copies layout information from the given image state to the current instance. 
        /// Does not populate message or 'features' variables
        /// </summary>
        /// <param name="s"></param>
        public void PopulateFrom(ImageState s) {

            this.ow = s.originalSize.Width;
            this.oh = s.originalSize.Height;
            this.cropx = s.copyRect.X;
            this.cropy = s.copyRect.Y;
            this.cropw = s.copyRect.Width;
            this.croph = s.copyRect.Height;
            RectangleF dest = PolygonMath.GetBoundingBox(s.layout["image"]);
            this.dx = dest.X;
            this.dy = dest.Y;
            this.dw = dest.Width;
            this.dh = dest.Height;
        }

        public AlternateResponseException GetResponseException(string jsonCallback) {
            var ex = new AlternateResponseException("Resizing was canceled as JSON data was requested instead");
            ex.ContentType = "application/json; charset=utf-8";
            StringWriter sw = new StringWriter();
            new Newtonsoft.Json.JsonSerializer().Serialize(sw, this);
            string prefix = string.IsNullOrEmpty(jsonCallback) ? "" : jsonCallback + "(";
            string suffix = string.IsNullOrEmpty(prefix) ? "" : ");";
            ex.ResponseData = UTF8Encoding.UTF8.GetBytes(prefix + sw.ToString() + suffix);
            ex.StatusCode = 200;
            return ex;
        }

        public static void InjectExceptionHandler(ResponseArgs ra) {
            ra.ResponseHeaders.ContentType = "application/json; charset=utf-8";

            var old = ra.ResizeImageToStream;
            ra.ResizeImageToStream = new ResizeImageDelegate(delegate(Stream s) {
                try {
                    old(s);
                } catch (AlternateResponseException rce) {
                    s.Write(rce.ResponseData, 0, rce.ResponseData.Length);
                }
            });
        }
    }
}
