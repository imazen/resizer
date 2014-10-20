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
    /// <summary>
    /// Declares variables for the values determined by the Faces plugin (the original values and the values on the destination image).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DetectionResponse<T> {
        /// <summary>
        /// X value for the destination image.
        /// </summary>
        public float dx;
        /// <summary>
        /// Y value for the destination image.
        /// </summary>
        public float dy;
        /// <summary>
        /// Width value for the destination image.
        /// </summary>
        public float dw;
        /// <summary>
        /// Height value for the destination image.
        /// </summary>
        public float dh;
        /// <summary>
        /// Width value for the original image.
        /// </summary>
        public float ow;
        /// <summary>
        /// Height value for the original image.
        /// </summary>
        public float oh;
        /// <summary>
        /// X value for the cropped image (isolated facial feature).
        /// </summary>
        public float cropx;
        /// <summary>
        /// Y value for the cropped image (isolated facial feature).
        /// </summary>
        public float cropy;
        /// <summary>
        /// Width value for the cropped image (isolated facial feature).
        /// </summary>
        public float cropw;
        /// <summary>
        /// Height value for the cropped image (isolated facial feature).
        /// </summary>
        public float croph;
        /// <summary>
        /// The facial features which are being isolated.
        /// </summary>
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
