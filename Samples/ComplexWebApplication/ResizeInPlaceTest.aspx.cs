using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ImageResizer;

namespace ComplexWebApplication {
    public partial class ResizeInPlaceTest : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            ImageBuilder.Current.Build("~/watermarks/Sun_256.png", "~/watermarks/Sun_test.png", new ResizeSettings());
            ImageBuilder.Current.Build("~/watermarks/Sun_test.png", "~/watermarks/Sun_test.png", new ResizeSettings("width=20&height=20"));
        }
    }
}