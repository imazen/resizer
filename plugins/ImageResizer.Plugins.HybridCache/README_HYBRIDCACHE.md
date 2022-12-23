# ImageResizer.Plugins.HybridCache

This plugin is suggested for all ImageResizer installations. It caches image processing requests as static files and is essential for consistent low-latency responses and scalability.


HybridCache is our 7th-generation disk caching engine based on 13 years of large-scale production deployments.

HybridCache offers accurately bounded disk usage,
can store mime-type metadata when needed, and uses a sharded, asynchronous write-ahead-log for scalability. Cache entries are written as separate files to improve OS level performance.

This plugin only works with the URL API, not the managed API.

## Installation

1. ` PM> Install-Package ImageResizer.Plugins.HybridCache `
2. In the `<resizer><plugins>` section of `Web.config`, insert `<add name="HybridCache" />`.
3. (optional) In the `<resizer>` section of Web.config, insert <br />
   `<hybridCache cacheLocation="C:\imageresizercache\" cacheSizeMb="1,000" />`.



## Notes

* `<hybridCache cacheLocation="C:\imageresizercache\"/>` defaults to a app-unique subfolder of the IIS user account's temp folder. Cannot be located in the project or a web-accessible folder.
* `<hybridCache cacheSizeMb="1,000" />` is in MiB and cannot be set below 9 or no files will be cached. 1GiB is the suggested minimum.
* `<hybridCache databaseShards="8" />` adjust the number of shards (and write ahead log groups) in the database. Delete the cache folder after changing this number. Don't change this number unless directed by support.
* `<hybridCache writeQueueMemoryMb="100" />` limits how much RAM can be used by the asynchronous write queue before making requests wait for caching writing to complete. (HybridCache writes cache entries in the background to improve latency). 100MB is the default and suggested minimum.
* `<hybridCache.evictionSweepSizeMb="1" />` determines the minimum amount of bytes to evict from the cache once a cleanup is triggered. 1MB is the default and suggested minimum. 

## Migrating from DiskCache or TinyCache

* **Delete your `/imagecache/` folder (otherwise it will become publicly accessible!!!)** (Actually, if installed, HybridCache will kill the application with an error message to prevent that - for all we know you resize images of passwords and have directory listing enabled)
* Delete references to `DiskCache` and `TinyCache` from **both nuget.config and Web.config**
* `Install-Package ImageResizer.Plugins.HybridCache`
* Put `<add name="HybridCache" />` in the `<resizer><plugins>` section of `Web.config`
* Put `<hybridCache cacheLocation="C:\imageresizercache\" cacheSizeMb="1,000" />` in the `<resizer>` section of `Web.config`. If you want to use a temp folder, omit cacheLocation.
* HybridCache requires a cache folder outside of the web root. DiskCache did not support that.
* HybridCache, unlike DiskCache, can precisely limit the cache size & disk utilization.
* HybridCache uses a write-ahead log to prevent orphaned cache entries.
* HybridCache can store the associated mime-type for cached files.


### The type 'System.Object' is defined in an assembly that is not referenced. You must add a reference to assembly 'netstandard, Version=2.0.0.0,

If you get this error, add `<add assembly="netstandard, Version=2.0.0.0, Culture=neutral,
PublicKeyToken=cc7b13ffcd2ddd51"/>` to `<system.web><compilation><assemblies>` in web.config.

Example:
```
<system.web>
<compilation debug="true" targetFramework="4.8" >
    <assemblies>
        <add assembly="netstandard, Version=2.0.0.0, Culture=neutral,
        PublicKeyToken=cc7b13ffcd2ddd51"/>
    </assemblies>
</compilation>
```