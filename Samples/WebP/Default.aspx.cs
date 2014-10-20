using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ImageResizer;
using System.Text;

namespace WebP {
    public partial class _Default : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            string[] images = new string[] { "/private/m.jpg","tulip-leaf.jpg", "sample.tif", "Sun_256.png", "red-leaf.jpg", "quality-original.jpg", "fountain-small.jpg", "Sun_256.png" };
            string[] cropModes = new string[] { "width=500;height=200&mode=max", "crop=300,300,800,500" };
            var dict = new string[]{
                    "format=png","lossless=true",
                    "format=png","lossless=true;noalpha=true",
                    "quality=100","quality=100",
                    "quality=90","quality=78",
                    "quality=80","quality=65",
                    "quality=70","quality=55",
                    "quality=50","quality=40",
                    "quality=40","quality=30",
                     "quality=20","quality=10",
                     "quality=10","quality=3",
                    "quality=5","quality=0"
            };

            StringBuilder sb = new StringBuilder();
            foreach (string img in images) {
                foreach (string cropMode in cropModes) {
                    for (int i = 0; i < dict.Length; i += 2) {
                        string a = dict[i];
                        string b = dict[i + 1];

                        var ia = new Instructions(cropMode + ";" + a);
                        var ib = new Instructions(cropMode + ";" + b);
                        if (string.IsNullOrEmpty(ia.Format)) ia.Format = "jpg";
                        ib.Format = "webp";
                        if (ib.JpegQuality != null) { ib["noalpha"] = "true"; ib["bgcolor"] = "white"; }
                        sb.Append("<tr><td>");
                        sb.Append("<h6>" + a + "</h6>");
                        sb.Append("<img src=\"" + img + ia.ToQueryString() + "\" /><br/>");
                        ia["showbytes"] = "true";
                        sb.Append("<img src=\"" + img + ia.ToQueryString() + "\" />");
                        sb.Append("</td><td>");
                        sb.Append("<h6>" + b + "</h6>");
                        sb.Append("<img src=\"" + img + ib.ToQueryString() + "\" /><br/>");
                        ib["showbytes"] = "true";
                        sb.Append("<img src=\"" + img + ib.ToQueryString() + "\" />");
                        sb.Append("</td></tr>\n");

                    }
                }

            }
            lit.Text = sb.ToString();
                            
        }
    }
}