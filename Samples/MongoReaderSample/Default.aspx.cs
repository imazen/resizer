using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ImageResizer.Configuration;
using ImageResizer.Plugins.MongoReader;
using MongoDB.Driver.GridFS;
using System.IO;
using ImageResizer;

namespace MongoReaderSample {
    public partial class Upload : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {


            MongoGridFS g = Config.Current.Plugins.Get<MongoReaderPlugin>().GridFS;


            //Loop through each uploaded file
            foreach (string fileKey in HttpContext.Current.Request.Files.Keys) {
                HttpPostedFile file = HttpContext.Current.Request.Files[fileKey];
                if (file.ContentLength <= 0) continue; //Skip unused file controls.

                //Resize to a memory stream, max 2000x2000 jpeg
                MemoryStream temp = new MemoryStream(4096);
                new ImageJob(file.InputStream,temp,new ResizeSettings("width=2000;height=2000;mode=max;format=jpg")).Build();
                //Reset the stream
                temp.Seek(0, SeekOrigin.Begin);

                MongoGridFSCreateOptions opts = new MongoGridFSCreateOptions();
                opts.ContentType = file.ContentType;

                MongoGridFSFileInfo fi = g.Upload(temp, Path.GetFileName(file.FileName), opts);

                lit.Text += "<img src=\"" + ResolveUrl("~/gridfs/id/") + fi.Id + ".jpg?width=100&amp;height=100\" />";
            }
            
        }
    }
}