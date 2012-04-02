using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using System.IO;
using System.Web.Hosting;
using ImageResizer.Util;
using System.Collections.Specialized;

namespace ComplexWebApplication {
    public partial class Studio : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            StringBuilder sb = new StringBuilder();
            string folder = "private/redeye/picks";

            string[] files = System.IO.Directory.GetFiles(Path.GetFullPath(Path.Combine(Path.Combine(HostingEnvironment.ApplicationPhysicalPath, "..\\Images"), folder.Replace('/', '\\'))), "*.jpg");
            foreach (string s in files) {
                if (System.IO.Path.GetFileName(s).StartsWith(".")) continue; //skip temp files
                string url = ResolveUrl("~/" + folder.TrimEnd('/') + '/' + System.IO.Path.GetFileName(s));
                
                NameValueCollection nvc = PathUtils.ParseQueryStringFriendlyAllowSemicolons("width=100;height=100;margin=5;mode=pad;bgcolor=454545");
                url = PathUtils.MergeOverwriteQueryString(url, nvc);
                sb.AppendLine("<img src='" + url + "' alt='' />");
            }

            this.images.Text = sb.ToString();

        }
    }
}