using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Drawing;

namespace ImageResizer.Plugins.CustomOverlay {
    /// <summary>
    /// Provides a single overlay from the querystring. Understands &amp;customoverlay.coords=x1,y2,x2,y2,x3,y3,x4,y4&amp;customoverlay.align=topright&amp;customoverlay.image=alphanumeric.png
    /// </summary>
    public class QuerystringOverlayProvider:IOverlayProvider, IQuerystringPlugin {

        /// <summary>
        /// A virtual path specifying the folder containing the image
        /// </summary>
        public string OverlayFolder { get; set; }

        public string ValidImageChars { get; set; }

        public QuerystringOverlayProvider(NameValueCollection args) {
            ValidImageChars = !string.IsNullOrEmpty(args["validImageChars"]) ? args["validImageChars"] : "a-zA-Z0-9. -_";
            OverlayFolder = args["overlayFolder"];
        }

        public IEnumerable<Overlay> GetOverlays(string virtualPath, System.Collections.Specialized.NameValueCollection query) {
            string poly = query["customoverlay.coords"];
            string align = query["customoverlay.align"];
            string image = query["customoverlay.image"];
            string w = query["customoverlay.magicwidth"];
            string h = query["customoverlay.magicheight"];
            if (string.IsNullOrEmpty(poly) || string.IsNullOrEmpty(image)) return null; //Don't process this image, it's not ours

            

            string[] coords = poly.Split(',');
            
            if (coords.Length != 8 && coords.Length != 4) return null; //Not valid coords

            Overlay o = new Overlay();
            //Parse points
            o.Poly = new PointF[4];
            for (int i = 0; i < 4; i++) {
                if (coords.Length == 8) {
                    o.Poly[i] = new PointF(float.Parse(coords[i * 2]), float.Parse(coords[i * 2 + 1]));
                } else {
                    o.Poly[i] = new PointF(float.Parse(coords[(i == 3 || i == 0) ? 0 : 2]), float.Parse(coords[(i == 0 || i == 1) ? 1 : 3]));
                }
            }
            //Parse alignment
            o.Align = string.IsNullOrEmpty(align) ?  ContentAlignment.MiddleCenter : (ContentAlignment)Enum.Parse(typeof(ContentAlignment), align,true);

            //Parse magic sizes
            if (!string.IsNullOrEmpty(w)) o.PolyWidthInLogoPixels = float.Parse(w);
            if (!string.IsNullOrEmpty(h)) o.PolyHeightInLogoPixels = float.Parse(h);


            //Build path
            o.OverlayPath = OverlayFolder.TrimEnd('/') + '/' + Util.PathUtils.RemoveNonMatchingChars(image, ValidImageChars);

            return new Overlay[] { o };
        }

        public IEnumerable<string> GetSupportedQuerystringKeys() {
            return new string[] { "customoverlay.coords", "customoverlay.image" };
        }
    }
}
