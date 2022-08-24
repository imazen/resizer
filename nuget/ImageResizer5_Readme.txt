## ImageResizer v5 Upgrade Notes

You must remove references to any ImageResizer.Plugins packages that are not 5.x or higher, 4.x and 5.x cannot be mixed.

ImageResizer 5 consolidates 15 plugins into 1 - ImageResizer.Plugins.Imageflow

ImageResizer.Plugins.HybridCache replaces DiskCache. You must move the cache directory outside the web root and any accessible website folders. 

ImageResizer 5 also introduces some breaking changes - See BREAKING.txt
