using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.Hosting;
using System.Configuration;
using ImageResizer.Plugins.SqlReader;
using ImageResizer.Configuration;
using ImageResizer.Util;

namespace DatabaseSampleCSharp
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            //Configure Sql Backend
            SqlReaderSettings s = new SqlReaderSettings();
            s.ConnectionString = ConfigurationManager.ConnectionStrings["database"].ConnectionString;
            s.PathPrefix = "~/databaseimages";
            s.ImageIdType = System.Data.SqlDbType.UniqueIdentifier;
            s.ImageBlobQuery = "SELECT Content FROM Images WHERE ImageID=@id";
            s.ModifiedDateQuery = "Select ModifiedDate, CreatedDate From Images WHERE ImageID=@id";
            s.ImageExistsQuery = "Select COUNT(ImageID) From Images WHERE ImageID=@id";

            //Add plugin
            new SqlReaderPlugin(s).Install(Config.Current);

            //This is optional, but allows us to resize database images without putting ".jpg" after the ID in the path.
            Config.Current.Pipeline.PostAuthorizeRequestStart += Pipeline_PostAuthorizeRequestStart;
        }

        void Pipeline_PostAuthorizeRequestStart(IHttpModule sender, HttpContext context) {
            string path = Config.Current.Pipeline.PreRewritePath;
            if (path.StartsWith(ImageResizer.Util.PathUtils.ResolveAppRelative("~/databaseimages"))) {
                //Ok, this is a database image. The ImageResizer won't even touch it unless it has an image extension.
                
                string ext = PathUtils.GetFullExtension(path);
                if (string.IsNullOrEmpty(ext)) 
                    Config.Current.Pipeline.PreRewritePath = PathUtils.AddExtension(path,"jpg"); //We don't care which extension, it just has to be an image.

            }
        }

       
    }
}