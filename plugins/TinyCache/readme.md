Tags: plugin
Edition: free
Tagline: 
Aliases: /plugins/tinycache


# TinyCache plugin

TinyCache is a size-limited disk cache (30MB, max 1024 cache items). It writes to a single file located at `~/App_Data/tiny_cache.cache`.

The cache is stored in RAM, but periodically flushed to disk so that it can survive application restarts. It uses ProtoBuf to serialize the cache structure to disk quickly.

Cache loading and flushing occurs *during* the processing of certain requests, not on a background thread. To prevent sporadic request failure or timeout, it is important that the cache file remain small enough that it can be flushed to disk within a few seconds.

As individual files are not written for each cache item, it does not use IIS or the StaticFileModule, and therefore can't leverage the existing etag and Last-modified support. 

The design goals for this plugin are (a) fixed-size persistent cache, and (b) simplicity. [DiskCache](/plugins/diskcache) is preferred for nearly all real-wold scenarios. TinyCache does not offer any configuration parameters.

## Installation

Either run `Install-Package ImageResizer.Plugins.TinyCache` in the NuGet package manager, or:

1. Add ImageResizer.Plugins.TinyCache.dll to your project
2. Add `<add name="TinyCache" />` inside `<resizer><plugins></plugins></resizer>` in Web.config.

