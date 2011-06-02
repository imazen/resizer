using System;
using System.Collections.Generic;

using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using ImageResizer;
using System.IO;
using ImageResizer.Encoding;
using System.Data.SqlClient;
using System.Configuration;

namespace DatabaseSampleCSharp {
    public partial class Upload : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            if (!IsPostBack) return;

            Guid lastUpload = Guid.Empty;
            //Loop through each uploaded file
            foreach (string fileKey in HttpContext.Current.Request.Files.Keys) {
                HttpPostedFile file = HttpContext.Current.Request.Files[fileKey];
                if (file.ContentLength <= 0) continue; //Yes, 0-length files happen.

                //The resizing settings can specify any of 30 commands.. See http://imageresizing.net for details.
                ResizeSettings resizeCropSettings = new ResizeSettings("width=300&height=300&format=jpg&crop=auto");

                using (MemoryStream ms = new MemoryStream()) {
                    //Resize the image
                    ImageBuilder.Current.Build(file, ms, resizeCropSettings);
                    //Upload the byte array to SQL
                    lastUpload =StoreImage(ms.ToArray(), ImageBuilder.Current.EncoderProvider.GetEncoder(resizeCropSettings, file.FileName), file.FileName);
                }
            }

            if (lastUpload != Guid.Empty) Response.Redirect(ResolveUrl("~/"));
        }


        public Guid StoreImage(byte[] data, IEncoder fileInfo, string fileName) {
            SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["database"].ConnectionString);
            conn.Open();
            using (conn)
            {
                Guid id = Guid.NewGuid();
                //Select ModifiedDate, CreatedDate From Images WHERE ImageID=@id
                SqlCommand sc = new SqlCommand("INSERT INTO Images (ImageID, FileName, Extension, ContentLength, [Content]) " + 
                    "VALUES (@id, @filename, @extension, @contentlength, @content)", conn);
                sc.Parameters.Add(new SqlParameter("id", id));
                sc.Parameters.Add(new SqlParameter("filename", fileName));
                sc.Parameters.Add(new SqlParameter("extension", fileInfo.Extension));
                sc.Parameters.Add(new SqlParameter("contentlength", data.Length));
                sc.Parameters.Add(new SqlParameter("content", data));

                sc.ExecuteNonQuery();
                return id;
            }
        }
    }
}