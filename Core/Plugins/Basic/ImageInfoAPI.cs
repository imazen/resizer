using System;
using System.Collections.Generic;
using System.Text;
using ImageResizer.Configuration;
using ImageResizer.Caching;
using System.IO;
using System.Collections.Specialized;
using System.Drawing;

namespace ImageResizer.Plugins.Basic {
    public class ImageInfoAPI : IPlugin, IQuerystringPlugin {
        Config c;
        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            c.Pipeline.PreHandleImage += Pipeline_PreHandleImage;
            this.c = c;
            return this;
        }

        void Pipeline_PreHandleImage(System.Web.IHttpModule sender, System.Web.HttpContext context, Caching.IResponseArgs e) {
            string info = e.RewrittenQuerystring["getinfo"];
            if (string.IsNullOrEmpty(info)) return;

            ResponseArgs ra = e as ResponseArgs;
            e.ResponseHeaders.ContentType = "application/json; charset=utf-8";

            NameValueCollection d = new NameValueCollection();
            ra.ResizeImageToStream = new ResizeImageDelegate(delegate(Stream s) {
                try {
                    using (Stream src = ra.GetSourceImage()) {
                        
                        bool attemptFastMode = src.CanSeek;
                        long orig = attemptFastMode ? src.Position : 0;
                        bool trySlowMode = !attemptFastMode;
                        if (attemptFastMode) {
                            try {
                                using (Image i = System.Drawing.Image.FromStream(src, false, false)) {
                                    d["width"] = i.Width.ToString();
                                    d["height"] = i.Height.ToString();
                                }
                            } catch {
                                trySlowMode = true;

                            }
                        }
                        if (trySlowMode){
                            if (attemptFastMode) src.Seek(orig, SeekOrigin.Begin);
                            using (Bitmap b = c.CurrentImageBuilder.LoadImage(src,new ResizeSettings(ra.RewrittenQuerystring))){
                                d["width"] = b.Width.ToString();
                                d["height"] = b.Height.ToString();
                            }
                        }
                        SimpleJson(s, d, e.RewrittenQuerystring["jsonp"]);
                    }
                } catch (FileNotFoundException) {
                    d["result"] = "404";
                    SimpleJson(s,d,e.RewrittenQuerystring["jsonp"]);
                }
            });
        }

        private void SimpleJson(Stream target, NameValueCollection data, string callbackName) {
            StreamWriter sw = new StreamWriter(target,System.Text.Encoding.UTF8);
          
            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrEmpty(callbackName)){ sb.Append(callbackName); sb.Append('(');};

            sb.Append('{');

            foreach (string key in data) {
                if (data[key] == null) continue;
                sb.Append('\''); sb.Append(key.Replace('\'', '_')); sb.Append("':'");
                sb.Append(data[key]); sb.AppendLine("',");
            }

            sb.Append('}');

            if (!string.IsNullOrEmpty(callbackName)) { sb.Append(");"); };

            sw.Write(sb.ToString());
            sw.Flush();
            
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            c.Pipeline.PreHandleImage -= Pipeline_PreHandleImage;
            return true;
        }

        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return  new string[] { "getinfo" };
        }
    }
}
