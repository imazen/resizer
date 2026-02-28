Tags: plugin
Edition: performance
Tagline: Allows Azure Blob Storage images to be resized and served.
Aliases: /plugins/azurereader2

# AzureReader2 plugin

Allows images located in [Azure Blob Storage](https://azure.microsoft.com/en-us/documentation/services/storage/) to be read, processed, resized, and served. Requests for unmodified images can be redirected to the blobstore itself.

## Installation

1. Add `ImageResizer.Plugins.AzureReader2` to your project via NuGet: `Install-Package ImageResizer.Plugins.AzureReader2`
2. In the `<plugins />` section, insert one of the following, depending on your scenario.

```
<add name="AzureReader2" prefix="~/azure" connectionString="NamedConnectionString" />

<add name="AzureReader2" prefix="~/azure" connectionString="DefaultEndpointsProtocol=http;AccountName=myAccountName;AccountKey=myAccountKey" />

<add name="AzureReader2" prefix="~/azure" connectionString="UseDevelopmentStorage=true" />
```

The `connectionString` value can be:
- A named connection string from your `<connectionStrings>` section
- A named app setting from your `<appSettings>` section (checked first, for backwards compatibility)
- A raw Azure Storage connection string

## Configuration reference

AzureReader2 uses a prefix="~/azure/" by default.

* `connectionString` - The name of a connection string or the actual connection string
* `endpoint` - The server address to perform redirects to when we don't need to modify the blob. Ex. "http://<account>.blob.core.windows.net/" or "http://127.0.0.1:10000/account/" Automatically populated based on the connectionString.
* `redirectToBlobIfUnmodified="true"` If true, AzureReader2 will 302 redirect to the original blob. If false, it will be proxied and possibly cached.

## Migrating from v4.2.8 and prior to v4.3+

In v4.3, the underlying Azure SDK was upgraded from the legacy `WindowsAzure.Storage` (v6.0.0) package to the modern `Azure.Storage.Blobs` (v12.x) package.

### Breaking changes

- **`CloudBlobClient` property removed.** The public property `CloudBlobClient` (type `Microsoft.WindowsAzure.Storage.Blob.CloudBlobClient`) has been replaced by `BlobServiceClient` (type `Azure.Storage.Blobs.BlobServiceClient`). If your code accesses this property directly, update it.
- **`StorageException` replaced by `RequestFailedException`.** If you catch Azure storage exceptions, update your `catch` blocks from `Microsoft.WindowsAzure.Storage.StorageException` to `Azure.RequestFailedException`.
- **`Microsoft.WindowsAzure.ConfigurationManager` removed.** The plugin now uses `System.Configuration.ConfigurationManager` directly (same lookup behavior: appSettings first, then connectionStrings).
- **`Newtonsoft.Json` no longer a dependency.** The old SDK pulled this in; the new SDK does not require it. If your project depended on it transitively, add a direct reference.

### No changes required

- **Connection string format is unchanged.** The same Azure Storage connection strings work with both old and new SDKs, including `UseDevelopmentStorage=true`.
- **All XML configuration attributes are unchanged.** `connectionString`, `endpoint`, `prefix`, `redirectToBlobIfUnmodified`, etc. all work identically.
- **Local development:** Use [Azurite](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite) instead of the legacy Azure Storage Emulator. Install via `npm install -g azurite` and run with `azurite --silent`.

### v4.2.8 and prior

If you are still on v4.2.8 or earlier, the plugin uses these legacy packages:
- `WindowsAzure.Storage` 6.0.0
- `Microsoft.WindowsAzure.ConfigurationManager` 3.1.0

The Azure SDK does NOT need to be separately installed — NuGet handles the dependencies. The public API property is `CloudBlobClient` (type `Microsoft.WindowsAzure.Storage.Blob.CloudBlobClient`).
