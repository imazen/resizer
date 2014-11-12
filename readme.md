# ImageResizer - Much more than image resizing

develop branch: [![Build status](https://ci.appveyor.com/api/projects/status/77a52n4hd0y36ybs/branch/develop?svg=true)](https://ci.appveyor.com/project/imazen/resizer/branch/develop) develop-must-rebase branch: [![Build status](https://ci.appveyor.com/api/projects/status/77a52n4hd0y36ybs/branch/develop-must-rebase?svg=true)](https://ci.appveyor.com/project/imazen/resizer/branch/develop-must-rebase)

We use submodules - clone with `git clone -b develop --recursive https://github.com/imazen/resizer` or run `git submodule update --init --recursive` afterwards.

[Visit our home page for details and documentation](http://imageresizing.net). 

What is it?

* An IIS/ASP.NET HttpModule & image server
* An image processing library optimized and secured for server-side use
* A framework and collection of 40+ plugins to accomplish most common web imaging needs. 


## Install

Nearly all [ImageResizer plugins are on NuGet (33+)](https://www.nuget.org/packages?q=nathanaeljones).

Get the basics:
```
PM> Install-Package ImageResizer.WebConfig
PM> Install-Package ImageResizer.Plugins.DiskCache
PM> Install-Package ImageResizer.Plugins.PrettyGifs
```

## Intro

ImageResizer has a very simple (and powerful) URL API. Check out the [documentation](http://imageresizing.net/docs).

![Fit modes](http://z.zr.io/rw/diagrams/resizing-modes.png)


## License

Over half of ImageResizer's plugins are available under the Apache 2.0 license. See license.txt for details


# Major changes in V4

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


