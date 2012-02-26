using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ImageResizer.Util;
using System.Text;
using System.IO;
using System.Web.Hosting;
using System.Collections.Specialized;

namespace ComplexWebApplication {
    public partial class ListImages : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {

        }

        protected void show_Click(object sender, EventArgs e) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<table>");
            string[] queries = new string[] { col1.Text, col2.Text, col3.Text, col4.Text, col5.Text, col6.Text, col7.Text, col8.Text };
            foreach (string s in System.IO.Directory.GetFiles(Path.GetFullPath(Path.Combine(Path.Combine(HostingEnvironment.ApplicationPhysicalPath,"..\\Images"),folder.Text.Replace('/','\\'))), "*.jpg")) {
                if (System.IO.Path.GetFileName(s).StartsWith(".")) continue; //skip temp files
                string url = ResolveUrl("~/" + folder.Text.TrimEnd('/') + '/' + System.IO.Path.GetFileName(s));
                sb.AppendLine("<tr>");
                foreach(string q in queries){
                    if (q.Trim().Length == 0) continue;
                    NameValueCollection nvc = PathUtils.ParseQueryStringFriendlyAllowSemicolons(q);
                    sb.AppendLine("<td><img src=\"" + PathUtils.MergeOverwriteQueryString(url,nvc) + "\" /></td>");
                }
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
            lit.Text = sb.ToString();
        }

    }
}