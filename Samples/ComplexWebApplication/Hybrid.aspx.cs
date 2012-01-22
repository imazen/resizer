using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ImageResizer.Util;

namespace ComplexWebApplication {
    public partial class Hybrid : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            string path = "~/" + Request.QueryString["img"];



            png.ImageUrl = PathUtils.MergeOverwriteQueryString(PathUtils.AddQueryString(path, "?format=png"), Request.QueryString);  
            jpg.ImageUrl = PathUtils.MergeOverwriteQueryString(PathUtils.AddQueryString(path, "?format=jpg"), Request.QueryString);

            hybrid.ImageUrl = PathUtils.MergeOverwriteQueryString(PathUtils.AddQueryString(path, "?format=png&hybrid=true"), Request.QueryString);
            string backgroundUrl = ResolveUrl(PathUtils.MergeOverwriteQueryString(
                PathUtils.AddQueryString(path, Utils.getBool(Request.QueryString, "full", true) ? "?format=jpg&hybrid=false" : "?format=jpg&hybrid=true"), Request.QueryString));
            hybrid.Attributes["style"] = "background-image:url(" + HttpUtility.UrlPathEncode(backgroundUrl) + ")";
        }
    }
}