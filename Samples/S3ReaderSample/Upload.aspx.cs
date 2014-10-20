using Amazon.S3;
using ImageResizer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace S3ReaderSample
{
    public partial class Upload : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (upload.PostedFile != null && upload.PostedFile.ContentLength > 0 && !string.IsNullOrEmpty(Request["awsid"]) && !string.IsNullOrEmpty(Request["awssecret"]) && !string.IsNullOrEmpty(this.bucket.Text))
            {
                var name = "s3readersample/" + ImageUploadHelper.Current.GenerateSafeImageName(upload.PostedFile.InputStream, upload.PostedFile.FileName);

                var client = new Amazon.S3.AmazonS3Client(Request["awsid"], Request["awssecret"], Amazon.RegionEndpoint.EUWest1);
                
                //For some reason we have to buffer the file in memory to prevent issues... Need to research further
                var ms = new MemoryStream();
                upload.PostedFile.InputStream.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);

                var request = new Amazon.S3.Model.PutObjectRequest() {  BucketName = this.bucket.Text, Key = name, InputStream = ms, CannedACL = Amazon.S3.S3CannedACL.PublicRead };
               
                
                var response = client.PutObject(request);
                if (response.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    result.Text = "Successfully uploaded " + name + "to bucket " + this.bucket.Text;

                }
                else
                {
                    result.Text = response.HttpStatusCode.ToString();
                }
            }
        }
    }
}