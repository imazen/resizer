Tags: plugin
Edition: performance
Bundle: 3
Tagline: Allows blobstore images to be resized and served. (Azure 1.X compatible).
Aliases: /plugins/azurereader

# AzureReader plugin

[See AzureReader2 if you're using the Azure SDK 2.0](/plugins/azurereader2).

Allows images located in an Azure Blobstore to be read, processed, resized, and served. Requests for unmodified images get redirected to the blobstore itself.

This plugin was developed by Wouter Alberts with a bit of help and consultation from me. He's graciously donated it to the project, and you can find it in the /Contrib folder along with some sample projects.

.NET 3.5 or higher and the Azure SDK are required.

Please share any suggestions, corrections, or bugs with us at support@imageresizing.net. 

## Installation

1. Install the Azure SDK
2. Add ImageResizer.Plugins.AzureReader.dll to the project or /bin.
3. In the `<plugins />` section, insert `<add name="AzureReader" connectionString="ConnectionKeyName" endpoint="http://<account>.blob.core.windows.net/" />`



## Configuration reference

* connectionString - the name of an Azure connection string.
* endpoint - The server address to perform redirects to when we don't need to modify the blob. Ex. "http://<account>.blob.core.windows.net/" or "http://127.0.0.1:10000/account/"
* vpp - True(default): Installs the plugin as a VirtualPathProvider, so any ASP.NET software can access/execute the file. False only permits the ImageResizer to access the file.
* lazyExistenceChceck: False(default) Verifies the blob exists before trying to access it (slower). True assumes that it exists, failing later on if the file is missing.
* prefix - The subfolder of the site that is used to access azure files. Default: "~/azure/"


## Connection strings

As I don't personally use Azure, I don't claim to understand why Azure makes the following code needed just to lookup connection strings.

### In Global.asax.cs, Application_Start:

    CloudStorageAccount.SetConfigurationSettingPublisher((configName, configSettingPublisher) => {
        var connectionString = RoleEnvironment.GetConfigurationSettingValue(configName);
        configSettingPublisher(connectionString);
    });

### In Webrole.cs

    public class WebRole : RoleEntryPoint {

        public override bool OnStart() {
            // For information on handling configuration changes
            // see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

            RoleEnvironment.Changing += RoleEnvironmentChanging;

            Microsoft.WindowsAzure.CloudStorageAccount.SetConfigurationSettingPublisher((configName, configSetter) => {
                configSetter(Microsoft.WindowsAzure.ServiceRuntime.RoleEnvironment.GetConfigurationSettingValue(configName));
            });

            return base.OnStart();
        }

        private void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e) {
            // If a configuration setting is changing
            if (e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange))
                // set e.Cancel to true to restart this role instance
                e.Cancel = true;
        }
    }


## Example upload script

This isn't any different from a normal upload to Azure. 

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
                    StringBuilder sb = new StringBuilder(2000);
                    sb.Append("<img src=\"azure/imageresizer/" + filename + "?width=75\" border=\"0\"><br /><br />");
                    sb.Append("<img src=\"/azure/imageresizer/" + filename + "?width=150&height=150&crop=auto\" border=\"0\"><br /><br />");
                    sb.Append("<img src=\"/azure/imageresizer/" + filename + "\" border=\"0\">");

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

