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

namespace ImageStudio {
    public partial class _Default : System.Web.UI.Page {

        protected ICollection<string> GetVirtualFilenames(string physicalBasePath, string basePath, string extensions, params string[] subdirs) {
            List<string> paths = new List<string>();
            physicalBasePath = Path.Combine(HostingEnvironment.ApplicationPhysicalPath,physicalBasePath);
            string[] exts = extensions.Split(new char[]{'|',';',','}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string dir in subdirs) {
                var sdir = dir.Trim('/','\\');
                string full = Path.GetFullPath(Path.Combine(physicalBasePath, sdir.Replace('/', '\\')));
                if (System.IO.Directory.Exists(full)) {
                    foreach(string e in exts){
                        string[] physicals = Directory.GetFiles(full,e, SearchOption.TopDirectoryOnly);
                        foreach(string physicalFile in physicals){
                            string name = System.IO.Path.GetFileName(physicalFile);
                            if (name.StartsWith(".")) continue;
                            string url = ResolveUrl(basePath.TrimEnd('/') + '/' + sdir + '/' + System.IO.Path.GetFileName(name));
                            paths.Add(url);
                        }
                    }
                }
            }
            return paths;
        }
        protected void Page_Load(object sender, EventArgs e) {
            StringBuilder sb = new StringBuilder();
            var files = GetVirtualFilenames("..\\Images","~/","*.jpg|*.png|*.gif|*.tiff|*.tif","private/picks","","private");

            foreach (string s in files) {
                NameValueCollection nvc = PathUtils.ParseQueryStringFriendlyAllowSemicolons("width=100;height=100;margin=5;mode=pad;scale=canvas;bgcolor=454545");
                var url = PathUtils.MergeOverwriteQueryString(s, nvc);
                sb.AppendLine("<img src='" + url + "' alt='' />");
            }

            this.images.Text = sb.ToString();

        }
    }
}