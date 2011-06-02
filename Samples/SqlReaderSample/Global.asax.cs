using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.Hosting;
using System.Configuration;
using ImageResizer.Plugins.SqlReader;

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
            s.ImageIdType = System.Data.SqlDbType.Int;
            s.ImageBlobQuery = "SELECT Content FROM Images WHERE ImageID=@id";
            s.ModifiedDateQuery = "Select ModifiedDate, CreatedDate From Images WHERE ImageID=@id";
            s.ImageExistsQuery = "Select COUNT(ImageID) From Images WHERE ImageID=@id";

            //Register it as a virtual path provider. 
            HostingEnvironment.RegisterVirtualPathProvider(new SqlReaderPlugin(s));
        }

       
    }
}