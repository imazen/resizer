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
using ImageResizer.Configuration;
using ImageResizer.Util;
using System.Text;

namespace DatabaseSampleCSharp {
    public partial class Upload : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            if (!IsPostBack) return;

            object lastUpload = null;
            //Loop through each uploaded file
            foreach (string fileKey in HttpContext.Current.Request.Files.Keys) {
                HttpPostedFile file = HttpContext.Current.Request.Files[fileKey];
                if (file.ContentLength <= 0) continue; //Yes, 0-length files happen.

                if (Config.Current.Pipeline.IsAcceptedImageType(file.FileName)) {
                    //The resizing settings can specify any of 30 commands.. See http://imageresizing.net for details.
                    ResizeSettings resizeCropSettings = new ResizeSettings("width=300&height=300&format=jpg&crop=auto");

                    using (MemoryStream ms = new MemoryStream()) {
                        //Resize the image
                        ImageBuilder.Current.Build(file, ms, resizeCropSettings);
                        //Upload the byte array to SQL
                        lastUpload = StoreFile(ms.ToArray(), ImageBuilder.Current.EncoderProvider.GetEncoder(resizeCropSettings, file.FileName).Extension, file.FileName);
                    }
                } else {
                    using (MemoryStream ms = ImageResizer.Util.StreamUtils.CopyStream(file.InputStream)){
                        //It's not an image - upload as-is.
                        lastUpload = StoreFile(ms.ToArray(), PathUtils.GetExtension(file.FileName).TrimStart('.'), file.FileName);
                    }
                }
            }

            if (lastUpload != null) Response.Redirect(ResolveUrl("~/"));
        }

        protected string Base16Encode(byte[] bytes) {
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                sb.Append(b.ToString("x").PadLeft(2, '0'));
            return sb.ToString();
        }

        public object StoreFile(byte[] data,string extension, string fileName) {
            SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["database"].ConnectionString);
            conn.Open();
            using (conn)
            {
                string id = Base16Encode(Guid.NewGuid().ToByteArray());
                //Select ModifiedDate, CreatedDate From Images WHERE ImageID=@id
                SqlCommand sc = new SqlCommand("INSERT INTO Documents (DocKey, DocFnm, DocDat, DocObj) " + 
                    "VALUES (@id, @filename, @date, @content)", conn);
                sc.Parameters.Add(new SqlParameter("id", id));
                sc.Parameters.Add(new SqlParameter("filename", fileName));
                sc.Parameters.Add(new SqlParameter("date", DateTime.UtcNow));
                sc.Parameters.Add(new SqlParameter("content", data));

                sc.ExecuteNonQuery();
                return id;
            }
        }
    }
}