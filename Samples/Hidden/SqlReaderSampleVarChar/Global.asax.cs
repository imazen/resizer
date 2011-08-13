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
            s.StripFileExtension = true; 
            s.ImageIdType = System.Data.SqlDbType.Char;
            s.ImageBlobQuery = "SELECT DocObj FROM Documents WHERE DocKey=@id";
            s.ModifiedDateQuery = "Select DocDat From Documents WHERE DocKey=@id";
            s.ImageExistsQuery = "Select COUNT(DocKey) From Documents WHERE DocKey=@id";

            //Add plugin
            new SqlReaderPlugin(s).Install(Config.Current);

            //This is optional, but allows us to resize database images without putting ".jpg" after the ID in the path.
            Config.Current.Pipeline.PostAuthorizeRequestStart += delegate(IHttpModule sender2, HttpContext context) {
                string path = Config.Current.Pipeline.PreRewritePath;
                //Only work with database images
                if (!path.StartsWith(s.VirtualPathPrefix, StringComparison.OrdinalIgnoreCase)) return;

                Config.Current.Pipeline.SkipFileTypeCheck = true; //Skip the file extension check. FakeExtensions will still be stripped.
                //Non-images will be served as-is
                //Cache all file types, whether they are processed or not.
                Config.Current.Pipeline.ModifiedQueryString["cache"] = ServerCacheMode.Always.ToString();
            };

            Config.Current.Pipeline.Rewrite += delegate(IHttpModule sender2, HttpContext context, IUrlEventArgs ea) {
                //This is where we have to modify ea.VirtualPath to remove the 'filename' that's not part of the ID.

                //Without a file extension, though, the image resizer doesn't know if the file is an image that can be resized or not.
                //It also doesn't know the original file type, so it will revert to jpeg format if any processing occurs 
                //(although, without setting a file extension, we'd have to set  ea.QueryString["process"] = "Always" for that to occur).

                //So, we need to leave ea.VirtualPath in the form [id].[originalextension]
                if (!ea.VirtualPath.StartsWith(s.VirtualPathPrefix, StringComparison.OrdinalIgnoreCase)) return; //Only handle database requests
                int extension = ea.VirtualPath.LastIndexOf('.');
                if (extension < 0) return; //We gotta have a filename.
                int lastSlash = ea.VirtualPath.LastIndexOf('/');
                if (lastSlash < 0) return; //We gotta have a slash.
                //Remove everything between the last slash and the last period.
                ea.VirtualPath = ea.VirtualPath.Substring(0, lastSlash) + ea.VirtualPath.Substring(extension);



            };
   
        }


       
    }
}