using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Web;
using ImageResizer.Caching;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Performance;
using ImageResizer.ExtensionMethods;
using ImageResizer.Resizing;
using ImageResizer.Util;

namespace ImageResizer.Plugins.Basic
{
    public class DiagnoseRequest : BuilderExtension, IPlugin, IQuerystringPlugin
    {
        private Config c;

        public IPlugin Install(Config c)
        {
            c.Plugins.add_plugin(this);
            this.c = c;
            c.Pipeline.PreHandleImage += Pipeline_PreHandleImage;
            return this;
        }

        public bool Uninstall(Config c)
        {
            c.Plugins.remove_plugin(this);
            c.Pipeline.PreHandleImage -= Pipeline_PreHandleImage;
            return true;
        }

        protected override RequestedAction PostPrepareSourceBitmap(ImageState s)
        {
            if (s.sourceBitmap == null || s.settings.Get("resizer.debug", DebugType.None) == DebugType.None ||
                !Diagnostic.AllowResponse(HttpContext.Current, c)) return RequestedAction.None;

            var log = new StringBuilder();
            s.Data["debug"] = log;

            var alt = s.Job.SourceWidth == s.sourceBitmap.Width && s.Job.SourceHeight == s.sourceBitmap.Height
                ? ""
                : $" ({s.Job.SourceWidth}x{s.Job.SourceHeight})";

            log.AppendLine(
                $"{Normalize(s.sourceBitmap.RawFormat)} {s.sourceBitmap.Width}x{s.sourceBitmap.Height} {Normalize(s.sourceBitmap.PixelFormat)}{alt} {s.Job.DecodeTicks * 1000 / Stopwatch.Frequency}ms");


            return RequestedAction.None;
        }

        private string Normalize(PixelFormat fmt)
        {
            var names = new Dictionary<PixelFormat, string>()
            {
                { PixelFormat.Format24bppRgb, "bgr24" },
                { PixelFormat.Format32bppRgb, "bgr32" },
                { PixelFormat.Format32bppArgb, "bgra32" }
            };

            return names.ContainsKey(fmt) ? names[fmt] : fmt.ToString().Replace("Format", " ");
        }


        private string Normalize(ImageFormat fmt)
        {
            var names = new Dictionary<Guid, string>()
            {
                { ImageFormat.Bmp.Guid, "bmp" },
                { ImageFormat.Emf.Guid, "emf" },
                { ImageFormat.Exif.Guid, "exif" },
                { ImageFormat.Gif.Guid, "gif" },
                { ImageFormat.Icon.Guid, "icon" },
                { ImageFormat.Jpeg.Guid, "jpg" },
                { ImageFormat.MemoryBmp.Guid, "memorybmp" },
                { ImageFormat.Png.Guid, "png" },
                { ImageFormat.Tiff.Guid, "tif" },
                { ImageFormat.Wmf.Guid, "wmf" }
            };

            return names.ContainsKey(fmt.Guid) ? names[fmt.Guid] : fmt.ToString();
        }

