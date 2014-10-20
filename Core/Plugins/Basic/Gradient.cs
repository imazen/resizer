using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Drawing2D;
using ImageResizer.Util;
using System.IO;
using System.Drawing.Imaging;
using ImageResizer.ExtensionMethods;

namespace ImageResizer.Plugins.Basic {
    /// <summary>
    /// Allows gradients to be dynamically generated like so:
    /// /gradient.png?color1=white&amp;color2=black&amp;angle=40&amp;width=20&amp;height=100
    /// </summary>
    public class Gradient: IPlugin, IQuerystringPlugin, IVirtualImageProvider {
        public bool FileExists(string virtualPath, System.Collections.Specialized.NameValueCollection queryString) {
            return (virtualPath.EndsWith("/gradient.png", StringComparison.OrdinalIgnoreCase));
        }

        public IVirtualFile GetFile(string virtualPath, System.Collections.Specialized.NameValueCollection queryString) {
            return new GradientVirtualFile(queryString);
        }

        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] {"color1","color2", "angle", "width", "height" };
        }

        public IPlugin Install(Configuration.Config c) {
            c.Plugins.add_plugin(this);
            return this;
        }

        public bool Uninstall(Configuration.Config c) {
            c.Plugins.remove_plugin(this);
            return true;
        }


        public class GradientVirtualFile : IVirtualFile, IVirtualBitmapFile, IVirtualFileSourceCacheKey {
            public GradientVirtualFile(NameValueCollection query) { this.query = new ResizeSettings(query); }
            public string VirtualPath {
                get { return "gradient.png"; }
            }

            protected ResizeSettings query;

            public System.IO.Stream Open() {
                MemoryStream ms = new MemoryStream();
                using (Bitmap b = GetBitmap()) {
                    b.Save(ms, ImageFormat.Png);
                }
                ms.Seek(0, SeekOrigin.Begin);
                return ms;
            }

            public System.Drawing.Bitmap GetBitmap() {
                Bitmap b = null;
                try {
                    int w = query.Width > 0 ? query.Width : (query.MaxWidth > 0 ? query.MaxWidth : 8);
                    int h = query.Height > 0 ? query.Height : (query.MaxHeight > 0 ? query.MaxHeight : 8);
                    float angle = query.Get<float>("angle", 0);
                    Color c1 = ParseUtils.ParseColor(query["color1"], Color.White);
                    Color c2 = ParseUtils.ParseColor(query["color2"], Color.Black);
                    b = new Bitmap(w, h);

                    using (Graphics g = Graphics.FromImage(b)) 
                    using (Brush brush = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0,0,w,h),c1,c2,angle)){
                        g.FillRectangle(brush, 0, 0, w, h);
                    }
                } catch {
                    if (b != null) b.Dispose();
                    throw;
                }
                return b;
            }

            public string GetCacheKey(bool includeModifiedDate) {
                return VirtualPath + PathUtils.BuildQueryString(query.Keep("width", "height", "w", "h", "maxwidth", "maxheight", "angle", "color1", "color2"));
            }
        }
    }
}
