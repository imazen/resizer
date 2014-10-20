using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ImageResizer.Configuration;
using ImageResizer.Plugins.PsdComposer;
using System.Collections.Specialized;
using ImageResizer.Util;
using System.Text;

namespace PsdComposerSample {
    public partial class Default : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            var psd = Config.Current.Plugins.Get<PsdComposerPlugin>();

            var request = Request;

            // generate png image link 
            const string path = "~/text.psd";
            var query = new NameValueCollection { { "format", "png" } };
           
            var psdcmd = new PsdCommandBuilder(query);
            psdcmd.renderer = "psdplugin";

            foreach (string s in request.QueryString) {
                if (!"redraw".Equals(request.QueryString[s], StringComparison.OrdinalIgnoreCase))
                    psdcmd.SetText(s, request.QueryString[s]);
                psdcmd.Redraw(s);
                psdcmd.Show(s);

            }

            Encoding c = Encoding.Default;

            psdcmd.SaveToQuerystring(query);

            img.ImageUrl = ResolveUrl(path)  + PathUtils.BuildQueryString(query);


            if (psd != null) {
                //get all layers 
                var layers = psd.GetAllLayers(path, query);
                StringBuilder sb = new StringBuilder();
                foreach (IPsdLayer l in layers) {
                    sb.AppendLine(l.Name);
                    sb.AppendLine(l.Text);
                }

                lit.Text = "<pre>" + sb.ToString() + "</pre>";
            }
        }
    }
}