# ImageResizer.Plugins.HybridCache

This plugin is suggested for all ImageResizer installations. It caches image processing requests as static files and is essential for consistent low-latency responses and scalability.


HybridCache offers accurately bounded disk usage,
capable of storing mime-type metadata when needed, and utilizing a sharded write-ahead-log for scalability. Cache entries are written as separate files to improve OS level performance.

This plugin only works with the URL API, not the managed API.

## Installation

1. ` PM> Install-Package ImageResizer.Plugins.HybridCache `
2. In the `<resizer><plugins>` section of `Web.config`, insert `<add name="HybridCache" />`.
3. In the `<resizer>` section of Web.config, insert <br />
   `<hybridCache cacheLocation="C:\imageresizercache\" cacheMaxSizeBytes="1,000,000,000" />`.

## Notes

* `<hybridCache cacheLocation="C:\imageresizercache\"/>` defaults to a app-unique subfolder of the IIS user account's temp folder.
* `<hybridCache cacheMaxSizeBytes=""1,000,000,000" />` is in bytes and cannot be set below 9MB (9,000,000) or no files will be cached. 1GiB is the suggested minimum.
* `<hybridCache databaseShards="8" />` adjust the number of shards (and write ahead log groups) in the database. Delete the cache folder after changing this number. Don't change this number unless directed by support.
* `<hybridCache queueSizeLimitInBytes="100,000,000" />` limits how much RAM can be used by the asynchronous write queue before making requests wait for caching writing to complete. (HybridCache writes cache entries in the background to improve latency). 100MB is the default and suggested minimum.
* `<hybridCache.minCleanupBytes="1,000,000" />` determines the minimum amount of bytes to evict from the cache once a cleanup is triggered. 1MB is the default and suggested minimum.
