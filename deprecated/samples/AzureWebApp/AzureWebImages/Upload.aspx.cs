/* Copyright (c) 2011 Wouter A. Alberts and Nathanael D. Jones. See license.txt for your rights. */
using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using ImageResizer.Util;

namespace AzureWebImages {
    public partial class Upload : System.Web.UI.Page {

        static CloudBlobClient cloudBlobClient;

        protected void Page_Load(object sender, EventArgs e) {
            // Initialize container settings
            if (!IsPostBack)
                SetContainerAndPermissions();
        }

        protected void btnSubmit_Click(object sender, EventArgs e) {
            if (Page.IsValid) {
                if (fuPicture.HasFile == true && fuPicture.FileBytes.Length > 0) {
                    string[] extensions = { ".jpg", ".jpeg", ".gif", ".bmp", ".png" };
                    bool isImage = extensions.Any(x => x.Equals(Path.GetExtension(fuPicture.FileName.ToLower()), StringComparison.OrdinalIgnoreCase));

                    if (isImage) {
                        // Store the uploaded file as Blob in the Cloud storage

                        // Get the reference of the container in which the blobs are stored
                        CloudBlobContainer cloudBlobContainer = cloudBlobClient.GetContainerReference("imageresizer");

                        // Set the name of the uploaded document to a unique name
                        string filename = Guid.NewGuid().ToString() + PathUtils.GetExtension(fuPicture.FileName.ToLower());

                        // Get the blob reference and set its metadata properties
                        CloudBlockBlob blob = cloudBlobContainer.GetBlockBlobReference(filename);
                        blob.Properties.ContentType = fuPicture.PostedFile.ContentType;
                        blob.UploadFromStream(fuPicture.FileContent);

                        // Display images; use relative paths so the module will capture the urls
                        string relativePath = string.Concat("/azure/imageresizer/", filename);
                        StringBuilder sb = new StringBuilder(2000);

                        sb.AppendFormat("<p>Image uploaded to {0}</p>", Server.HtmlEncode(relativePath));
                        sb.AppendFormat("<p>width=75<br /><img src=\"{0}?width=75\" border=\"0\"></p>", relativePath);
                        sb.AppendFormat("<p>width=150, height=150, crop=auto<br /><img src=\"{0}?width=150&height=150&crop=auto\" border=\"0\"></p>", relativePath);
                        sb.AppendFormat("<p>original image<br /><img src=\"{0}\" border=\"0\"></p>", relativePath);

                        litImages.Text = sb.ToString();
                    }
                }
            }
        }

        private void SetContainerAndPermissions() {
            try {
                // Creating the container
                var cloudStorageAccount = CloudStorageAccount.FromConfigurationSetting("BlobConn");

                cloudBlobClient = cloudStorageAccount.CreateCloudBlobClient();
                CloudBlobContainer blobContainer = cloudBlobClient.GetContainerReference("imageresizer");
                blobContainer.CreateIfNotExist();

                var containerPermissions = blobContainer.GetPermissions();
                containerPermissions.PublicAccess = BlobContainerPublicAccessType.Container;
                blobContainer.SetPermissions(containerPermissions);
            }
            catch (Exception Ex) {
                throw new Exception("Error while creating the container: " + Ex.Message);
            }
        }
    }
}