        protected override RequestedAction PreFlushChanges(ImageState s)
        {
            if (!s.Data.ContainsKey("debug")) return RequestedAction.None;
            var log = s.Data["debug"] as StringBuilder;
            if (log == null) return RequestedAction.None;
            var kind = s.settings.Get("resizer.debug", DebugType.None);

            log?.AppendLine(
                $"To {s.Job.ResultFileExtension} {s.destBitmap.Width}x{s.destBitmap.Height} {Normalize(s.destBitmap.PixelFormat)} ");
            log?.AppendLine(
                $"{DateTime.UtcNow:u}");
            log?.AppendLine($"RawUrl: {HttpContext.Current.Request.RawUrl}");
            log?.AppendLine(
                $"Settings: {s.settings}");
            log?.AppendLine(
                $"PathData: {s.Job.SourcePathData}");

            log?.AppendLine(
                $"Requester: {HttpContext.Current.Request.UserHostName} {HttpContext.Current.Request.UserHostAddress} {HttpContext.Current.Request.UserAgent}");
            log.AppendLine();

            switch (kind)
            {
                case DebugType.Image:
                    log.Append(new DiagnosticsReport(c, HttpContext.Current).Header());
                    var fmt = StringFormat.GenericTypographic;
                    fmt.Alignment = StringAlignment.Near;
                    fmt.LineAlignment = StringAlignment.Center;
                    fmt.Trimming = StringTrimming.None;
                    var font = new Font(FontFamily.GenericSansSerif, 10.0f, FontStyle.Regular);
                    s.destGraphics?.DrawString(log.ToString(), font, new SolidBrush(Color.DarkGray),
                        new RectangleF(9, 9, s.destBitmap.Width - 20, s.destBitmap.Height - 20),
                        StringFormat.GenericTypographic);
                    s.destGraphics?.DrawString(log.ToString(), font, new SolidBrush(Color.DarkGray),
                        new RectangleF(11, 11, s.destBitmap.Width - 20, s.destBitmap.Height - 20),
                        StringFormat.GenericTypographic);
                    s.destGraphics?.DrawString(log.ToString(), font, new SolidBrush(Color.White),
                        new RectangleF(10, 10, s.destBitmap.Width - 20, s.destBitmap.Height - 20),
                        StringFormat.GenericTypographic);
                    break;
                case DebugType.Text:
                    log.Append(new DiagnosticsReport(c, HttpContext.Current).Generate());
                    throw GetResponseException(log.ToString());
            }

            return RequestedAction.None;
        }

        private enum DebugType
        {
            [EnumString("false")] None,
            Image,
            [EnumString("true")] Text
        }

        /// <summary>
        ///     This is where we hijack the resizing process, interrupt it, and send back the json data we created.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="context"></param>
        /// <param name="e"></param>
        private void Pipeline_PreHandleImage(IHttpModule sender, HttpContext context, IResponseArgs e)
        {
            if (e.RewrittenQuerystring.Get("resizer.debug", DebugType.None) != DebugType.None)
            {
                InjectRequestMod(e as ResponseArgs);

                if (!Diagnostic.AllowResponse(context, c)) throw GetResponseException(Diagnostic.DisabledNotice(c));
            }
        }


        internal class AlternateResponseException : Exception
        {
            public byte[] ResponseData { get; set; }

            public string ContentType { get; set; }

            //public int StatusCode { get; set; }
            public AlternateResponseException(string message) : base(message)
            {
            }
        }

        private AlternateResponseException GetResponseException(string text)
        {
            var ex = new AlternateResponseException("Resizing was canceled as request debug info was needed instead");
            ex.ContentType = "text/plain";
            var sw = new StringWriter();
            ex.ResponseData = System.Text.Encoding.UTF8.GetBytes(text);
            //ex.StatusCode = 200;
            return ex;
        }

        private static void InjectRequestMod(ResponseArgs ra)
        {
            ra.ResponseHeaders.ContentType = "text/plain";

            var old = ra.ResizeImageToStream;
            ra.ResizeImageToStream = delegate(Stream s)
            {
                try
                {
                    old(s);
                }
                catch (AlternateResponseException rce)
                {
                    if (rce.ContentType != null) ra.ResponseHeaders.ContentType = rce.ContentType;

                    s.Write(rce.ResponseData, 0, rce.ResponseData.Length);
                }
            };
            ra.ResponseHeaders.DefaultHeaders["X-IR-Rewritten-Querystring"] =
                PathUtils.BuildQueryString(ra.RewrittenQuerystring);
            ra.ResponseHeaders.DefaultHeaders["X-IR-Request-Key"] = ra.RequestKey;
        }

        /// <summary>
        ///     Returns the querystrings command keys supported by this plugin.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetSupportedQuerystringKeys()
        {
            return new[] { "resizer.debug" };
        }
    }
}