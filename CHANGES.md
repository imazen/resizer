#v4-0-0

###.NET 4.5 is now required by the core and all plugins. What does this mean?

- We no longer support Windows XP or Windows Server 2003. You'll also want Visual Studio 2012 or newer.
- You no longer need to install MvcRoutingShim and ImageResizer.Mvc.dll. ImageResizer is Routing compatible.
- Extension methods are back! All our utility functions are now accessible in the ImageResizer.ExtensionMethods namespace.
- We can access APIs for better filesystem consistency.
- We can go async!

### We offer an asynchronous pipeline

Replace  `InterceptModule` with `AsyncInterceptModule` in Web.Config, and you'll be using our asynchronous pipeline.

Keep in mind that the async pipeline can only access data from plugins which implement IVirtualFileAsync (or inherit from BlobProviderBase). The standard pipeline can access files exposed by any VirtualPathProvider.

### Virtual file (blob provider) plugins are now much easier to create.

- ImageResizer.Storage provides a standard, unified `BlobProviderBase` class with 90% of the functionality you need. Override `FetchMetadataAsync` and `OpenAsync`, and you'll have a full-fledged provider with async/sync pipeline support, metadata caching, and even (optional) exposure through the ASP.NET VirtualPathProvider system.

- No more VirtualPathProvider. At startup, we install a single VirtualPathProviderShim. Any plugin can implement IVirtualImageProviderVpp and return true from `bool VppExposeFile(string virtualPath)`. It will then be wrapped in a VirtualFile instance and shared to all systems which access the VirtualPathProvider system. 

New interfaces include: IVirtualImageProviderAsync, IVirtualFileAsync, IVirtualImageProviderVpp, IVirtualImageProviderVppCaching, IVirtualFileWithModifiedDateAsync, IVirtualFileCacheAsync

### Plugins referenced in Web.config are loaded differently

In V3, we used a convention-based approach to generate dozens of possible fully-qualified type names, and then tried those against each loaded assembly. This approach was slow, and broke with .NET 4, as it only loads assemblies with explicit references. We now maintain a hints file (which includes the assembly name), and use that to ensure a faster and more reliable load-time experience.  

When installed via `Web.config`, custom plugins will need to be referenced by their fully-qualified name, such as "MyRootNamespace.MyPluginNamespace.MyPluginClass, MyAssembly"

### Our blob providers have been rewritten. 

They are now consistent in interface, functionality, and configuration, as they inherit from BlobProviderBase. This means you'll have to update some of your configuration.

### We've dropped some deprecated (and redundant) classes

These include: ImageResizer.Mvc, AzureReader, S3Reader, Controls, StreamUtils, UrlHasher, WpfBuilder.

# Smaller improvements

Add ImageJob.FinalWidth and ImageJob.FinalHeight (all pipelines). Closes #10. Needs unit tests.
NuGet restore, .NET 4.5 compatibility

Restore ExtensionMethods namespace... to actually be Extension Methods again (we had to roll this back due to bugs in .NET 2.0 extension method support (it was hacky/broken)

Expose LoadNativeDependenciesForType(Type t) from Config.Plugins. Previously there was no code-based method to load a native dependency manually.

DiskCache: Attempt semi-transactional writes. Write to temporary file, then rename.

Update Webp to 4.0

Migrate to xUnit from MbUnit, improve code coverage.

Add Pipeline.AuthorizeAllImages configuration. When true, URL authorization and the AuthorizeImage event will be executed for all image requests, not just those processed or cached by ImageResizer.

Core: Add BeforeEncode and EndBuildJob to AbstractImageProcessor and ImageBuilder to enable easier benchmarking.

Core: Add IProfiler interface and ImageJob.Profiler property.

Core: Add PluginConfig.AddPluginByName(string name, NameValueCollection pluginConfig = null) to enable code-driven installation of arbitrary plugins.

Add ImageResizer.Util.AsyncUtils class and CopyToMemoryStreamAsync extension method.

## Bug fixes:

Catch DirectoryNotFoundException as well as FileNotFoundException -> 404

Fix Core Bug: EndProcess is never called for BuilderExtension plugins.

Core: NoCache will buffer data to memorystream before writing to output stream. InvalidOperationException when accessing stream.Position within Bitmap.save - despite buffer = true. )(<- todo, this buffering should be conditional)

Fixes #44. Switch from FileStream.Flush() to FileStream.Flush(true) to ensure files are written to disk before the method completes.

Fixes #74. Delete unfinished image output file in case of an exception. Doesn't handle situation where source and dest path are the same; file will be deleted.

Fixes #77; Ensure diagnostics page redaction code can handle a null node.

Fixes issue #54, InvalidOperationException in TinyCache when there are 0 recent reads.  Supersedes pull request #105. Regression tests added.

PathUtils.AddQueryString("/path?","key=value") should now produce "/path?key=value" instead of "/path?&key=value"

Don't nullref when passed a ~/aspnet path outside of ASP.NET. Drop the tilde and pass to the virtual path providers.

Prevent nullref when VirtualFolder is used outside of an ASP.NET application.

Fixed: WIC builder no longer throws a NullReferenceException when provided a null stream. (This was preventing the correct error message)

Fix incorrect status code when any IVirtualFileCache is installed and an 404 occurs. Source(Mem/Disk)Cache call .Open and .Read during the GetFile() stage (callers expect only a null result on not found failure). PipelineConfig.GetFile must absorb FileNotFound exceptions to prevent the behavior contract from being violated.

#Changes in plugins

