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
    /// JSON response image layout data
    /// </summary>
    /// <typeparam name="T">Type of object contained in the response</typeparam>
    public class DetectionResponse<T> {

        /// <summary>
        /// coordinate dx
        /// </summary>
        public float dx;

        /// <summary>
        /// coordinate dy
        /// </summary>
        public float dy;

        /// <summary>
        /// coordinate dw
        /// </summary>
        public float dw;

        /// <summary>
        /// coordinate dh
        /// </summary>
        public float dh;

        /// <summary>
        /// coordinate ow
        /// </summary>
        public float ow;

        /// <summary>
        /// coordinate oh
        /// </summary>
        public float oh;

        /// <summary>
        /// coordinate where to crop x
        /// </summary>
        public float cropx;

        /// <summary>
        /// coordinate where to crop y
        /// </summary>
        public float cropy;

        /// <summary>
        /// coordinate where to crop w
        /// </summary>
        public float cropw;

        /// <summary>
        /// coordinate where to crop h
        /// </summary>
        public float croph;

        /// <summary>
        /// List of Features for the detector to use in recognition
        /// </summary>
        public List<T> features;

        /// <summary>
        /// Error message
        /// </summary>
        public string message;

        /// <summary>
        /// Copies layout information from the given image state to the current instance. 
        /// Does not populate message or 'features' variables
        /// </summary>
        /// <param name="s">State of image being resized</param>
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

        /// <summary>
        /// Get JSON data from the ImageREsizer pipeline
        /// </summary>
        /// <param name="jsonCallback">JSON callback data</param>
        /// <returns>ALternate response that can bubble up to a method that can replace the output stream</returns>
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

        /// <summary>
        /// InjectExceptionHandler can catch the JSON respons exceptions
        /// </summary>
        /// <param name="ra">JSON Callback data to be passed through</param>
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
