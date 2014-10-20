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
using ImageResizer.Caching;
using ImageResizer;
using System.Data.SqlClient;

namespace DatabaseSampleCSharp
{
    public class Global : System.Web.HttpApplication
    {
        
        protected void Application_Start(object sender, EventArgs e)
        {
            //If you don't want to configure the plugin from XML, you can also do it from code
            ////Configure Sql Backend
            //SqlReaderSettings s = new SqlReaderSettings();
            //s.ConnectionString = ConfigurationManager.ConnectionStrings["database"].ConnectionString;
            //s.PathPrefix = "~/databaseimages";
            //s.StripFileExtension = true; 
            //s.ImageIdType = System.Data.SqlDbType.UniqueIdentifier;
            //s.ImageBlobQuery = "SELECT Content FROM Images WHERE ImageID=@id";
            //s.ModifiedDateQuery = "Select ModifiedDate, CreatedDate From Images WHERE ImageID=@id";
            //s.ImageExistsQuery = "Select COUNT(ImageID) From Images WHERE ImageID=@id";
            //s.CacheUnmodifiedFiles = true;
            //s.RequireImageExtension = false;

            ////Add plugin
            //new SqlReaderPlugin(s).Install(Config.Current);

            //This is example code for protecting authorization 
            Config.Current.Plugins.LoadPlugins();
            Config.Current.Plugins.Get<SqlReaderPlugin>().BeforeAccess += delegate(string id) {
                bool allowed = true;
                //INSERT HERE: execute query or whatever to check authorization to view this files
                //  SqlParameter pId = Config.Current.Plugins.Get<SqlReaderPlugin>().CreateIdParameter(id);
                // 
                if (HttpContext.Current.Request.QueryString["denyme"] != null) allowed = false;
                //END pretend code

                if (!allowed) throw new HttpException(403, "Access denied to this resource.");
            };

        }



       
    }
}