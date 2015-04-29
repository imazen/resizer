Tags: plugin
Edition: performance
Tagline: Allows blobstore images to be resized and served. (Azure 2.0 compatible).
Aliases: /plugins/azurereader2


# AzureReader2 plugin

**AzureReader2 supports the Azure SDK V2.0 and requires .NET 4.0 instead of .NET 3.5. Many thanks to Marcus Briggs for his invaluable help in getting this released quickly.**

Also, Chris Skardon has [posted a nice walkthrough on using AzureReader2](http://geekswithblogs.net/cskardon/archive/2013/02/26/imageresizer-azurereader2-and-wellhellip-azure-duh.aspx), and Ben Foster has [written a great article](http://benfoster.io/blog/high-performance-image-processing-with-image-resizer-and-azure) on getting the most out of ImageResizer and Azure together.

Allows images located in an Azure Blobstore to be read, processed, resized, and served. Requests for unmodified images get redirected to the blobstore itself.

## Installation

1. Install the Azure SDK
2. Add ImageResizer.Plugins.AzureReader2.dll to the project or /bin.
3. In the `<plugins />` section, insert `<add name="AzureReader2" connectionString="DefaultEndpointsProtocol=http;AccountName=myAccountName;AccountKey=myAccountKey" endpoint="http://<account>.blob.core.windows.net/" />`



## Configuration reference

* connectionString - The actual connection string
* endpoint - The server address to perform redirects to when we don't need to modify the blob. Ex. "http://<account>.blob.core.windows.net/" or "http://127.0.0.1:10000/account/"
* vpp - True(default): Installs the plugin as a VirtualPathProvider, so any ASP.NET software can access/execute the file. False only permits the ImageResizer to access the file.
* lazyExistenceChceck: False(default) Verifies the blob exists before trying to access it (slower). True assumes that it exists, failing later on if the file is missing.
* prefix - The subfolder of the site that is used to access azure files. Default: "~/azure/"



