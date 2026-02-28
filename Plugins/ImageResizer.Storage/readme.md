Tags: plugin
Edition: performance
Tagline: Base class for creating your own blob provider
Aliases: /plugins/blobproviderbase /plugins/blobprovider


# BlobProviderBase plugin

BlobProviderBase allows you to quickly create a file provider for ImageResizer - while only implementing 2 methods. 

```
/// <summary>
/// Should perform an immediate (uncached) query of blob metadata (such as existence and modified date information)
/// </summary>
/// <param name="virtualPath"></param>
/// <param name="queryString"></param>
/// <returns></returns>
public abstract Task<IBlobMetadata> FetchMetadataAsync(string virtualPath, NameValueCollection queryString);


public abstract Task<Stream> OpenAsync(string virtualPath, NameValueCollection queryString);
```

BlobProviderBase supports both synchronous and asynchronous interfaces, and can optionally expose your files via a VirtualPathProvider as well. It also offers metadata caching to minimize server round-trips. 


## Configuration

## Version history

### v4.3 (current)

All .NET Framework projects now target .NET 4.7.2 (previously 4.5/4.5.2). No functional changes to this plugin.

### v4.2.8 and prior

This plugin has been stable since its initial release. No breaking changes.
