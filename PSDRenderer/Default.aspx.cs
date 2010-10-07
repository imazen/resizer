using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using PsdRenderer;

namespace PSDRenderer
{
    public partial class _Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            IList<ITextLayer> layers = PsdRenderer.PsdProvider.getTextLayers("~/1001.psd");
            foreach (ITextLayer l in layers){
                mapdata.Text += "\n<area shape=\"rect\" coords=\"" + l.Rect.X + "," + l.Rect.Y + "," + l.Rect.Right + "," + l.Rect.Bottom + "\" href=\"#\" alt=\"" + l.Name + "\" />";

            }
        }
    }
}