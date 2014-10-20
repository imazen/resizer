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
                if (file.ContentLength <= 0) continue; //Skip unused file controls.

                //The resizing settings can specify any of 30 commands.. See http://imageresizing.net for details.
                //Destination paths can have variables like <guid> and <ext>
                ImageJob i = new ImageJob(file, "~/uploads/<guid>_<filename:A-Za-z0-9>.<ext>", new ResizeSettings("width=200&height=200&format=jpg&crop=auto"));
                i.CreateParentDirectory = true; //Auto-create the uploads directory.
                i.Build();
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

        protected void btnUploadAndGenerate_Click(object sender, EventArgs e) {
            Dictionary<string, string> versions = new Dictionary<string, string>();
            //Define the version to generate
            versions.Add("_thumb", "width=100&height=100&crop=auto&format=jpg"); //Crop to square thumbnail
            versions.Add("_medium", "maxwidth=400&maxheight=400format=jpg"); //Fit inside 400x400 area, jpeg
            versions.Add("_large", "maxwidth=1900&maxheight=1900&format=jpg"); //Fit inside 1900x1200 area

            //Loop through each uploaded file
            foreach (string fileKey in HttpContext.Current.Request.Files.Keys) {
                HttpPostedFile file = HttpContext.Current.Request.Files[fileKey];
                if (file.ContentLength <= 0) continue; //Skip unused file controls.

                //Generate each version
                foreach (string suffix in versions.Keys) {

                    ImageJob i = new ImageJob(file, "~/uploads/<guid>" + suffix + ".<ext>", new ResizeSettings(versions[suffix]));
                    i.CreateParentDirectory = true; //Auto-create the uploads directory.
                    i.Build();
                }

            }
        }

        public IList<string> GenerateVersions(string original) {
            Dictionary<string, string> versions = new Dictionary<string, string>();
            //Define the versions to generate and their filename suffixes.
            versions.Add("_thumb", "width=100&height=100&crop=auto&format=jpg"); //Crop to square thumbnail
            versions.Add("_medium", "maxwidth=400&maxheight=400format=jpg"); //Fit inside 400x400 area, jpeg
            versions.Add("_large", "maxwidth=1900&maxheight=1900&format=jpg"); //Fit inside 1900x1200 area

            //To store the list of generated paths
            List<string> generatedFiles = new List<string>();

            //Generate each version
            foreach (string suffix in versions.Keys)
                //Let the image builder add the correct extension based on the output file type
                generatedFiles.Add(new ImageJob(original, "<path>" + suffix + ".<ext>", new ResizeSettings(versions[suffix])).Build().FinalPath);

            return generatedFiles;   
        }

      
    }
}