using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using ImageResizer.Caching;
using ImageResizer.Configuration;
using ImageResizer.Configuration.Performance;
using ImageResizer.Resizing;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.Basic
{
    public class DiagnoseRequest: BuilderExtension, IPlugin, IQuerystringPlugin
    {
        Config c;

        public IPlugin Install(Configuration.Config c)
        {
            c.Plugins.add_plugin(this);
            this.c = c;
            c.Pipeline.PreHandleImage += Pipeline_PreHandleImage;
            return this;
        }

        public bool Uninstall(Configuration.Config c)
        {
            c.Plugins.remove_plugin(this);
            c.Pipeline.PreHandleImage -= Pipeline_PreHandleImage;
            return true;
        }

        protected override RequestedAction PostPrepareSourceBitmap(ImageState s)
        {
            if (s.sourceBitmap == null || s.settings.Get("resizer.debug", DebugType.None) == DebugType.None || !Diagnostic.AllowResponse(HttpContext.Current, this.c)) return RequestedAction.None;

            var log = new StringBuilder();
            s.Data["debug"] = log;

            log.AppendLine(
                $"Source image {s.sourceBitmap.Width}x{s.sourceBitmap.Height} {Normalize(s.sourceBitmap.RawFormat)} {s.sourceBitmap.PixelFormat.ToString()}");

            return RequestedAction.None;
        }


        protected override RequestedAction PostRenderImage(ImageState s)
        {





            return RequestedAction.None;
        }

        string Normalize(ImageFormat fmt)
        {
            var names = new Dictionary<Guid, string>() {

                {ImageFormat.Bmp.Guid, "bmp"},
                {ImageFormat.Emf.Guid, "emf"},
                {ImageFormat.Exif.Guid, "exif"},
                {ImageFormat.Gif.Guid, "gif"},
                {ImageFormat.Icon.Guid, "icon"},
                {ImageFormat.Jpeg.Guid, "jpeg"},
                {ImageFormat.MemoryBmp.Guid, "memorybmp"},
                {ImageFormat.Png.Guid, "png"},
                {ImageFormat.Tiff.Guid, "tiff"},
                {ImageFormat.Wmf.Guid, "wmf"}
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
                    $"SourcePathData: {s.Job.SourcePathData} Source {s.Job.SourceWidth}x{s.Job.SourceHeight}");
            log?.AppendLine(
                $"Decoded in {s.Job.DecodeTicks * 1000 / Stopwatch.Frequency}ms");
        

            log?.AppendLine(
                $"PostRenderImage settings: {s.settings.ToString()}");
            log?.AppendLine(
                $"Request.RawUrl: {HttpContext.Current.Request.RawUrl}");
            log?.AppendLine(
                $"Requester: {HttpContext.Current.Request.UserHostName} {HttpContext.Current.Request.UserHostAddress} {HttpContext.Current.Request.UserAgent}");
            log.AppendLine();
            
            switch (kind) {
                case DebugType.Image:
                    log.Append(new DiagnosticsReport(c, HttpContext.Current).Header());
                    var fmt = StringFormat.GenericTypographic;
                    fmt.Alignment = StringAlignment.Near;
                    fmt.LineAlignment = StringAlignment.Center;
                    fmt.Trimming = StringTrimming.None;
                    var font = new Font(FontFamily.GenericSansSerif, 18.0f, FontStyle.Bold);
                    s.destGraphics?.DrawString(log.ToString(), font, new SolidBrush(Color.DarkGray), new RectangleF(9, 9, s.destBitmap.Width - 19, s.destBitmap.Height - 19), StringFormat.GenericTypographic);
                    s.destGraphics?.DrawString(log.ToString(), font, new SolidBrush(Color.DarkGray), new RectangleF(11, 11, s.destBitmap.Width - 21, s.destBitmap.Height - 21), StringFormat.GenericTypographic);
                    s.destGraphics?.DrawString(log.ToString(),font, new SolidBrush(Color.White), new RectangleF(10,10,s.destBitmap.Width - 20, s.destBitmap.Height - 20), StringFormat.GenericTypographic);
                    break;
                case DebugType.Text:
                    log.Append(new DiagnosticsReport(c, HttpContext.Current).Generate());
                    throw GetResponseException(log.ToString());
            }
            return RequestedAction.None;

        }

        enum DebugType
        {
            [EnumString("false")]
            None,
            Image,
            [EnumString("true")]
            Text 
        }

        /// <summary>
        /// This is where we hijack the resizing process, interrupt it, and send back the json data we created.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="context"></param>
        /// <param name="e"></param>
        void Pipeline_PreHandleImage(System.Web.IHttpModule sender, System.Web.HttpContext context, Caching.IResponseArgs e)
        {
            if (e.RewrittenQuerystring.Get("resizer.debug", DebugType.None) != DebugType.None) {
                InjectRequestMod(e as ResponseArgs);

                if (!Diagnostic.AllowResponse(context, this.c)) {
                    throw GetResponseException(Diagnostic.DisabledNotice(c));
                }
            }
        }
        
        
       


        internal class AlternateResponseException : Exception
        {
            public byte[] ResponseData { get; set; }
            public string ContentType { get; set; }
            //public int StatusCode { get; set; }
            public AlternateResponseException(string message) : base(message) { }

        }
         AlternateResponseException GetResponseException(string text)
        {
            var ex = new AlternateResponseException("Resizing was canceled as request debug info was needed instead");
            ex.ContentType = "text/plain";
            StringWriter sw = new StringWriter();
            ex.ResponseData = System.Text.Encoding.UTF8.GetBytes(text);
            //ex.StatusCode = 200;
            return ex;
        }

        static void InjectRequestMod(ResponseArgs ra)
        {
            ra.ResponseHeaders.ContentType = "text/plain";

            var old = ra.ResizeImageToStream;
            ra.ResizeImageToStream = delegate (Stream s) {
                try
                {
                    old(s);
                }
                catch (AlternateResponseException rce)
                {
                    if (rce.ContentType != null) {
                        ra.ResponseHeaders.ContentType = rce.ContentType;
                    }
                   
                    s.Write(rce.ResponseData, 0, rce.ResponseData.Length);
                }
            };
            ra.ResponseHeaders.DefaultHeaders["X-IR-Rewritten-Querystring"] = Util.PathUtils.BuildQueryString(ra.RewrittenQuerystring);
            ra.ResponseHeaders.DefaultHeaders["X-IR-Request-Key"] = ra.RequestKey;
            
            
        }

        /// <summary>
        /// Returns the querystrings command keys supported by this plugin. 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetSupportedQuerystringKeys()
        {
            return new string[] { "resizer.debug" };
        }
    }

}
