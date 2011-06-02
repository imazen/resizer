using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using ImageResizer.Encoding;
using ImageResizer;

namespace ComplexWebApplication {
    public partial class UploadSample : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            btnUpload.Click +=new EventHandler(btnUpload_Click);
        }


        void btnUpload_Click(object sender, EventArgs e) {
            //Loop through each uploaded file
            foreach (string fileKey in HttpContext.Current.Request.Files.Keys) {
                HttpPostedFile file = HttpContext.Current.Request.Files[fileKey];
                if (file.ContentLength <= 0) continue; //Yep, it happens all the time

                //Get the physical path for the uploads folder
                string uploadFolder = MapPath("~/uploads");

                //Create the upload folder if it is missing
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                //The resizing settings can specify any of 30 commands.. See http://imageresizing.net for details.
                ResizeSettings resizeCropSettings = new ResizeSettings("width=200&height=200&format=jpg&crop=auto");

                //Generate a filename (GUIDs are safest).
                string fileName = Path.Combine(uploadFolder, System.Guid.NewGuid().ToString());

                //What final type of file will we have? This can depend on whether the resizeCropSettings sepcifies a format,
                // and what the original format was (if resizecropSettings doesn't specify one)
                fileName += "." + ImageBuilder.Current.EncoderProvider.GetEncoder(resizeCropSettings, fileUpload.PostedFile.FileName).Extension;

                //Resize the image
                ImageBuilder.Current.Build(file, fileName, resizeCropSettings);
            }

            //Here's an example of getting a byte array for sending to SQL

            ////Loop through each uploaded file
            //foreach (string fileKey in HttpContext.Current.Request.Files.Keys) {
            //    HttpPostedFile file = HttpContext.Current.Request.Files[fileKey];

            //    //The resizing settings can specify any of 30 commands.. See http://imageresizing.net for details.
            //    ResizeSettings resizeCropSettings = new ResizeSettings("width=200&height=200&format=jpg&crop=auto");

            //    using (MemoryStream ms = new MemoryStream()) {
            //        //Resize the image
            //        ImageBuilder.Current.Build(file, ms, resizeCropSettings);

            //        //Upload the byte array to SQL: ms.ToArray();
            //    }
            //}
        }

      
    }
}