DiskCache no longer supports HashModifiedDate (it's always true). 

## Breaking changes for custom plugins:

[breaking plugin api change]: Replace AbstractImageProcessor.buildToStream and AbstractImageProcessor.buildToBitmap with AbstractImageProcessor.BuildJobBitmapToStream and ImageBuilder.BuildJobBitmapToBitmap.

InterceptModule now combines the date into the RequestKey, so no more conditional logic in plugins. ResponseArgs.HasModifiedDate and ResponseArgs.GetModifiedDateUtc are now obsolete.

Deprecated functionality now removed:

Drop deprecated ImageResizer.Mvc AzureReader, S3Reader and Controls plugins.

Drop long-time deprecated StreamUtils class.

Drop long-time deprecated UrlHasher class.

Delete stub WPF plugin. WPF support is a sub-par goal considering it's poor image quality and other options available.
Drop misspelled, obsolete, duplicate CropRectange property from Instructions class.

Catch DirectoryNotFoundException as well as FileNotFoundException -> 404

#v3-4-3
Date: May 8 2014

FullFile: http://downloads.imageresizing.net/Resizer3-4-3-full-May-8-2014.zip

This release introduces [the CopyMetadata plugin](/plugins/copymetadata), and the [DiagnosticJson plugin](/plugins/diagnosticjson).

## New plugins

* The [CopyMetadata plugin](/plugins/copymetadata), when installed, allows you to copy all metadata from the source image to the destination image with `&copymetadata=true`.
* The [DiagnosticJson plugin](/plugins/diagnosticjson) allows for remote debugging of layout and rendering issues by providing the execution plan in json form. Extremely useful for unit testing.

## Fixed bugs

* AzureReader2 now works with 404Plugin. Fixes [bug #28](https://github.com/imazen/resizer/issues/28).
* `scale=down` is now always respected, even when `mode=crop` is used. Fixes [bug #12](https://github.com/imazen/resizer/issues/12).
* File streams returned by the RemoteReader plugin are now always seekable. Fixes [bug #26](https://github.com/imazen/resizer/issues/26).
* For performance, the Watermark plugin will prefetch overlays prior to the actual render stage. This will prevent I/O blocking during rendering phases where high RAM consumption is required. Fixes [bug #69](https://github.com/imazen/resizer/issues/69).

## New features in existing plugins

* Add named connection string support to AzureReader2. Fixes [bug #40](https://github.com/imazen/resizer/issues/40)
* Watermark plugin now offers a 'defaultImageQuery' Web.config setting that can be used to simplify common overlay queries like `scache=true`. Fixes [#21](https://github.com/imazen/resizer/issues/21).

## API warnings

* protected methods ImageBuilder.buildToStream and ImageBuilder.buildToBitmap methods are obsolete and will be removed in V4. There are no known usages of these methods outside the core.

## No known remaining bugs

#v3-4-2
Date: Nov 26 2013

FullFile: http://downloads.imageresizing.net/Resizer3-4-2-full-Nov-26-2013.zip

This release introduces [the new S3Reader2 plugin](/plugins/s3reader2), which includes support for the rewritten AWSSDK 2.0.

## New plugins

* [The S3Reader2 plugin](/plugins/s3reader2) introduces support for the rewritten AWSSDK 2.0. You must explicitly set the S3 bucket region if you're not using the  Virginia or Northern Californa datacenters.

## Fixed bugs

* AzureReader and AzureReader2 now use correct NuGet dependency version ranges
* S3Reader and S3Reader2 now use correct NuGet dependency version ranges

## Known remaining bugs

* AzureReader2 and 404plugin are incompatible
* When using auto-cropping (mode=crop), scale=down may sometimes be ignored, acting as if scale=both was set.


#v3-4-1
Date: Oct 30 2013

FullFile: http://downloads.imageresizing.net/Resizer3-4-1-full-Oct-31-2013.zip

This release fixes a small but troublesome bug in ImageJob.

## Bug fixes in core

* Fix NullReferenceException (for RequestedInfo or ResultInfo) when the ImageJob was created without any constructor parameters. Fixes bug #54.

## Enhancements to RemoteReader

* Requests in base64 encoded form (which lack a signature) can be now validated via whitelisting.


## Known remaining bugs

* AzureReader2 and 404plugin are incompatible
* When using auto-cropping (mode=crop), scale=down may sometimes be ignored, acting as if scale=both was set.

#v3-4-0
Date: Oct 17 2013

FullFile: http://downloads.imageresizing.net/Resizer3-4-0-full-Oct-17-2013.zip

## New plugins

* [SourceDiskCache](/plugins/diskcache) enables source file disk caching. 
* [MemCache and OutputMemCache](/plugins/diskcache) enable fixed-size source and output mem caching
* [FFMpegPlugin](/plugins/ffmpeg) enables video thumbnailing

## New guidance

* Use the `Instructions` class instead of `ResizeSettings` whenever possible. `ResizeSettings` will eventually be removed.
* If you use any plugins with native dependencies, call `ImageResizer.Configuration.Config.Current.Plugins.LoadPlugins()` during Application_Start. 
* Don't work directly with Bitmap instances. Methods that return a Bitmap instance are now marked obsolete due to the high probability of memory leaks and encoding bugs in user code.
* Unit tests now require .NET 4.5 to run. The core library and most plugins can still be compilied under Visual Studio 2010 or higher. Run `PM> Update-Package -Reinstall` if your nuget packages aren't automatically restored.
* Does your plugin need to pass additional information back to the ImageJob? Use ImageJob.ResultInfo.
* If you have more than 2 cores, consider using an IIS Web Garden or our [WIC pipeline](/plugins/wic) to get optimal throughput.

## Bug fixes in core

* Fixed: Combining manual cropping and sourcerotation (`crop=0,10,30,30&srotate=90`) causes distorion. Bug introduced in 3.3.2 with CMYK fix. (#51)
* Fixed: DefaultSettings plugin doesn't work with w/h abbreviation (#45) 
* Fixed: Improved I/O predictability through explicit Flush calls for all I/O operations. (Breaking change in .NET 4 - closing a FileStream does not always flush it).
* Fixed: Instructions.CropRectangle is now spelled correctly. Mispelling will also remain functional until the next major release.

## Known remaining bugs

* AzureReader2 and 404plugin are incompatible
* When using auto-cropping (mode=crop), scale=down may sometimes be ignored, acting as if scale=both was set.
* .NET 4 introduces a FileStream.Flush(true) method to restore the flush-to-disk guarantee. When we move to .NET 4 in the next major release, we will use it.
* Recent windows updates have reduced the performance of System.Drawing, preventing more than one resize operation at a time per process. You can work around this by using the IIS Web Garden feature, or by switching to the [WIC pipeline](/plugins/wic).

## Improvements to core

* You can now access SourceWidth and SourceHeight after running an ImageJob, as well as the ResultFileExtension and ResultMimeType. Only the default pipeline populates this data for now - WIC and FreeImage support comes later.
* You can now read the size of an image without resizing it, using `ImageBuilder.LoadImageInfo(object source, string[] requestedInfo)`.
* An ImageJob can return information instead of a result image now. Added `new ImageJob(source, requestedInfo)`, ImageJob.RequestedInfo, and ImageJob.ResultInfo.
* Introduced IFileSignatureProvider and added default implementation to DefaultEncoder. This allows plugins to provide unified file type detection data.
* The diagnostics page now warns you if precompilation is detected (it breaks data source plugins).
* Dangerous ImageBuilder methods (such as `LoadImage` and `Build(bitmap,settings)`) are now marked as obsolete (see guidance section above)

### Changes to DiskCache plugin

* Fixed rare 500 bug in DiskCache (serving file that is partially flushed to disk). More likely to occur when file system write caching is disabled.
* Fixed: Diskcache blocked any url including the segment /imagecache/, instead of just the root instance. Note that if you have sub-applications, those /imagecache/ folders will be exposed if DiskCache is not installed.

### Changes to SqlReader

* Added support for stored procedures to SqlReader: via QueriesAreStoredProcedures setting.


### Changes to AzureReader2

* Fixed bug with blob redirection when the application is hosted in a virtual folder instead of at the domain root. (Fixes #49)
* Fixed bug causing 400 errors with newer version of Azure library ([thanks to Martin Larsen](http://stackoverflow.com/questions/18791817/receive-400-bad-request-from-image-resizer-request-in-azure))

## Changes to MongoReader

* Updated MongoReader to 1.8 (Thanks @jakenuts!)
* Updated Newtonsoft.Json 

### Changes to Watermark

* Watermark now throws a 500 error instead of a 404 if a watermark image can't be found. (#23)

### Changes to RemoteReader

* Fixed bug in RemoteReader - extension correction was still failing to work (causing PNG images to be rencoded as jpegs when no format was explicitly specified).

#v3-3-3
Date: March 2 2013

FullFile: http://downloads.imageresizing.net/Resizer3-3-3-full-Mar-2-2013.zip

## S3Reader AWSSDK binding failure

Amazon [silently changed their private key](
http://stackoverflow.com/questions/14921297/is-it-possible-to-ignore-assembly-manifest-mismatch) for AWSSDK.dll, causing widespread havoc when binding redirects couldn't be applied.

We've updated our internal copy of AWSSDK to the latest version, and we are building against Amazon's new private key.

In addition, we've modified our build scripts to prevent any version-specific or strong-named references. This should allow you to upgrade AForge, Azure, AWSSDK, NLog, and OpenCV more easily in the future.


## WicDecoder

WicDecoder: Fixed NullReferenceException when decoding certain types of images.

## Release notes

Are you using the FreeImage, WebP, Faces, or RedEye plugins with downloadNativeDependencies="true"? Make sure you call `Config.Current.Plugins.LoadPlugins()` from `Application_Start` in `Global.asax.cs` to prevent a rare loading bug.

Not yet fixed: When using auto-cropping (mode=crop), scale=down may sometimes be ignored, acting as if scale=both was set.


#v3-3-2
Date: Jan 24 2013

FullFile: http://downloads.imageresizing.net/Resizer3-3-2-full-Jan-24-2013.zip

This release fixes a performance issue in DiskCache, which was preventing 304 Not Modified responses from being returned. The bug was introduced in 3.3.0

## Release notes

Are you using the FreeImage, WebP, Faces, or RedEye plugins with downloadNativeDependencies="true"? Make sure you call `Config.Current.Plugins.LoadPlugins()` from `Application_Start` in `Global.asax.cs` to prevent a rare loading bug.

Not yet fixed: When using auto-cropping (mode=crop), scale=down may sometimes be ignored, acting as if scale=both was set.

## Bug fixes in Core (which affected DiskCache)

* Fixed: IResponseHeaders.LastModified was still being written to HTTP headers, even when it had a value of DateTime.MinValue. Caused by timezone difference.
* Fixed: ClientCache was forcing HTTP last-modified to match the source file. This is incorrect behavior. Together with the previous bug, this was preventing 304-Not-Modified from being returned properly when using DiskCache. Bug was introduced in 3.3.0.
* Fixed: Improved error messages for image decoding issues


#v3-3-1
Date: December 19 2012

FullFile: http://downloads.imageresizing.net/Resizer3-3-1-full-Dec-20-2012.zip

This is a `beta` quality release.

## Notice for users of downalodNativeDependencies=true

To ensure all dependencies get downloaded before ASP.NET starts locking them, call `Config.Current.Plugins.LoadPlugins()` from `Application_Start` in `Global.asax.cs` 

## Bugs that could not be fixed in this release

* When using auto-cropping (mode=crop), scale=down may sometimes be ignored, acting as if scale=both was set.

## Bug fixes in Core

* Core: Fixed incorrect parsing of 'process' command in ResizeSettings - always returned Default, regardless of setting. **Bug was introduced in 3.2.0, and may have security implications for users with untrusted content, as re-encoding was NOT being enforced**.
* Fixed "ArgumentException: Parameter is not valid." when using  `srotate` on **CMYK**(not RGB) Jpegs on *Server 2008 R2 or higher*.
* Fixed &srotate side effects on 'source' object for managed API. 

## Bug fixes in plugins

* AzureReader2 supports Azure Storage Client 2.0
* SourceMemCache: Changed the default cache size from 1GB to 10MB, as documentation specifies.
* Faces & RedEye now use the same directory calculation algorithm used by NativeDependencyManager to provide XML files. ImageProcessingExceptions are thrown instead of FileNotFoundExceptions for missing .xml files (FNFs are auto-converted to 404s).
* Watermark: Added cache-breaking system
* S3Reader has been rebuilt against the latest version of AWSSDK.dll.


#v3-3-0
Date: December 3 2012

FullFile: http://downloads.imageresizing.net/Resizer3-3-0-full-Dec-4-2012.zip

This is a `beta` quality release.

## Known bugs

The following bugs were not discovered in time to be fixed for this release. These bugs are not regressions; they have existed for 8-9 months, but due to rarity were not encountered or reported.

* When using auto-cropping (mode=crop), scale=down may sometimes be ignored, acting as if scale=both was set.
* Attempting to use `srotate` on **CMYK**(not RGB) Jpegs on *Server 2008 R2 or higher* will cause a "ArgumentException: Parameter is not valid.". 
* Changes to watermark settings in Web.config may require cache-breaking (or clearing) to take effect.
* Under concurrent traffic, ASP.NET may lock partially-downloaded assemblies (for plugins with `downalodNativeDependencies=true` set) when running the application the first time on a new server. Call `Config.Current.Plugins.LoadPlugins()` from `App_Start` in `Global.asax.cs` to ensure this doesn't occur.

The November 2012 release of the Azure SDK 2.0 completely broke backwards compatibility with V1.7, meaning we can't upgrade AzureReader to support 2.0 without breaking things for existing users (which would be especially bad for NuGet users). 

The solution going forwards will be to release AzureReader2, and gradually phase out support for AzureReader.

## Bug fixes in Core

* Plugin loading now blocks all requests until complete to prevent early image requests from failing.
* Fixed bug in SizeLimits, where the totalHeight setting was never being read, instead reverting to 3200.
* HTTP Headers: Changed default cache-control to ServerAndPrivate instead of just Private. Increased support for customizing HTTP headers under IIS Classic Mode.
* Fixed KendoUI compatibility issue with MvcRoutingShim
* InterceptModule now respects HttpContext.SkipAuthorization when re-applying URL auth checks: http://stackoverflow.com/questions/13594729/correct-way-to-skip-authorization-with-imageresizer

## Bug fixes in plugins

* S3Reader: Fixed 500 error for underlying 404 and 403s.
* RemoteReader: Added support for remote URLs with non-image extensions (defaults to fake extension used, or format setting if specified)
* Xml:Node  Added beta support for parsing/serializing element text/whitespace/entity contents. 
* FreeImage: Fixed error message "Attempted to read or write protected memory. This is often an indication that other memory is corrupt." exception caused by trying to dispose an image twice.
* FreeImage now supports 48 and 64-bit raw images, and automatically reads RAW files in display mode.
* Faces & RedEye now dispose of loaded cascade files immediately instead of waiting on gc.

## New features in plugins

* FreeImage: Added `&usepreview=true` to read the embedded jpeg in raw files instead of decoding and processing them.
* FreeImage: Added (alpha!) support for multi-page tiff, gif, and ico files to FreeImageDecoder and FreeImageBuilder. 
* AdvancedFilters: Added alpha support for edge feathering:  &a.featheredges=100&a.featherin=.96&featherout=0.3
* AdvancedFilters: Added [automatic white balancing support](/plugins/advancedfilters): &a.balancewhite=true

* Faces: Added 1-line APIs FacesPlugin.GetFacesFromImage() and FacesPlugin.GetFacesFromImageAsString()
* Faces & RedEye plugins have been fully refactored.
* AdvancedFilters: Implemented support for prerender filters
* SourceMemCache now uses LockProvider to optimize concurrency and prevent duplication of effort.
* WicEncoder: Added PNG interlace support via &interlace=true

## Behavioral changes (alpha plugins only)

* RedEye: Changed json schema slightly - rectangles now have .Accuracy instead of .accuracy, in order to match case of other members, and that of Faces.
* Faces: Now automatically looks for 1-8 faces instead of just 1 by default.
* FreeImage: Now decodes RAW files in display mode; this should produce better results, but may change their appearance.

## New plugins

* Added WebP Plugin for decoding and encoding WebP files (alpha). ImageResizer.Plugins.WebP.dll
* Added MemCache plugin - Like SourceMemCache, but for output instead of input. Part of ImageResizer.Plugins.DiskCache.dll

## Sample projects

* Added Samples/_ImageStudio to replace Samples/RedEyeRemoval and Samples/ComplexWebApplication
* Removed ComplexWebApplication, replaced it with _PluginTests
* CustomOverlayPlugin: Now invalidates when overlay files have a modification date change - but only for physically present files (virtual files not supported).

#v3-2-4
Date: August 6 2012

FullFile: http://downloads.imageresizing.net/Resizer3-2-4-full-Aug-6-2012.zip

**NuGet users: The latest ImageResizer.WebConfig package may insert a duplicate element in Web.config when you upgrade. Simply delete the one that doesn't include `requirePermission="false"`.**

	<section name="resizer" type="ImageResizer.ResizerSection"/>
	<section name="resizer" type="ImageResizer.ResizerSection" requirePermission="false"/>

## Bug fixes in 3.2.4

* Fixed S3Reader CryptographicException concurrency bug with accessing private buckets. 
* Fixed loophole-permitting requests to execute while plugin loading is taking place. Should fix failed S3/Azure/Remote requests during a cold start.
* Fixed bug with [Guassian Sharpen for large kernel values](http://code.google.com/p/aforge/issues/detail?id=307&q=GaussianSharpen&colspec=ID%20Stars%20Type%20Status%20Priority%20Project%20Milestone%20Owner%20Summary).
* Fixed NullReferenceExcpetion in RemoteReader when no URLs are whitelisted.
* Fixed bug in ImageBuilder.TranslatePoints() that causes incorrect result coordinates when the image is being cropped. This method is infrequently used, and this bug would not affect image results.
* Fixed potential bug in ImageBuilder.Build - underlyingStream.Dispose() is called after bitmap.Dispose() now. No known effects reported, but this should ensure there are no issues if bitmap.Dispose() (for any reason) requires access to the underlying stream.
* YARSMTF (Yet Another RackSpace Medium Trust fix): Diagnostics page no longer crashes while trying to report OS bitness - if RackSpace prevents access, it will skip the test.

## Improvements in 3.2.4

* Diagnostics: Added warning for IIS6 and lower to remind them to use .ashx.
* Added Faces plugin
* Added CropAround plugin
* RedEye plugin now requires .NET 3.5 and depends on the Faces plugin.
* Improved ImageHandler syntax support to permit overriding mode=stretch and stretch=fill. Added support for DAMP syntax (Umbraco Digibiz Advanced Media Picker)
* Plugins now redact any sensitive information from the /resizer.debug diagnostics page, to protect users who have set CustomErrors=Off.

## Breaking changes in S3Reader

* The S3Reader plugin now depends on AWSSDK.dll instead of LitS3.dll
* S3Reader.S3config is now S3Client, and an instance of AmazonS3Client instead of S3Service
* S3VirtualPathProvider.Service is now S3Client, and an instance of AmazonS3Client instead of S3Service.
* The useSubdomains setting is no longer used (or relevant). 

#v3-2-beta-3
Date: June 30 2012

FullFile: http://downloads.imageresizing.net/Resizer3-2-beta-3-full-Jun-30-2012.zip

This is a beta release: Version 3.2 underwent heavy refactoring. Hundreds of changes were made to more than 258 code files. While the changes were quadruple-checked, there may still be some bugs. 
Be the first to report a given bug to support@imageresizing.net and claim the bounty! The best place to look for bugs is probably in querystring parsing, or the newly added Instructions class.

**NuGet users: The latest ImageResizer.WebConfig package may insert a duplicate element in Web.config when you upgrade. Simply delete the one that doesn't include `requirePermission="false"`.**

	<section name="resizer" type="ImageResizer.ResizerSection"/>
	<section name="resizer" type="ImageResizer.ResizerSection" requirePermission="false"/>

## Bug fixes in 3.2.3

* Fixed false positive warning "An external process indicates it is managing cleanup..." on diagnostics page
* Added support for multi-page .TIFF files that have pages of different dimensions.
* VirtualFolder now works when vpp="false" and for UNC paths.
* Improved support for Rackspace Cloud (eliminated NativeDependencyManager-related issues).

## Improvements in 3.2.3

* Added support for VirtualPathProviders that return IVirtualFile compliant VirtualFile instances, but do not implement IVirtualImageProvider
* [Ben Foster](http://ben.onfabrik.com/) contributed a small plugin that makes it easier to configure which file extensions ImageResizer intercepts. This can be useful if you're not using standard image extensions, or need .ico support, etc. You can find the plugin in Contrib\AdditionalFileExtensionsPlugin

#v3-2-beta-2
Date: June 20 2012

FullFile: http://downloads.imageresizing.net/Resizer3-2-beta-2-full-Jun-20-2012.zip

This is a beta release: Version 3.2 underwent heavy refactoring. Hundreds of changes were made to more than 258 code files. While the changes were quadruple-checked, there may still be some bugs. 
Be the first to report a given bug to support@imageresizing.net and claim the bounty! The best place to look for bugs is probably in querystring parsing, or the newly added Instructions class.

**NuGet users: The latest ImageResizer.WebConfig package may insert a duplicate element in Web.config when you upgrade. Simply delete the one that doesn't include `requirePermission="false"`.**

	<section name="resizer" type="ImageResizer.ResizerSection"/>
	<section name="resizer" type="ImageResizer.ResizerSection" requirePermission="false"/>
	
## Improvements in 3.2.2

* `Instructions` and `ResizeSettings` now offer generic `Get<>()` and `Set<>()` methods for culture-invariant parsing and serialization of primitive types. Introduced via new base class, QuerystringBase.

## Bug fixes in 3.2.2

* Finally eliminated [ExtensionAttribute-related compile-time warnings and errors](http://stackoverflow.com/q/10990536/166893) by removing extension attribute support altogether. The utilities are still usable as static methods, and the new QuerystringBase class minimizes the need for them now.
* Jason Morse fixed a bug in PdfRenderer - ampersands in PDF metadata would prevent the file from being rendered
* VirtualFolder plugin: Fixed bug that occurs when vpp="False" and when virtualPath="folder" (no leading slash or tilde on the path).

## Breaking changes

* If you are already using the extension methods introduced in 3.2.0, you will need to reference them as static methods instead (I.E. `StreamExtensions.CopyStream(s)`, etc).
* The most commonly used extension methods (.Get<> and .Set<>) will continue working, as they were implemented in a base class.

#v3-2-alpha-1
Date: June 4 2012

FullFile: http://downloads.imageresizing.net/Resizer3-2-alpha-1-full-Jun-4-2012.zip

This is an **alpha** release.

Version 3.2 underwent heavy refactoring. Hundreds of changes were made to more than 258 code files. While the changes were quadruple-checked, there may still be some bugs. 
Be the first to report a given bug to support@imageresizing.net and claim the bounty! The best place to look for bugs is probably in querystring parsing, or the newly added Instructions class.

**NuGet users: The latest ImageResizer.WebConfig package may insert a duplicate element in Web.config when you upgrade. Simply delete the one that doesn't include `requirePermission="false"`.**

	<section name="resizer" type="ImageResizer.ResizerSection"/>
	<section name="resizer" type="ImageResizer.ResizerSection" requirePermission="false"/>

## Bug fixes in 3.2.1

* Eliminated ExtensionAttribute-related compile-time warnings (C#) and errors (VB) for, uh, *most* users. 

If you still experience any ExtensionAttribute-related problems, see [ my StackOverflow answer for a list of workarounds](http://stackoverflow.com/a/10996336/166893).

If that doesn't resolve the problem, please e-mail a .zip file of the project to `support@imageresizing.net`, and include your VisualStudio/.NET version numbers (Go to Visual Studio, Help, About, and click `Copy Info`, then paste it into the e-mail). 

#v3-2-alpha-0
Date: June 3 2012

Fullfile: http://downloads.imageresizing.net/Resizer3-2-alpha-0-full-Jun-3-2012.zip
**Note: this release has a compatibility issue with .NET 4 and VB.NET. Use [3.2.1 instead](/releases/3-2-alpha-1).**

This is an **alpha** release containing heavy refactoring. Hundreds of changes were made to more than 258 code files. While the changes were quadruple-checked, there may still be some bugs. 

Be the first to report a given bug to support@imageresizing.net and claim the bounty! The best place to look for bugs is probably in querystring parsing, or the newly added Instructions class.

**Note for NuGet users: The latest ImageResizer.WebConfig package may insert a duplicate element in Web.config. Simply delete the one that doesn't include `requirePermission="false"`.**

	<section name="resizer" type="ImageResizer.ResizerSection"/>
	<section name="resizer" type="ImageResizer.ResizerSection" requirePermission="false"/>

## Known bugs still in this release

* S3Reader has concurrency issues when accessing S3 buckets over SSL. This issue existed in previous releases. The temporary workaround is to only use public buckets with SSL disabled. The next release will migrate from LitS3 to AWSSDK to avoid this and potentially other undiscovered issues.

## What didn't make the cut, but was expected to

* URL Builder API & MVC URL/HTML helpers; I need more users to volunteer for private beta testing before I can publish this kind of API. Please e-mail your Github username to `support@imageresizing.net` if you can volunteer.
* GetCurrentConfig - multi-tenanting support for Umbraco & Orchard. 

## Bug fixes in Core (ImageResizer.dll)

* Fixed background transparency bug when using `s.alpha` on a jpeg image.
* Fixed bug where specifying both `maxwidth` and `width` or `height` and `maxheight` would cause 'mode' to be ignored, assuming it to be 'max'.
* Fixed bug where ResponseHeaders wouldn't convert LastModified value to UTC before checking against UTCNow, triggering an ArgumentException from the ASP.NET framework for invalid modified dates. (Usually, from SqlReader)
* Fixed Diagnostics page false error: `Virtual path outside of the current application is not supported.` 

## Bug fixes in plugins

* Watermark: Fixed `InvalidOperationException: Object is currently in use elsewhere.` issue.
* WhitespaceTrimmer: Upgraded to hotfix release of AForge, fixes AccessViolationException
* WIC: Fixed GC bug (underlying bytes were being disposed before WIC had finished reading them).
* SqlReader now assumes SQL modified dates are UTC instead of server-local. Bug effects: items with a modified date don't get cached until they are X hours old, where X is the time zone offset.
* SqlReader: Changed behavior - If UntrustedData is set, RequireImageExtension automatically becomes false to prevent recoding circumvention.
* All datasource plugins now support multiple instances
* SeamCarving: now triggered by just &mode=carve, both &mode=carve and &carve=true are not required.

## Major changes to Core

* All querystring parsing is now handled through `ImageResizer.ExtensionMethods` instead of `ImageResizer.Util.Utils`. Update your custom plugins.
* All stream copying is now handled through `ImageResizer.ExtensionMethods` instead of `ImageResizer.Util.StreamUtils`.
* Enumeration parsing and serialization is now handled through `ImageResizer.ExtensionMethods`, to allow support for alternate (yet unlisted) variants for given values, and for custom serialization preferences.
* `ImageResizer.Util.ParseUtils` was introduced to restore `System.Drawing.Color` parsing and serialization, and it contains some aliases to NameValueCollectionExtensions.
* System.Drawing dependencies are being minimized. Introduced enumerations `OutputFormat`, `FlipMode`, `AnchorLocation`, `GrayscaleMode`, `JpegSubsamplingMode`, and class `BoxPadding` into root `ImageResizer` namespace.
* The new MVC-friendly `Instructions` class is replacing `ResizeSettings`, and adding support for more of the available commands. The two are easily convertible via their constructors: `new ResizeSettings(new Instructions())` or `new Instructions(new ResizeSettings)`.
* CLSCompliant has been set to false for the ImageResizer.dll assembly. While it's generally CLS compliant, some protected members don't qualify. I don't expect this change to have any ill effects.
* Implemented new rendering stage, PreRenderImage (and matching ImageState.preRenderBitmap variable). This stage permits 'mid-resizing' operations to take place in a chained manner, like seam carving, FreeImageResizing, Red-eye removal, etc. 
* Added support for source file caching plugins, via `IVirtualFileCache` and `IVirtualFileSourceCacheKey`.
* Added support for opt-in automatic native dependency installation with the new `NativeDependencyManager` class and the `NativeDependenciesAttribute` assembly attribute. Extremely useful for PdfRenderer, RedEye, Faces, and FreeImage plugins. 
* Removed ImageResizer.X, ImageResizer.Plugins.X, and ImageResizer.Plugins.Pro.X class location patterns. I.E, you can no longer specify `<add name="Plugins.DiskCache.DiskCachePlugin" />` or  `<add name="DiskCache.DiskCachePlugin" />`, only `<add name="DiskCache" />` or `<add name="ImageResizer.Plugins.DiskCache.DiskCachePlugin" />`.
* Added ResponseArgs.GetSourceImage so plugins can override the entire processing method to, say, return JSON instead of an encoded image.

## Minor changes to Core

* PathUtils now (generally) supports parsing URLs with fragments
* PathUtils.ParseQueryStringFriendly no longer assumes a path is a querystring unless it contains '='.
* Renamed PathUtils.FromBase64UToButes to FromBase64UToBytes
* Removed stub 'Caption' plugin and legacy FriendlyUrls plugin (now called FolderResizeSyntax).
* Querystring commands that accept comma-delimited lists: leading and trailing commas are now ignored, they no longer indicate 'empty' slots.

## Minor additions to Core

* PolygonMath.Dist(a,b)
* PolygonMath.GetShortestPair(poly)
* PolygonMath.NormalizeTo90Intervals
* PolygonMath.CombineFlipAndRotate
* PolygonMath.GetCroppingRectange
* Introduced ImageResizer.Util.ParseUtils as a replacement for certain Obsolete Utils methods. 
* Added Plugins.GetOrInstall<T>()
* Added Plugins/ImageStudio stub project.
* Added AWSSDK, Newtonsoft.Json, and OpenCVSharp to Plugins/Libs

## Marked obsolete in Core

* ImageResizer.Util.Utils (moved to extension methods and ParseUtils)
* ImageResizer.Util.StreamUtils (moved to extension methods)
* ImageResizer.Util.UrlHasher (moved to DiskCache)
* ImageResizer.StretchMode (Use FitMode.Stretch instead)
* ImageResizer.CropMode (Use FitMode.Crop instead)
* ImageResizer.CropUnits (Use cropxunits and cropyunits instead)
* ImageResizer.ResizeSettings was not marked obsolete, but will be in a future release. Time to start using Instructions.cs

## Plugins

This release includes the following new draft plugins: [RedEye](/plugins/redeye), DiskCache\SourceMemCache, ImageInfoAPI, Encrypted, and [Samples\CustomOverlayPlugin](/plugins/customoverlay). 
These plugins are not yet alpha, and will be changing before their final release.

## AdvancedFilters

* Changed the way blur/a.blur, sharpen/a.sharpen, a.removnoise, and a.oilpainting values are interpreted - With this release, your existing images may become slightly blurrier or sharper if you use these commands. This change was only made after consulting all registered users of the plugin. This change makes the specified radius values percentages of the image size. This will allow &blur=5 to have the same effect regardless of the image size, as expected. To be exact, the value will be interpreted as 1/1000ths of the smaller of the image width and height. This provides both granularity and very good consistency.

## AzureReader

* Now supports multiple instances

## DiskCache

* Added SourceMemCache plugin prototype to DiskCache.dll. 

## FreeImage

* Now supports downloadNativeDependencies="true" auto-install!
* FreeImageResizer now supports PreRenderImage system

## MongoReader

* Now supports multiple instances and IVirtualFileSourceCacheKey.

## PsdReader

* Now ignores requests where an alternate decoder is requested, and always attempts decoding with `decoder=psdreader` is specified.

## RedEye

* Alpha version released - supports JSON API for eye detection, URL API for correction.

## RemoteReader

* Added support for extension-less remote URLs.
* Added support for IVirtualFileSourceCacheKey.
* Added alpha support for XML-defined URL pattern whitelisting. (Needs testing)

## S3Reader

* Added support for IVirtualFileSourceCacheKey 
* Added support for multiple instances
* Added alpha support for RequireImageExtension, CacheUnmodifiedFiles, and UntrustedData settings

## SeamCarving

* Added alpha support for manual object removal/preservation with carve.data, LZW/custom dictionary-derived compressed block array.
* Implementation not fully complete.

## Security/Encrypted

* Added Plugins/Security project
* Added first draft of the Encrypted plugin

## SimpleFilters

* Added draft support for `s.roundcorners` command, supporting single and individual radii.

## SqlReader

* Added multi-instance support
* SqlReader: Added checkForModifiedFiles setting - (eliminates per-request DB hit when false).
* Fixed bug: SqlReader now assumes SQL modified dates are UTC instead of server-local. Bug effects: items with a modified date don't get cached until they are X hours old, where X is the time zone offset.
* Changed behavior - If UntrustedData is set, RequireImageExtension automatically becomes false.

## Watermark

* Fixed bug: InvalidOperationException: Object is currently in use elsewhere.

## WhitespaceTrimmer

* Eliminated use of AForge.UnamangedImage, which seems to have solved some memory consistency issues.
* Got hotfix from AForge author to solve AccessViolationExceptions.

### PdfRenderer

* Now supports downloadNativeDependencies="true" auto-install!

#v3-1-5
Date: February 22 2012

FullFile: http://downloads.imageresizing.net/Resizer3-1-5-full-Feb-22-2012.zip

Read the release notes for v3.1), V3.1.3, and V3.1.4 if you're upgrading from 3.0. V3.1 included major changes to all plugins and the core, and included a long list of bug fixes. It also introduced 11 new plugins and 5 rewritten ones. V3.1.3 introduced 2 new plugins and a new path syntax, while V3.1.4 fixed several bugs

This release fixes 19 bugs and adds 8 minor features. Experimental mono support is added, as is a new experimental [MongoReader plugin for accessing GridFS files](http://imageresizing.net/plugins/mongoreader). 


### Known bugs (as of April 19)

These bugs were discovered several weeks after release. Please contact support@imageresizing.net if you are using any of the affected plugins to get a hotfix. These fixes will be part of the official V3.2 release, which has been slightly delayed due to [financial issues with the project](http://icont.ac/101KI). I apologize for this deviation from the normal release schedule. 

* [Fixed in hotfix] - The WIC plugins are exhibiting a garbage collection bug causing some image requests to fail.
* [Fixed in hotfix] - Watermark plugin: InvalidOperationException: Object is currently in use elsewhere.
* [Fixed in hotfix] - Fixed reliability issues with WhitespaceTrimmer by eliminating use of AForge.UnamangedImage.
* [Fixed in hotfix] - False alarm on /resizer.debug page: System.ArgumentException: Virtual path outside of the current application is not supported.
* [Fixed in hotfix] - IMultiInstancePlugin wasn't applied to SqlReader, S3Reader, AzureReader, or MongoReader, preventing them from having multiple instances.
* [Fixed in hotfix] - SqlReader plugin now assumes datas are UTC instead of server-local. Fixes ArgumentOutOfRangeException in certain time zones for recently-updated images.
* [Not yet fixed] - S3Reader experiences failed requests when using encrypted mode. Due to a bug in LitS3, multithreaded use of a single-threaded .NET crypto library. 

### Potentially breaking changes

* Plugins are now loaded at a different time.
* All querystring and config values are now culture invariant, so if you've been using commas instead of periods for the decimal symbol, since your server was non-English, it's time to switch them back.
* Watermark plugin now renders text better, using a better algorithm. Make sure your watermarks aren't somehow messed up by the clearer text.

## Changes in ImageResizer.dll

* For ASP.NET, plugins are now loaded on first request instead of at application start. For non-ASP.NET apps, plugins are now loaded at application start instead of ... never. This bug was caused by a typo introduced in V3.0.7 during a simple refactoring.

* Experimental Mono support!

* &mode=crop now correctly respects &scale=upscaleonly, &scale=downscaleonly, and &scale=upscalecanvas restrictions. Previously, it would cause distortion on images by incorrectly cropping even when the image was being 'left alone' due to the &scale setting. This problem would only occur when specifying both width, height, and &mode=crop and when the original image size was smaller than width/height. Bug introduced in V3.1.1 while adding support for double cropping (combined manual and automatic cropping).

* The ImageResizer now uses culture-invariant number and date handling for all configuration and querystring data. This means that the ImageResizer should now behave the same on an english-language server as a non-english language server. Although only detected as a problem with the PsdComposer plugin, it's possible that this has caused non-integer values for &rotate, &margin, &padding, &crop, &cropxunits, and &cropyunits to be interpreted incorrectly on severs whose culture settings have the inverse meanings for "." and "," than en-US. Other plugins this may have affected would be SimpleFilters, SeamCarving, and WhitespaceTrimmer. This behavior has existed since V1. Please e-mail support@imageresizing.net if this change would be unexpected to you. 

* Fixed bug: &flip=y&rotate=180 and &flip=y&rotate=270 would produce the wrong result by 90 degrees. Only those two combinations of settings were affected.

## Bug fixes by plugin

### PrettyGifs changes

* PrettyGifs no longer throws an exception when &dither=4pass is used. This typo was introduced with V3.1.3. 

### PsdComposer changes

* Psd Parser: Culture-invariant, and now BinaryReverseReader explicitly uses code page ISO-8859-1 instead of the system default. 
* PsdCommandBuilder: Added support for null and empty string values in string dictionaries. Made color parsing culture invariant. Made all layer name comparisons OrdinalIgnoreCase instead of CurrentCultureIgnoreCase
* MemCachedFile now throws FileNotFoundException when the PSD files are missing, which turns into an HTTP 404 error and allows Image404 to work.
* Now supports virtual source files, anything IVirtualFile (must be registered before those plugins, however).
* No longer inherits from VirtualPathProvider - just implements IVirtualImageProvider. No longer supports IVirtualFileWithModifiedDate due to caching without dependencies.
* When strict mode is disabled, ignores missing fonts and uses generic sans serif instead.
* When PsdReader and PsdComposer are used together, only bundle 4 is reporting being used.

### CloudFront changes

* New redirectThrough feature now works properly on querystrings with more than one command.
* Now compatible with Amazon S3 buckets that contain periods.
* Now compatible with extensionless urls.

### RemoteReader changes

* Added support for redirects (default limited to 5, configurable with remotereader.allowRedirects), added 403 error pass through (404 already implemented).
* Added support for extension-less remote URLs, even using the human-friendly syntax.
* Added support for spaces and many url-encoded characters in the human-friendly syntax.

### PdfRenderer changes

* Fixed version number by including shared version file
* Added support for PDF files with rotated pages
* Added pdfwidth and pdfheight querystring settings to allow overriding the ghostscript rendering size for higher quality thumbnails.

### Watermark plugin changes

* Fixed: font style attribute was being ignored.
* Changed the default text rendering algorithm to one better suited to image compositing.
* Fixed bug in OtherImages configuration; was previously throwing an exception for querystring-specified filenames.
* Added Rendering property/attribute for text layers to allow adjustment of rendering algorithm. Changed the default from ClearType to AntiAliasGridFit (better for images).

### DiskCache changes 

* Now logs an issue if cleanup strategy values are modified.
* Fixed flaw in file lookup algorithm which caused a subset of files to 'miss the cache', always regenerating instead of using the existing version. This flaw affected virtual files without modified dates; specifically RemoteReader provided files, Gradient plugin files, and S3Reader files when fastMode was enabled.
* DiskCache: Fixed bug where a 0-byte file would get cached when an exception occurred during the e.ResizeImageToStream method. For optimistic IVirtualImageProviders (Like S3Reader in fastMode, RemoteReader, and MongoReader, but not VirtualFolder, SqlReader, Gradient, or S3Reader with fastMode=false), this would cause the second request for the missing file to return a 0-byte result instead of a 404. This behavior would be noticed most easily with the 404Plugin installed, as it would only redirect the first request, and not during subsequent ones. 

### ImageHandlerSyntax changes

* Removed legacy w->width and h->height conversion, as 'w' and 'h' are now supported by the core, and it didn't support mixing the conventions properly.

### Image404 changes

* Now normalizes both the original querystring and the 404 image's querystring before merging them to prevent duplicates like ?width=200&w=400.

#v3-1-4
Date: January 22 2012

FullFile: http://downloads.imageresizing.net/Resizer3-1-4-full-Jan-22-2012.zip

Read the release notes for v3.1 and V3.1.3 if you're upgrading from 3.0.X. V3.1 included major changes to all plugins and the core, and included a long list of bug fixes. It also introduced 11 new plugins and 5 rewritten ones. V3.1.3 introduced 2 new plugins and a new path syntax.

This release adds minor new features and fixes some minor bugs. 

## Major bug fixes

* The setter for the ResizeSettings.Height property works again (it was broken in 3.1.3 due to a typo made while adding the w/h syntax).
* ImageBuilder now throws a FileNotFoundException instead of a NullReferenceException when a ASP.NET virtual path like  "~/file.png" doesn't exist.

## Minor new features

* Added ImageJob.CreateDir() alias - a chainable way to set CreateParentDirectory to true.
* Added /resizer.debug warning when running under IIS7 classic mode instead of integrated mode.

## Minor bug fixes

* Now compiles under Visual Studio 11 preview
* Better error messages for empty or read-to-end streams from a (I)VirtualFile or HttpPostedFile source value.
* ImageBuilder.Build, LoadImage, and GetStreamFromSource now throw an ArgumentNullException if parameter 'source' is null.

#v3-1-3
Date: January 14 2012

FullFile: http://downloads.imageresizing.net/Resizer3-1-3-full-Jan-14-2012.zip

Read the release notes for v3.1if you're upgrading from 3.0.X. V3.1 included major changes to all plugins and the core, and included a long list of bug fixes. It also introduced 11 new plugins and 5 rewritten ones.

This release fixes some bugs in V3.1, adds some new features, and introduces 2 new plugins, [PdfRenderer](/plugins/pdfrenderer) and [IEPngFix](/plugins/iepngfix). 

## Bug fixes

* Fixed a theoretical memory leak in the PrettyGifs plugin (undisposed Bitmap instance), inside the quantization algorithm I ported many years ago. I believe it's gone undiscovered this long because most GIF images are small, and of insufficient volume to stay ahead of the garbage collector. This bug hasn't been seen in practice; I discovered it during a routine self-code-review of the PrettyGifs source code. You should immediately update to 3.1.3 if you use the PrettyGifs plugin and generate a high volume of GIF or 8-bit PNG images.
* Fixed 'black background when rotated odd angles' bug, which was introduced in version 3.1.2.
* [RemoteReader](/plugins/remotereader) now works properly with the 404Plugin, instead of failing to redirect to the 404 image.
* [S3Reader](/plugins/s3reader) no longer silently fails when there is a problem with the S3 bucket; the error is thrown properly, and is displayed if error display is enabled. Also, bucket names are now case-insensitive and whitespace is ignored.
* [S3Reader](/plugins/s3reader) now supports non-US buckets when useSubdomains="true". The default is false for compatibility with legacy buckets.  
* When an empty stream is passed to the ImageResizer, a more informative message is used in the exception. 
* Null values assigned to querystring keys during the Rewrite event. No longer block default values assigned in the RewriteDefaults event.

## New destination path syntax

Tired of doing path math? You can now do most renaming tasks with a simple syntax instead of writing your own string logic.

Uploading a file with a GUID filename is now much simpler: 

	ImageJob i = new ImageJob(file, "~/uploads/<guid>.<ext>", new ResizeSettings("width=1600"));
	i.CreateParentDirectory = true; //Auto-create the uploads directory.
	i.Build();

### List of variables

* &lt;guid> - lowercase hexadecimal GUID with no dashes or brackets
* &lt;ext> - the correct extension for the file based on how it will be encoded
* &lt;filename> - the original filename (minus folder and extension)
* &lt;path> - the original path (dir and filename) without the original extension.
* &lt;originalext> - the original extension
* &lt;width> - the final width of the written image
* &lt;height> - the final height of the written image
* &lt;settings.*> - any value from the settings used to resize it.

### Filtering (alpha)

You can also filter values

* <filename:A-Za-z0-9_-> - Will keep only alphanumerics, dashes, and underscores from the filename.

This can be useful, as uploaded filenames are usually unacceptable for use on the server, even when combined with a guid.

## New plugins & cool features

* Jason Morse brought us the [PdfRenderer plugin](/plugins/pdfrenderer), which I promptly reviewed and tested, and decided to include immediately due to its great code quality.
* The extremely boring [IEPngFix plugin](/plugins/iepngfix), for those who still care about IE6. 
* It's official; you can now use 'w' and 'h' instead of 'width' and 'height'. 9 less characters to type!
* [Presets plugin](/plugins/presets) now works with managed API as well as URL API. (Managed API does not enforce the OnlyAllowPresets setting). 
* The ImageResizer can now 'auto-create' the parent directory for an image it is writing. Use ImageJob and set `CreateParentDirectory` to true. Nice for uploads.

## Boring new stuff

* Added ResizeSettings.CropXUnits and ResizeSettings.CropYUnits
* Added ResizeSetting constructor that accepts width, height, FitMode, and image format.
* PrettyGifs: Added preservePalette setting.
* ImageJob: added Stream->Stream constructor overload
* ImageHandlerSyntax: Now &mode can be used even in the image handler syntax.
* Diagnostics page now reports on trust level and incompatible assemblies.
* Improved error reporting for plugins that subclass VirtualPathProvider on GoDaddy hosting.
* PathUtils: Added GuessVirtualPath and GenerateImageUrl
* Added Config.Current.Pipeline.ModuleInstalled variable.
* PathUtils.MapPathIfAppRelative, ResolveVariablesInPath, and RemoveNonMatchingChars were added as supporting methods for the dest path syntax.
* You can turn off the new destination path syntax with ImageJob.AllowDestinationPathVariables = false

## Changes that may break your build.

* ImageJob.DisposeSourceStream was renamed to DisposeSourceObject as the previous name was incorrect; both Bitmaps and Streams are disposed by it. It's unlikely many people use this property already as it was only recently introduced.

#v3-1-alpha-2
Date: Dec 7 2011

FullFile: http://downloads.imageresizing.net/Resizer3-1-alpha-2-full-Dec-7-2011.zip

This is the first public release of 3.1. It's a major milestone, involving over 229 commits, introducing 11 new plugins, and bringing majorly cool features or overhauls to 10 existing plugins. Please contact support@imageresizing.net with any bugs or suggestions about this release.

In fact, to encourage everyone to actively look for bugs and report them, I'm **currently offering a $5-$50 bounty per bug you find and report before anyone else**! Most bug reports are firmly in the $5 category, but a security hole or memory leak could net you $50 for the one bug. Payments sent via PayPal. All class libraries whose names start with ImageResizer are eligible. Email bug reports to `support@imageresizing.net`.

Please [visit the idea forum](http://resizer.uservoice.com/forums/108373-image-resizer-v3) and share any new plugin ideas or features you don't see listed in this release or on the forums. 

### Changes since 3.0.13 (Oct 12)

As nearly all plugins were modified in this release, please refer to the individual plugin-specific sections below to see how you might be affected.

Version 3.1 adds the following *free* plugins:

* [SpeedOrQuality](/plugins/speedorquality) - Sacrifice quality for 10-40% more speed. Still in alpha.
* [DefaultSettings](/plugins/defaultsettings) - Decide which ScaleMode setting should be the default, and for which situations. Many people find the &scale=downscaleonly default value frustrating - now they can change it.
* [Presets](/plugins/presets) - Create preset setting groups in Web.config and use them from the URL. 
* [AutoRotate](/plugins/autorotate) - Automatically rotate the image based on the camera's gravity sensor data, with `&autorotate=true`
* [Logging](/plugins/logging) - NLog wrapper to allow optional logging. Logging only implemented for DiskCache plugin at the moment.

The Design bundle gets these new plugins. If you've already bought the Design bundle, you get these for free!

* [WicEncoder](/plugins/wic) - Encode jpegs with adjustable quality and subsampling settings. Encode GIFs and 8-bit PNGs with adjustable palette size and bit depth.
* [WicDecoder](/plugins/wic) - Decode images using any WIC-enabled codec installed on the computer
* [WicBuilder](/plugins/wic) - Provides a completely alternate pipeline, which supports most basic resize/crop/pad operations. 2-4x faster than the default pipeline. Enable on a per-request basis using `builder=wic`.
* [FreeImageResizer](/plugins/freeimage) - Adds support for Lanczos3 and CatmullRom scaling algorithms: &fi.scale=bicubic|bilinear|box|bspline|catmullrom|lanczos command

The Extras bundle is introduced, and starts out with the following 2 plugins

* [BatchZipper](/plugins/batchzipper) - Asynchronously generated .zip archives of files and resized images. Great for providing customized downloads to customers.
* [PsdComposer](/plugins/psdcomposer) - Dynamically edit and render PSD files - turn layers on and off, edit text layer contents, and apply certain effects.


### FriendlyUrls reminder

If you are still using the FriendlyUrls plugin, [you need to rename it in Web.config - it has been part of the core for 3 releases now as FolderResizeSyntax](/plugins/friendlyurls), and is no longer included as a separate dll.

## Cool new features in the ImageResizer.dll core project

* `mode=max|pad|crop|stretch|carve` provides a single command to control how aspect ratio changes are handled. It doesn't introduce new features, but is easier to remember than `maxwidth` vs `width`, `crop=auto`, `stretch=fill`, and `carve=true`. Although, as always, everything is fully backwards-compatible. Example: `?maxwith=10&maxheight=10` is equivalent to `?width=10&height=10&mode=max`. 
* Control how images are cropped or padded (when the aspect ratio is being modified) using the `anchor` command:  &anchor=topleft|bottomright|middlecenter... 
* You can now rotate the source image in 90 degree intervals using `sRotate`. This is useful if you'd like to rotate prior to doing a manual crop.
* The new `ImageBuilder.Build(Job job)` overload allows you more control over how streams are handled - you can even choose to dispose the destination stream when the job is complete, or restore the position of the original stream.
* You can now specify individual left, top, right, and bottom edge widths for margin, borderWidth, and paddingWidth values. I.e, `?margin=10,20,10,20&borderWidth=50,10,50,10&borderColor=blue`

## Boring new features in ImageResizer.dll 

* Set the print density of an output image with `&dpi=96|300|600`. No browsers support this, but a few printers do. Default is 96.  See [this article on why the DPI (resolution) setting is worthless](http://apptools.com/examples/dpi.php).
* Added performance optimization for resizing non-transparent to non-transparent images with no padding.
* Diagnostics page has dozens of new checks to ensure you've configured everything for optimal performance.
* &scale=down|up|canvas are newly supported shortcuts for downscaleonly,  upscaleonly, and upscalecanvas
* You can now perform a source flip on an image using `sFlip` instead of `sourceFlip`, although the longer name is also supported.
* ResizeSetting now accepts semicolon-delimited pairs in its constructor: `new ResizeSettings("width=100;height=100")` Yay for readability!  
* You can now both perform a manual crop, then autocrop the result to a specified with and height. I.e. `image.jpg?crop=400,10,-10,-10&width=100&height=100&mode=crop`

For a list of API changes that affect plugin developers, see the bottom of the page.

## The last surviving GDI+ artifact, dead

After 3 years of wrestling with the last GDI+ border bug (the 50% transparency white 1px border on resized images), it has finally been killed! 

With the introduction of the WIC and FreeImage pipelines, I finally tried the border test on them as well, and guess what... All three had the border artifact! Knowing that to be impossible, I checked the original photos, and it was there as well. Sometime in 2008, those photos were resized with V2.0, and acquired the border artifact. The last remaining GDI border bug was actually solved in V2.6, but since the border was already in the original images, it only showed up as a 25% reduction in brightness. 

Chagrin would be the word. How could I ever fix a non-existent bug? I sure spent a lot of time trying...

## [DiskCache](/plugins/diskcache)

* Greatly reduced CPU usage when autoClean="true" (for certain rare situations).
* Added beta support for async writes. Enable by setting `asyncWrites=true` on the `<diskcache>` element. The async buffer size defaults to a maximum of 10MB, but can be changed with the `asyncBufferSize` setting (which is in bytes). Async writes can greatly improve performance if you have a slow, overloaded, or high-latency hard disk/SAN.
* Fixed bug in LockProvider where a failed image resizing request would cause the lock provider to fail to remove the lock object for the request from the dictionary. If you have millions of failed requests during a single app pool cycle, this could cause reduced performance an higher RAM usage.
* Added support for cooperative cache cleanup, using a mutex to prevent more than one process from cleaning the same folder at the same time. Should help support extended overlapped recycles and web garden scenarios.
* Added support for recovering from another process writing to the same cache file at the same time. Only works in `hashModifiedDate=true` mode.
* Added optional logging support so you can see exactly what is happening with the cache in real-time
* Handles insufficient permissions more gracefully, explains issue on the diagnostics page

## [PrettyGifs](/plugins/prettygifs)

* Fixed bug where an OverflowException would occur when generating a GIF or 8-bit PNG file on a 64-bit server, but only when over 4GB of ram was in use.


## [Watermark](/plugins/watermark) plugin

We admit, the original Watermark plugin was sad. 

The new one isn't. Here are a few of the new features

* Fully XML configurable. No more Global.asax.cs junk.
* Full-featured text layers
* Flexible image layers
* Layer groups 
* Reference multiple groups or layers from the URL
* Flexible layer layout system to shame even WPF. Anything is possible, and it's intuitive. 
* Overlay AND background layer support.
* Image layers can use any virtual or physical file - even a generated gradient from the Gradient plugin! 
* Image layers can be pre-processed with the same pipeline... they can even have their own watermarks inside. 
* Text layers support both fixed and scale-to-fit font sizes, configurable font typefaces, and font styles.
* Text layers support text outline and glow effects (even combined), all colors are configurable, and the text can be rotated to any angle.

Oh, and guess what - it's fully backwards compatible. You can take advantage of the new features while still supporting your old code and URLs. You can migrate your old code to the new XML syntax at your leisure. Your URLs don't ever need to be changed.


## [SimpleFilters](/plugins/simplefilters)

* Added support for combining multiple filters
* Added adjustable grayscale conversion
* Added brightness, contrast, and saturation adjustment settings ("s.brightness", "s.contrast", "s.saturation")
* Fixed broken sepia filter
* Added invert filter


## [SizeLimiting](/plugins/sizelimiting)

* Size limits are now inclusive instead of exclusive, to match normal user expectation

## [VirtualFolder](/plugins/virtualfolder)

* Now can fall back to IVirtualImageProvider
* Handles insufficient permissions more gracefully, explains issue on the diagnostics page


## [FreeImage](/plugins/freeimage)

###Breaking changes

* FreeImageEncoder no longer acts as the default encoder. To enable, use `encoder=freeimage` in the request URL.
* FreeImageBuilder is no longer activated via &freeimage=true. To activate, use `builder=freeimage`.
* FreeImageDecoder can be requested as the primary decoder with `decoder=freeimage` instead of `freeimage=true`. As always, it is a default fallback decoder if the other decoders can't handle the stream.

###The good news

* Got a customized version of FreeImage based on libjpeg-turbo. 2-4x faster jpeg encoding and decoding!
* Improved overall performance of non freeimage-related requests when a FreeImage plugin was installed by reordering some statements.
* Now works with http:// source URLs when RemoteReader is installed.
* Now works with COM clients such as ASP
* FreeImageBuilder now supports all the standard 'source' and 'dest' formats (except Bitmap), plus all the standard parameters.
* Added FreeImageResizer plugin, with &fi.scale=bicubic|bilinear|box|bspline|catmullrom|lanczos command. Uses GDI resizing afterwards for cases where stretching or rotation occurs. Useful if you need a high-quality upscaling algorithm or something a bit more specialized than GDI's 2-pass bicubic algorithm.
* FreeImageBuilder longer rescales image unless needed.
* FreeImageEncoder now supports `quality=10|25|50|75|100`, `subsampling=444|422|420|411`, and `progressive=true` for adjusting jpeg encoding!
* FreeImageDecoder and FreeImageBuilder now support &autorotate=true

## [CloudFront](/plugins/cloudfront)

* Now supports (optional) automatic redirection - change a setting to redirect all image traffic through a CloudFront distribution. `<cloudfront redirectThrough="http://cdn.mysite.com/" redirectPermanent="false /">`
* Fixed bug - now handles folders with a '.' in them properly, i.e, /folder.ext/file.ext;width=100 (ASP.NET's PathInfo madness...).
* Now merges query strings during PreAuthorizeEvent instead of at RewriteDefaults, which should be more expected. 
* Now plays nice with other PreRewritePath and ModifiedQueryString stuff that may be registered.

## [SqlReader](/plugins/sqlreader)

* Now completely configurable via XML
* Added RequireImageExtension, CacheUnmodifiedFiles, and UntrustedData settings
* 
Also added complete XML configurability.

* Can now fallback to IVirtualImageProvider if security restrictions prevent it registering as a VirtualPathProvider. Or, you can manually request it only register as an IVirtualPathProvider by setting `vpp="false"`. IVirtualImageProviders can only provide files to the image resizer.
* Fixed infinite loop triggered by handling the BeforeAccess event (stupid typo)
* SqlReader: Added Settings property to allow easy access to configuration.
* Renamed methods to indicate they are ready for public consumption.
.authorize -> .FireAuthorizeEvent
.getStream -> .GetStream
.getIdParameter -> .CreateIdParameter
.rowExists -> .RowExists
.getDateModifiedUtc -> GetDateModifiedUtc
.getIdFromPath -> ParseIdFromVirtualPath

## [AzureReader](/plugins/azurereader)

* Can now fallback to IVirtualImageProvider if security restrictions prevent it registering as a VirtualPathProvider. Or, you can manually request it only register as an IVirtualPathProvider by setting `vpp="false"`. IVirtualImageProviders can only provide files to the image resizer.
* Can now use lazy blob existence checking to increase performance. Use `lazyExistenceCheck=true` to enable (warning: untested).
* Fixed bug where 'prefix' values not starting in ~ or / wouldn't be handled correctly.
* Fixed bug where images outside the prefix directory were assumed to be image files
* Now a nuget package

## [S3Reader](/plugins/s3reader)

* Can now fallback to IVirtualImageProvider if security restrictions prevent it registering as a VirtualPathProvider. Or, you can manually request it only register as an IVirtualPathProvider by setting `vpp="false"`. IVirtualImageProviders can only provide files to the image resizer.

## [RemoteReader](/plugins/remotereader)

* RemoteReader: Now overrides GetStream() instead of PreLoadImage() so that it can be used across all pipelines. Added support for underscores instead of periods in the domain name of the 'friendly' syntax. Allows users to avoid peculiar IIS configurations.

## [ImageHandlerSyntax](/plugins/imagehandlersyntax)

* WebImageResizer compatibility - now supports grayscale and invert commands when the SimpleFilters plugin is installed.

## [Logging](/plugins/logging)

* New Logging system - the Config.Current.Plugins.LogManager property and the Config.Current.Plugins.LoggingAvailable event allow plugins to support logging without having NLog as a dependency.
* New Logging plugin provides an NLog-based implementation of the logging system. This allows logging to be supported, but not required - no extra dependencies or overhead unless you add the Logging plugin!.
* The DiskCache plugin now supports detailed (optional) logging. Enabled by setting `<diskcache logging="true"/>`, installing the Logging plugin, and configuring NLog. 

# Core API changes

## Breaking API changes that affect nobody

* The ImageBuilder.Create() and ImageBuilder constructors now require 2 additional arguments, "IVirtualImageProvider virtualFileProvider" and "ISettingsModifier settingsModifier". As the ImageBuilder class shouldn't be created directly, this change should not affect anyone. Only subclasses of ImageBuilder should be affected - they will need to modify their constructors, .Create(), and .Copy() methods to include this new parameter. This change does not affect plugins or user code - only ImageBuilder subclasses (of which there are currently none).
* Removed unused constructor overload ImageBuilder(IEncoderProvider encoderProvider, IVirtualImageProvider virtualFileProvider) as it didn't offer anything new, and added to cost of implementing a subclass.
* The internal class BitmapHolder is no more. It has been replaced by ImageJob
* Non-breaking change: Config.Pipeline.GetFile now always returns an IVirtualFile instance, instead of an object which could either be a VirtualFile or IVirtualFile instance.

## API additions

* New class ImageJob - A class to contain all the parameters of a job. Makes alternate pipeline support easier.
* Added ImageBuilder.Build(Job job) overload. All other overloads now funnel through this method, and subsequently through extensible protected method BuildJob()
* ImageBuilder.LoadImage has a new overload with a 3rd parameter, restoreStreamPosition.
* New method for converting 99% of the 'source' object types into a stream: Stream ImageBuilder.GetStreamFromSource(object source, ResizeSettings settings, ref bool disposeStream, out string path, out bool restoreStreamPosition); Plugins can extend by subclassing BuilderExtension and overriding protected method GetStream (same parameters). This method makes it easier to build replacement pipelines.
* ImageBuilder, AbstractImageProcessor: Added PostDecodeStream() method, called at end of LoadImage method. Used by AutoRotate
* ImageResizer.Configuration.Logging namespace - ILogManager, ILogger, ILoggerProvider and PluginConfig support mean that the ImageResizer can support any logging library - with no extra dependencies. 


## Extensibility additions

* Alternate pipelines can now be implemented as BuilderExtension plugins instead of being forced to subclass ImageBuilder.

* AbstractImageProcessor: Added `protected virtual RequestedAction BuildJob(ImageResizer.ImageJob job)` Enables replacement pipelines to be implemented as a plugin.

* AbstractImageProcessor: Added `protected virtual Stream GetStream(object source, ResizeSettings settings, ref bool disposeStream, out string path, out bool restoreStreamPosition)` Allows RemoteReader and similar plugins to support alternate pipelines.
* ISettingsModifier plugins are now supported, and allow modified of resizing settings without subclassing BuilderExtension.


## Minor bug fixes

* Plugins missing a constructor don't crash the request; they just log an issue.
* If a plugin throws an exception during GetIssues, it is now logged as an issue instead of crashing the resizer.debug page.
* Earlier disposal of Pen and Brush instances used for rendering image padding, borders, and drop shadow.

## Deprecated

* ImageResizer.StretchMode and ImageResizer.CropMode are deprecated, and have been replaced by ImageResizer.FitMode

#v3-0-13
Date: October 12 2011

FullFile: http://downloads.imageresizing.net/Resizer3-0-13-full-Oct-12-2011.zip

### Changes since 3.0.12 (Aug 15)

## Manual steps for users of the FriendlyUrls plugin

Version 3.0.13 does not include `ImageResizer.Plugins.FriendlyUrls.dll`! The FriendlyUrls plugin is now included in `ImageResizer.dll`, and has been renamed to `FolderResizeSyntax`. This change reduces the number of dlls you need to deploy, and simplifies migration for v2 customers.

* Change `<add name="FriendlyUrls" />` to `<add name="FolderResizeSyntax" />` in Web.config and remove `ImageResizer.Plugins.FriendlyUrls` through Project References, /bin/, or nuget. 

## New features

* New alpha [FreeImageDecoder plugin](/plugins/freeimage) introduces support for RAW & HDR image formats, such as CRW/CR2, NEF, RAF, DNG, MOS, KDC, DCR, etc. Also introduced support for XBM, XPM, TARGA, SGI, Sun RAS, PSD, PICT, PNG, PFM, PBM, PGM, PPM, PCX, MNG, Kodak PhotoCD, KOALA, JPEG-2000, JIF, JNG, IFF, ICO, Raw Fax G3, EXR, DDS, and Dr. Halo CUT files.

## Bug fixes

* Fixed: Using the &404 command without any other resizing commands would cause the image to be re-encoded needlessly. 
* Fixed: Converting a transparent PNG or GIF to jpeg format would cause the transparent areas to become black unless otherwise specified. Now defaults to white.
* Fixed: RemoteReaderPlugin was modifying the cache key incorrectly, causing different remote URLs to be cached as the same request.
* Fixed: DiskCache; setting subfolders=1 no longer causes config error - behaves as subfolders=0 
* Fixed: Watermark plugin would display decreasingly smaller watermarks.
* Fixed in ClientCache plugin: On a web server serving images from another file server, when the servers' clocks are not synchronized, and images are served within the time offset (between the servers) from when they are created. Symptoms: ArgumentOutOfRangeException. Fault: Overzealous ASP.NET framework code. Extremely rare.

#v3-0-12
Date: August 15 2011

FullFile: http://downloads.imageresizing.net/Resizer3-0-12-full-Aug-15-2011.zip

### Changes since 3.0.11 (Jul 28)

## Upgrade steps for users of the FriendlyUrls plugin

Make sure you perform the following step before upgrading to 3.0.13, as `ImageResizer.Plugins.FriendlyUrls.dll` will not be included in the next version.

* Change `<add name="FriendlyUrls" />` to `<add name="FolderResizeSyntax" />` in Web.config and remove `ImageResizer.Plugins.FriendlyUrls` from Project References, /bin/, or nuget. 

## New features

* Added new [WhitespaceTrimmer](/plugins/whitespacetrimmer) plugin to Design bundle. (Also on NuGet)
* Added new [ImageHandlerSyntax](/plugins/imagehandlersyntax) plugin to Core. (Provides URL syntax compatibility with 4 other image resizing handlers)
* Added new [MvcRoutingShim](/plugins/mvcroutingshim) plugin to Core.Mvc, in the ImageResizer.Mvc.dll assembly. (Allows MVC routing to be disabled for certain requests - useful when routes are interfering with images).
* Added NuGet packages for Watermark, BatchZipper, WhitespaceTrimmer, and ImageResizer.Mvc
* Added dlls\release\COMInstaller.exe for quick and accurate COM installation. 
* Added SampleAspSite project for ASP users.
* Added a copy of the [ImageStudio](http://imagestudio.codeplex.com) project in the Contrib folder. The ImageStudio project now uses the ImageResizer to perform image manipulation, resulting in higher-quality images. 
* AdvancedFilters now has experimental support for oil painting filters, noise removal, histogram equalization, contrast, brightness, and saturation adjustment. These features may disappear, change, or be modified based on feedback. Share yours.

## API additions/changes

* New method RemoteReaderPlugin.CreateSignedUrlWithKey(string remoteUrl, string settings, string key) allows COM clients (such as ASP) to generate signed URLs.
* Config.Current.BuildImage() now creates the destination directory if it is missing, as a convenience for COM clients. ImageBuilder.Current.Build() does not do this. 


## Significant bug fixes

* Core: Now works in low-trust on .NET 4. Fixed "[VerificationException: Operation could destabilize the runtime](http://stackoverflow.com/questions/6919808/why-does-this-line-cause-a-verificationexception-when-running-under-net-4)" error.
* Core: Diagnostics page now works in low-trust, thanks to Jesse Ehrenzweig's patches! Also thanks to Jesse, there are now far few compiler warnings!
* SeamCarving: Now works through Build() as well as the URL syntax.  &carve=true was previously only accessible through the URL syntax.
* SeamCarving: Now maintains transparency for GIF and 8-bit PNG images. 32-bit PNG images still lose most of their alpha channel due to limitations in CAIR.EXE.
* AnimatedGifs: Fixed bug where animated (but non-looping) gifs would start looping in certain browsers. 
* SimpleFilters: Brightness adjustments no long cause color inversions on overexposed photographs

## Insignificant bug fixes

The bugs that nobody has probably ever encountered, but were turned up by comprehensive unit testing.

* Core: Fixed NotImplemetedException when &borderWidth is negative
* Core: Fixed exception when &paddingColor is specified but &paddingWidth is not used.
* Core: Fixed bug where &margin= would not be applied unless other commands were present.
* AdvancedFilters: Fixed: &sharpen and &blur were not taking effect unless used with other commands. 
* DropShadow: Fixed bug where ImageBuilder.GetFinalSize() causes a NullReferenceException when &shadowWidth or &shadowColor is used
* AdvancedFilters: Fixed NullReferenceException when ImageBuilder.GetFinalSize() is called and &blur or &sharpen is specified


#v3-0-11
Date: July 29 2011

FullFile: http://downloads.imageresizing.net/Resizer3-0-11-full-Jul-28-2011.zip

### Changes since 3.0.10 (Jun 16)

## New features

* Added [SeamCarving](/plugins/seamcarving) plugin, uses C++ CAIR.EXE underneath. Requires full trust.
* Added [RemoteReader](/plugins/remotereader) plugin. Allows images located on external sites/servers to be processed and resized using both the URL and managed APIs.
* ImageResizingGUI and BatchZipper are now 200KB lighter (using Ionc.Ziplib.Reduced version)
* Build() now supports HttpPostedFileBase as a valid source.
* Build() now supports byte[] as a valid source.
* There are now [NuGet packages](/docs/nuget) for 12 of the 14 plugins. AzureReader and Watermark do not yet have NuGet packages.

## Bug fixes

* Fixed bug in StreamUtils - was causing Build() to fail on non-seekable source streams. Added unit test.
* The incorrect file type was being guessed for png and gif images that (a) didn't have a file extension, or (b) were loaded directly from a stream.
	This was also causing AnimatedGif resizing to crash completely. The source was a .NET framework bug:
	`ImageFormat.Gif.Equals(f)` evaluates to `true`, yet 	`ImageFormat.Gif == f` evaluates to `false`. Switched to using .Equals().
* AnimatedGif plugin no longer closes the source stream incorrectly.
* Fixed bug in LoadImage: HttpPostedFileBase stream is returned to its original position after use. 
* Fixed several bugs in Samples\ImageResizerGUI
* Added lots of tests for new functionality

## API additions

* Added URL-safe base 64 encoding and decoding methods to PathUtils.
* Added PolygonMath.ScaleOutside and PolygonPath.getParallelogramSize
* Modified BuilderExtension.PreLoadImage to include path and disposeStream parameters. If you are overriding this method, you will need to change your code.
* LoadImage now supports byte[] arrays.

#v3-alpha-10
Date: June 16 2011

FullFile: http://downloads.imageresizing.net/Resizer3-alpha-10-full-Jun-16-2011.zip

### Changes since alpha 9 (Jun 8)

## Bug fixes

* Fixed bug in ImageBuilder.Build() - ArgumentException if Build() is called twice on the same HttpPostedFile instance (due to stream being disposed).  LoadImage() no longer disposes the HttpPostedFile instance, and even restores the position of the stream afterwards.
* DiskCache: Fixed bug where cached files were not being re-used when hashModifiedDate=true or when using S3Reader in fast mode.

## New features

* Added new overload: string Build(object source, object dest, ResizeSettings settings, bool disposeSource, bool addFileExtension)
 Now you don't have to calculate file extensions when saving a resized image to disk - simply pass an extension-less path to 'dest' and get the resulting physical path back from the overload.
* VirtualFolder now supports multiple instances (you can create multiple virtual folders now)
* Added first draft of AzureReader (Created by Wouter Alberts with a bit of help from me). Can be found in the Contrib folder.
* Added PathUtils.RemoveExtension and PathUtils.RemoveFullExtension
* S3Reader: Added support for useSsl, accessKeyId, and secretAccessKeyId configuration in Web.config. Changed includeModifiedDate setting to checkForModifiedFiles .

## New examples

* ComplexWebApplication: Added example watermarking based on folder and output image size
* ComplexWebApplication: Added example on how to generate multiple image versions during upload.

## NuGet Packages

There are now [NuGet](http://nuget.org) packages for 8 of the 12 paid plugins, as well as 2 packages for the core and 1 sample project.

#v3-alpha-9
Date: June 8 2011

FullFile: http://downloads.imageresizing.net/Resizer3-alpha-9-full-Jun-8-2011.zip

### Changes since alpha 8 (Jun 2)

## Fixed bugs

* Content-type "image/jpeg" would be sent for PNG and GIF images when 'format' was not specified.
* Visiting the resizer.debug page would result in a NullReferenceException when the DiskCache was installed and autoClean=false or enabled=false was configured.
* ResizeSettings: Setting CropTopLeft and CropTopRight had no effect, the values weren't saved. (Setting ["crop"] always worked).
* Incorrect behavior when invalid (non-numeric) values were used for width, height, maxwidth, maxheight, shadowWidth, andgle, and rotate. The intended behavior was to interpret invalid values as 'unspecified'. Instead, they were interpreted as '0'. 
* Fixed potential NullReferenceException masking an ImageCorruptedException in LoadImage (wrong exception thrown).
* ResizeSettings: Rotate no longer rounds values to the nearest integer.
* ResizeSettings: Setting the BackgroundColor, PaddingColor, or BorderColor properties would cause the alpha portion of the assigned color to be ignored. Reading these properties when the underlying string values were invalid (like "ghhaggee") could have caused an Exception to be thrown instead of returning Color.Transparent. 
* ClientCache.Uninstall() returned false, despite uninstalling correctly. 

## New features

* Added support for image margins (outside the border and drop-shadow or other effect). Added ResizeSettings.Margin. 
* Added support for [independent, separate control over caching and processing](/docs/process-and-cache). (&cache=no/default/always, &process=no/default/always). This allows the DiskCache to be used for non-image data. For URLs without image extensions, however, you'll still need to add [a PostAuthorizeRequestStart handler](/docs/howto/cache-non-images).
* Added Pipeline.SkipFileTypeCheck, Pipeline.ModifiedQueryString, Pipeline.PreRewritePath so PostAuthorizeRequestStart can customize the processing and caching behavior.
* Added ResizeSettings.ToString(). Better debugging!
* Added ResizeSettings.Process property.
* Added PathUtils.GetExtension(). 

## Potentially breaking API changes

* ResizeSettings.get and .set are now protected instead of public, as they should have been originally.
* protected variable ImageBuilder.encoderProvider is now \_encoderProvider for CLS compliance.

## New features in SqlReader

* Now supports char, nchar, varchar, and nvarchar identifiers for images.
* Now supports loading and caching non-image files from SQL binary columns.
* SqlReaderSample: Added support for uploading and listing regular files. Added "Remove all images" button.
* Now supports named connection strings so you don't have to duplicate configuration. Uses the "ConnectionStrings:namedKey" syntax like the ASP.NET declarative data source controls.
* Now provides better diagnostics
* SqlReaderSettings: Added VirtualPathPrefix readonly property, StripFileExtension boolean property, and IsIntType, IsStringType methods.

## Tests

The bug fixes of this release were primarily driven by unit test discoveries. Code coverage doubled with this release, and I'm aggressively adding regression tests for everything I fix.

### Remaining known bugs

None. Isn't this when most people mark a product as stable?

#v3-alpha-8
Date: June 2 2011

FullFile: http://downloads.imageresizing.net/Resizer3-alpha-8-full-Jun-02-2011.zip

### Changes since alpha 7 (May 26)

### API changes (core)

* BuilderExtension/AbstractImageProcessor): Renamed OnBuildToStream to buildToStream
* PipelineConfig: Added PreRewritePath convenience property for Items[ModifiedPathKey]
* PathUtils: Added SetExtension, GetFullExtension, and AddExtension methods

### Bug fixes (core)

* Build(): Reading a corrupted image with Build() would cause a NullReferenceException instead of a ImageCorruptedException.
* Build(source,settings,disposeSettings) was ignoring the 'disposeSetting' boolean. Fixed.
* Build(LoadImage(stream),dest,settings,disposeSettings=false) was disposing the stream, due to a boolean logic error. Only affects nested LoadImage() calls such as used here.
* buildToStream now calls plugin methods

## Bug fixes (Plugins)

### DiskCache 
* autoClean=true now works (it was just pretending to work in previous releases) Once this was discovered, many more bugs came to light and were fixed.
* You can now configure the CleanupStrategy settings through XML. (See [the DiskCache docs](/plugins/diskcache) for details.
* DiskCache: New behavior with last accessed times. Since NTFS doesn't update them in Vista and up, we now explicitly update the index cache when we use a file. When refreshing file info from disk, the more recent 'accessed' value is kept.   To preserve the last-accessed value across app restarts, we lazily flush lastaccessedutc values to disk using the worker queue.

### AnimatedGif plugin
- Was also just pretending to work before. I somehow missed the test failure (yes, I had a manual test for it).
- Now properly extends BuilderExtension instead of AbstractImageProcessor - so it can actually resize GIFs.
- Uses c.CurrentImageBuilder.Build instead of this.buildToBitmap (so it actually encodes properly)
- Now uses source.RawFormat to filter GIF images instead of checking the output type. No longer swallows ExternalException, since we've found the cause, I think.
 
### SqlReader plugin

* Fixed configuration bug: Setting ImageIdType would incorrectly throw an exception.
* Behavior change: Now throws FileNotFoundException when an image doesn't exist, instead of causing a NullReferenceException later on.

## Samples and Documenation

* Added JCropExample ([read the article](http://nathanaeljones.com/573/combining-jcrop-and-server-side-image-resizing/))
* Added SqlReaderSample - Shows how to use the SqlReader plugin to resize and upload images to SQL.
* Added ComplexWebApplication\CropExample showing how to use jCrop with the image resizer
* ComplexWebApplication\UploadSample.aspx now works with multiple upload controls. Added commented-out code showing how to get a byte array for upload to SQL, etc.
* Removed 800x600 limitation on ComplexWebApplication - was accidentally left in during last release.
* Added some more sample pics

## Tests

* Added DiskCacheWebTest for real-world testing of the DiskCache cleanup worker

#v3-alpha-7
Date: May 26 2011

FullFile: http://downloads.imageresizing.net/Resizer3-alpha-7-full-May-26-2011.zip

## Changes since alpha 5 (May 15)

### Stability-related bug fixes

* Fixed serious bug introduced in alpha 5: Underlying stream is disposed before the bitmap instance is disposed. Affected all Build() overloads. This was primarily introduced with the *Replaced LoadImageFailed overloads* API change in 3.0.5, when streams became used universally instead of only for virtual files and passed Stream instances. This issue can manifest as an InvalidOperationException, or any of many 'random' GDI-related messages. Often appears when using the Watermark or AnimatedGifs plugin. See Core/gdi-bugs.txt for details on how this and all related issues were solved.
Bitmaps are now Tagged with a BitmapTag instance that references the underlying stream, stopping accidental garbage collection issues. 
* Fixed leak of intermediate Bitmap in Bitmap Build(source, settings) overload. Would still be quickly garbage collected due to being gen 0, but still incorrect behavior.
* Fixed potential threading bug in the Watermark plugin (related to concurrent cached Bitmap access). Important update for users of the Watermark plugin.

## New features

* Added first draft of 'cropxunits' and 'cropyunits'.
	These can be set to a decimal value.
	If set to a decimal value, crop units for that dimension will be interpreted as relative to the value. This allows easy cropping without knowing the original size of the image.
	For example, ?crop=10,10,90,90&cropxunits=100&cropyunits=100 will crop a 10% border off each edge.
* Added first version of ImageResizerGUI, a **WPF app for batch resizing images**. 
* **Added COM support**, and Samples\ScriptAccess folder of examples and registration scripts.
* Added example ConsoleApplication
* /resizer.debug now lists supported file extensions and querystring keys
* When disabled, /resizer.debug provides instructions on enabling itself.
* LOTS of documentation fixes. 

## API changes

* **Build now *always* disposes 'source' unless disposeSource=false** (added 2 new overloads with disposeSource boolean). **This behavior is 'safer', and generally preferred.**
* The SizeLimiting plugin is now only loaded by default in *ASP.NET applications*. It doesn't make sense to restrict WinForms, Console, and WPF applications by default.
* LoadPlugins is called immediately if not running ASP.NET. Previously, it was only called on the first request in an ASP.NET application, and was never called in WinForms/WPF/Console apps.
* Changed behavior: BuilderExtension.DecodeStreamFailed is only called once if DecodeStream returns null.

### API Additions

* Added Plugins.Install and Plugins.Uninstall convenience methods
* Added Plugins.VirtualImageProviders - no longer using dynamic query. 
* Added default Config() constructor for COM compatibility.
* Added Config.BuildImage(source,dest,string) shortcut for COM-friendly access.
* Added Config.WriteDiagnosticsTo and Config.GetDiagnosticsPage() to simplify debugging.
* Added Build(source, settings, bool disposeSource) overload (see API changes)
* Added Build(source, dest, settings, bool disposeSource) overload (see API changes)

### Bug fixes

* Fixed numerous bugs in the Watermark plugin (which may have resulted in Overflow errors or rendering anomalies.)
  * Now works outside ASP.NET
  * Now handles simulation layouts
  * Now supports physical paths
  * Now produces integer dimensions and position for the watermark 
  * No longer incorrectly upscales watermark to fill entire image when keepAspectRatio is true. 
  * No longer acts incorrectly when padding and size add up to > 1 and valuesPercentages=true.
* ImageBuilder now uses the correct Config instance when selecting an encoder. Previously used Config.Current instead of the EncodeProvider passed in the constructor. Symptom: Plugin encoders were ignored by ImageBuilder unless present in Config.Current. 
* AnimatedGifs now works with independent Config instances.
* BatchZipper no longer sends both failure and success notifications for the same item.
* Removed many unneeded dependencies from PsdReader and PrettyGifs plugins
* LoadImage no longer disposes the stream if a stream was passed directly to it.

#v3-alpha-5
Date: May 15 2011

FullFile: http://downloads.imageresizing.net/Resizer3-alpha-5-full-May-15-2011.zip

## Changes since alpha 3 (May 2)

### Bug fixes

* Fixed rounding bug which would occasionally cause a gap between an image and its border
* Png files are now served with the mime-type image/png instead of image/x-png. Chrome didn't support 'x-png' for individual requests.
* Added support for GoDaddy hosting (which prevents UrlAuthorizationModule.CheckUrlAccessForPrincipal calls).
If required permissions do not exist, (a) an issue will be logged, and (b) url authorization rules will not take effect.
* All DLls now share the same version number and assembly information (except title and guid).
* PsdPlugin loads properly now
* Diagnostic plugin now works in Classic mode, just add ".ashx". (/resizer.debug.ashx)
* Fixed build paths - some plugins were not building to the dlls directory

### Managed API Changes

* The **[SizeLimiting](/plugins/sizelimiting)** plugin is now installed by default! This helps protect against RAM usage DOS attacks. SizeLimiting now defaults to imageWidth=0, imageHeight=0, totalWidth=3200, totalHeight=3200. (imageWidth/Height were 1680x1680)
* Replaced LoadImageFailed overloads with DecodeStream and DecodeStreamFailed methods. (Allows plugins to decode alternative formats more easily)
* Replaced the Pipeline.PostAuthorizeImage event with Pipeline.AuthorizeImage. The new event allows handlers to prevent (as well as create) access denied responses by simply changing the default of "e.AllowAccess".
* Moved exception classes to the ImageResizer root namespace.
* Moved PathUtils to the ImageResizer.Util namespace
* Moved SafeList and ReverseEnumerator to the Collections namespace/folder. Added ReadOnlyDictionary class.

### New features

* Added FriendlyUrls plugin
* Added AdvancedFilters plugin
* Added CloudFront plugin
* Added Url rewriting example to ComplexWebApplication
* Added SamplePlugin example to ComplexWebApplication
* Added *lots* of docs
* Watermark plugin now supports overlays that are virtual files (such as 'gradient.png')
* Added support for modifying the path during PostAuthorizeRequest, using context.Items\[Config.Current.Pipeline.ModifiedPathKey\] (Enabling feature for CloudFront plugin)

#v3-alpha-3
Date: May 2 2011

FullFile: http://downloads.imageresizing.net/Resizer3-alpha-3-full-May-02-2011.zip

#v3-alpha-2
Date: Apr 24 2011

FullFile: http://downloads.imageresizing.net/ImageResizer3-full-alpha2_apr-24-2011.zip

#v2-8 
Date: May 27, 2011 Upgrade notes from V2.6 to v2.8
* **Update (Jan 15, 2012): This version has an unpatched memory leak in the GIF and PNG quantization system (Quantizer.cs). The issue has been patched in V3, but V2 has been deprecated for 1 year now. [Upgrading is a simple process](/docs/2to3/), and there are currently no plans to make another maintenance release of V2.**

This is probably the last update the V2 line will receive, as it was superseded by V3 on Apr. 24. Support for 2.8 will be ending June 15, 2011, **3 years** after V2 was first released.

This is a **high-priority update** for all users, as it blocks a potential avenue for a DOS attack and fixes many important bugs. Users of v2.6 can simply replace the ImageResizer.DLL file or the ImageResizer folder of .cs files. See [the changelog](/docs/v2/changelog) if you have an version prior to 2.6, as configuration changes may be required..

It is highly recommended that [you upgrade to V3 instead of V2.8, so you can continue to receive support and patches for the next few years](/docs/2to3/). V3 is designed for better performance, has an easier API, and is far more flexible. 

Existing users can upgrade before June 15 for only $40 using the discount code 60OFFLOYALTY.

<a href="/docs/2to3/" class="awesome green">Upgrade to V3</a>

<a href="http://downloads.imageresizing.net/ImageResizer2.8-full-may-27-2011.zip" class="awesome black">Download V2.8 (3.5MB)</a>

<a href="http://downloads.imageresizing.net/ImageResizer2.8-core-may-27-2011.zip" class="awesome black">Download V2.8 DLL only (100K)</a>

  * <span style="color:red;">Fixed serious limitation of ImageResizerMaxWidth/Height settings.</span>  
    These settings only control the size of the photo portion of the image. They do not limit the dimensions of the resulting bitmap. 
    
    **New behavior**: When the final dimensions of an image would exceed 2x the configured max width and height, the request will be ignored with the following message: "The specified image will be more than 2x the permitted size. Request terminated."
  * Fixed bug: Mime-type: image/x-png was being sent instead of image/png. **Causes Chrome to download images instead of displaying them.**
  * Fixed bug in disk caching system: **Cached files modified by just one day or one hour don't get updated.**
  * Fixed bug where **specifying both width and maxheight would cause width to be ignored.**
  * Fixed bug: Two simultaneous ImageManager.getBestInstance() calls at app startup could return two different instances.
  * Fixed bug causing Dictionary exception on the first request after the app was restarted. Only occurred if two simultaneous requests occurred. Only would happen once per app lifetime. 
  * Fixed potential bug: **Extremely** rare Access Denied message occurring on one of 2 simultaneous requests for a newly added source image. No reported occurrences.
  * Removed System.Data and System.Xml dependencies. 
  * 
#v2-6
Date: Nov 11, 2010

(<a href="http://nathanaeljones.com/489">Upgrade notes from 2.1b to 2.6</a>)
  * Fixed bug where a NullReference exception would occur if the Authentication module didn't process the request. All requests appear anonymous now in that situation.
  * Fixed rounding bug and added regression test. New behavior is to round ALL values before performing drawing, but AFTER math is done. Was previously trimming a line of pixels off certain images.
  * Fixed border bug where border was drawn over top of padding.
  * Fixed threading bug with creating the web.config file. Two concurrent requests would cause an exception.
  * Fixed bug where no URL Authorization was occurring UNLESS DisableImageURLAuthorization=TRUE in web.config (This bug did not exist in v2.1b, only in custom versions sent to customers between Mar. 19 and Nov. 11)
  * Fixed SecurityException errors occurring on GoDaddy and in other low-trust environments: changed the Animation plugin to use static methods instead of reflection. Users of the animation plugin, contact me for an updated version.
  * Added support for splitting the image cache into subfolders, allowing scalability to millions of images:
  * Set "ImageCacheSubfolders" to the number of required folders.
  * Added support for resizing images from VirtualPathProviders.
    Set either <em>ImageResizerUseVirtualPathProvider </em>or <em>ImageResizerUseVirtualPathProviderAsFallback </em>to true to enable the functionality. In Fallback mode, the virtual path provider is only called if no physical file exists.
  * Added support for implementing cache-friendly database-driven image resizing using a VirtualPathProvider.
  * Added IVirtualFileWithModified and IVirtualBitmapFile. Allow custom virtual path providers to be cache-friendly and even send bitmaps directly to the image resizer. Great for implementing new image formats.
  * Added &scale=UpscaleCanvas mode. Instead of upscaling the image, the canvas expands to the specified Width and Height.
  * Added DisableImageURLAuthorization setting. Set to TRUE to disable additional URL authorization checking within the resizer (imagecache is still protected).
  * Added BuildImage overloads with VirtualFile support
  * Added static event hooks for URL rewriting on images (replaces CustomFolders.cs, although CustomFolders.cs still works).
  * CustomFolders.cs will be removed in the next major revision.
  * Added the ability to specify custom extension/ImageFormat mappings, in case your jpegs are named .cow or .pig for some strange reason.
  * Added TranslatePoint methods to allow simulation of a resize (useful for image map generation).
  * Added Size GetFinalSize() methods to ImageManager.cs for determining the resulting size of an image.
  * Performance boost: modified DiskUtil.UpdateCachedVersionIfNeeded to use 'cachedFile' instead of 'sourceFile' as lock/sync key.
#v2-1b
Date: Nov. 13, 2009
(<a href="http://nathanaeljones.com/438/version-2-1b-released/">Upgrade notes from 2.0 to 2.1b</a>)
  * Fixed: Fixed elusive performance bug in DiskCache that caused directory listings to run every image request.
  * Added: GIF/PNG dithering support!
  * Added: Zero-IIS-configuration installation mode! No wildcard mapping needed. Syntax: "image.jpg.axd?width=500"
  * Fixed: All requests are forced to pass through the UrlAuthorizationModule now. Previously, any URL rewriting (like customfolders.cs) caused URL auth rules to be circumvented. This was documented behavior, but a secure solution has now been found.
  * Added: DisableCustomQuantization setting to allow GIFs to be generated on servers where the Marshal class is prohibited.
  * Added: PerfTests project to run benchmarks on the image resizing and encoding code.
  * Added: ImageManager.BuildImage now accepts an HttpPostedFile instance for resizing, making upload and resize simple. Sample project included.
  * 
#v2-0rc2 
Date: Jun 3, 2009

(<a href="http://nathanaeljones.com/11181_Image_Resizer_2_0_Upgrade_notes">Upgrade notes from 1.2 to 2.0</a>)
  * Fixed: Extremely rare bug where rounding causes Bitmap to be initialized with a dimension of 0, and causes a Parameter exception.
    Occurred when resizing an image to < 2px in height or width (usually happens with 2x1000 size images, etc).
    Added regression test for 500x2 image resized to 100px wide.
  * Fixed: Typo (missing else) in SaveToNonSeekableStream. This method is for extensibility, and is not used by the Resizer directly.
    This method is now tested and part of the Regression tests (HandlerTest.ashx).

#v2.0rc1
Date: May 21, 2009

(<a href="http://nathanaeljones.com/11181_Image_Resizer_2_0_Upgrade_notes">Upgrade notes from 1.2 to 2.0</a>)
  * Fixed: Transparency is preserved more reliably with GIF files. Certain GIF files were losing transparency because the way the color palette was constructed.
  * Fixed: .tif is now a supported input extension... previously only .tiff and .tff were allowed.
  * Added WatermarkSettings.cs class for watermarking. Easy to extend for your own use.
  * Converted ImageManager from a Static class to a normal class with a getBestInstance() static method. Allows easy plugin creation for ImageManager.
  * Added support for ?frame=1-x and ?page=1-x. You can now select frames from GIF images and pages from TIFF files. Removed ?time
  * Hashes are now SHA-256 instead of .NET 32-bit. They are base-16 encoded. This results in longer file names, but astronomically low chances of hash collisions.
  * Fixed upgrade notes link in upgrade notes.txt

#v2.0b 
Date: May 16, 2009 

(<a href="http://nathanaeljones.com/11181_Image_Resizer_2_0_Upgrade_notes">Upgrade notes from 1.2 to 2.0</a>)
  * Fixed: Incorrect aspect ratio issue if both maxwidth, width, and height are specified.<
  * Fixed: UNC hosted websites are now supported.
  * Added DisableCacheCleanup command, and made MaxCachedImages < 0 behave the same as DisableCacheCleanup=true
  * Fixed: rounding error that could cause a pixel line on the right and/or bottom sides of the image. Rare floating point rounding error in GDI native code. Added code to force rounding to be consistent.

#v2.0a
Date: Mar 4, 2009 (E-mail distribution)
  * Fixed: Cleanup routine can cause bottleneck on GetFiles() - fix so that Directory.GetFiles() only happens at startup and when items are added. Only affects sites with slow filesystems (or without filesystem caching), and with thousands of images.
  * Fixed: imagecache/ is not protected when AllowURLRewriting is enabled
http://localhost/resize(40,40)/imagecache/1639776677.jpg
bypasses it.  Added protection in the HttpModule.
  * Fixed: Potential issue in Quantizer.cs that may cause lines in GIF output.
  * Fixed Maxwidth/maxheight not getting picked up.
  * Fixed: Custom crop coordinates at 0 were being applied in the negative coordinate zone. Fixed so x1,y1 weren't affected, but setting x2 and y2 to 0 is bottom-right relative.
  * Changed flip to be after all operations, and added sourceFlip to replace its behavior.
  * Added -ignoreicc parameter and made ICC reading the default. ICC profiles are not written out - browser does not support them.

#v1-2
Date: 17 May 2008

[Original download source](http://www.nathanaeljones.com/blog/2008/server-side-image-resizing-module-for-asp-net-asp-phpiis-2)

##Upgrade Notes
Version 1.2 includes several new features, a simplified URL syntax, and easier installation. It now supports (and takes advantage of) IIS7 Integrated mode, as well as IIS5, IIS6, and IIS7 classic. The new version is now available for download. If you are an existing customer, you should already have received an e-mail containing a free upgrade. Query string syntax has changed (old links will still work, though). thumbnail has been renamed to format, and is no longer needed to force resizing. You can now simply use image.jpg?width=200 instead of image.jpg?thumbnail=jpg&width=200. I've also added a quality setting to adjust jpeg compression.

##Upgrade instructions

The resizer is now implemented as an HttpModule instead of an HttpHandler. Because of this, you will need to undo these changes to web.config and make these much simpler changes instead. There are also 2 new settings you can take advantage of. Using ImageResizerClientCacheMinutes, you can now control how long an image will sit in the browser cache before being updated. If you don't use ASP.NET's URL authorization system to protect your images, you may want to enable AllowFolderResizeNotation. You will then be able to /resize(40,40)/image.jpg instead of image.jpg?maxwidth=40&maxheight=40. Last, delete the /imagecache folder. It will automatically be re-created and populated as images are requested.

##Additional changes

The code has been refactored quite a bit - You'll notice there are now 5 code files. Image manipulation has been factored out into its own class ImageManager so you can use it directly from your code. Disk caching has similarly been abstracted into DiskCache.cs, so you can leverage that independently also. You can inherit from InterceptModule to easily build your own image processing pipelines (overriding CheckRequest and MakeResizedImage). Last but not least, you can modify CustomFolders.cs to force the resizing of all images in certain folders or matching certain patterns. The?download=true feature has been removed for cleanliness. If you want this feature back, leave feedback below.