# AzureReader2 Plugin for ImageResizer

This plugin allows you to map folders in your website to Azure Blob Containers and processes and optimize images therein.

Ex. https://example.com/azure/<container>/<folder>/<file>.jpg?width=100

## Configuration
    
    # AzureReader2 plugin

Allows images located in [Azure Blob Storage](https://azure.microsoft.com/en-us/documentation/services/storage/) to be read, processed, resized, and served. Requests for unmodified images can be redirected to the blobstore itself.

Ben Foster has [written a great walkthrough](http://benfoster.io/blog/high-performance-image-processing-with-image-resizer-and-azure) on using ImageResizer and AzureReader2; it provides screenshots and more detail than our reference guide here.

## Installation

1. Install-Package ImageResizer.Plugins.AzureReader2
2In the `<plugins />` section of Web.config, insert one of the following, depending on your scenario.

```
<add name="AzureReader2" prefix="~/azure" connectionString="NamedConnectionString" />

<add name="AzureReader2" prefix="~/azure" connectionString="DefaultEndpointsProtocol=http;AccountName=myAccountName;AccountKey=myAccountKey" />

<add name="AzureReader2" prefix="~/azure" connectionString="UseDevelopmentStorage=true" />
```

## Configuration reference

AzureReader2 uses a prefix="~/azure/" by default.

* `connectionString` - The name of a connection string or the actual connection string
* `endpoint` - The server address to perform redirects to when we don't need to modify the blob. Ex. "http://<account>.blob.core.windows.net/" or "http://127.0.0.1:10000/account/" Automatically populated based on the connectionString.
* `redirectToBlobIfUnmodified="true"` If true, AzureReader2 will 302 redirect to the original blob. If false, it will be proxied and possibly cached.
