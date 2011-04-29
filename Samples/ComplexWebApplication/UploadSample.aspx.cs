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

        ResizeSettings resizeCropSettings = new ResizeSettings("width=200&height=200&format=jpg&crop=auto");

        void  btnUpload_Click(object sender, EventArgs e)
        {
            //Quit if no file was uploaded
            if (!fileUpload.HasFile) return; 

            //Get the physical path for the uploads folder
 	        string uploadFolder = MapPath("~/uploads"); 

            //Create the upload folder if it is missing
            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);


            //Generate a filename (GUIDs are best).
            string fileName = Path.Combine(uploadFolder, System.Guid.NewGuid().ToString());
            
            //What final type of file will we have? This can depend on whether the resizeCropSettings sepcifies a format,
            // and what the original format was (if resizecropSettings doesn't specify one)
            string extension = ImageBuilder.Current.EncoderProvider.GetEncoder(resizeCropSettings,fileUpload.PostedFile.FileName).Extension;
            fileName += "." + extension;

            //Resize the image
            ImageBuilder.Current.Build(fileUpload.PostedFile,fileName,resizeCropSettings);
        }
    }
}