using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ImageResizer;
namespace ComplexWebApplication {
    public partial class CropExample : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            //Here we just do a redirect, but we could store the URL in SQL just as easily, saving the crop settings for future pages.
            if (Page.IsPostBack && !string.IsNullOrEmpty(img1.Value)) Response.Redirect(ResolveUrl(img1.Value));
            //We could even write the cropped image to disk, if we wanted
            ImageBuilder.Current.Build(ResolveUrl(ImageResizer.Util.PathUtils.RemoveQueryString(img1.Value)), "~/savedimages/cropped.jpg", new ResizeSettings(img1.Value));
        }
    }
}