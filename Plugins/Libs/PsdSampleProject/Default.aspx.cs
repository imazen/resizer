using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PsdRenderer;
using System.Collections.Specialized;
using fbs;

namespace PsdSampleProject
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            IList<IPsdLayer> layers = PsdRenderer.PsdProvider.getVisibleTextLayers("~/1001.psd",Request.QueryString);
            foreach (IPsdLayer l in layers){
                mapdata.Text += "\n<area shape=\"rect\" coords=\"" + l.Rect.X + "," + l.Rect.Y + "," + l.Rect.Right + "," + l.Rect.Bottom + "\" href=\"#\" alt=\"" + l.Name + "\" />";

            }
            yrl y = yrl.Current;
            y.BaseFile = "";
            img.Src += y.ToString();
            
        }
    }
}