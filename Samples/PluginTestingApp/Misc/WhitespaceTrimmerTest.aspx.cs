using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using System.Text;
using System.Web.Hosting;

namespace ComplexWebApplication {
    public partial class WhitespaceTrimmerTest : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            
            string dir = Path.Combine(Path.Combine(Path.GetDirectoryName(HostingEnvironment.ApplicationPhysicalPath.TrimEnd('/','\\')),  "Images"), "private");
            if (!Directory.Exists(dir)) return;
            string[] files = Directory.GetFiles(dir,"*.jpg");
            StringBuilder sb = new StringBuilder();
            foreach (string s in files) {
                sb.Append("<img src='/private/" + Path.GetFileName(s) +
                    "?width=200' /><img src='/private/" + Path.GetFileName(s) + "?width=200&trim.threshold=80&trim.percentpadding=1'/><br/>");

            }
            lit.Text = sb.ToString();
            lit.Mode = LiteralMode.PassThrough;
        }
    }
}