using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace DiskCacheWebTest {
    public partial class _Default : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            DebugInfo.Text = Server.HtmlEncode(ImageResizer.Configuration.Config.Current.GetDiagnosticsPage());
            
        }
    }
}
