using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PsdRenderer;

namespace PsdSampleProject
{
    public partial class PerfTest : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < 50; i++)
            {
                Trace.Warn("Loop " + i);
                PsdProvider.getStream("~/1001.psd",HttpContext.Current.Request.QueryString);
            }
        }
    }
}