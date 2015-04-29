Aliases: /docs/requirements /docs/workswith/requirements /docs/workswith/mediumtrust /docs/workswith/hosting /docs/system_requirements

# System Requirements

## Supported operating systems

Both 32 and 64-bit versions of Windows are supported. OS X and Linux can be used under the latest version of Mono, but individual plugin support may vary.

* Windows 2000 (v3 and below only)
* Windows Server 2000 (v3 and below only)
* Windows XP (v3 and below only)
* Windows Server 2003 (and R2) (v3 and below only)
* Windows Vista
* Windows Server 2008 (and R2)
* Windows 7
* Windows Server 2012
* Windows 8
* Windows 8.1


## Supported Windows web servers

One of the following web servers is required to host the image resizing server, although you can use any web server to host the actual application that calls it. For example, you could run both a Ruby server and IIS on the same machine, performing the image processing in IIS, but hosting the application in Ruby, Java, Python, or whichever modern language you prefer.
 
* IIS 5 (v3 and below only)
* IIS 5.1  (v3 and below only)
* IIS 6 (v3 and below only)
* IIS 7 (both classic and integrated mode supported)
* IIS 7.5
* IIS 8
* IIS 8.5
* IIS Express
* Cassini, the Visual Studio Web Development Server (WebDev.exe).

## Supported .NET Framework versions

* .NET 2.0 SP2 (v3 and below only)
* .NET 3.0  (v3 and below only)
* .NET 3.5 (v3 and below only)
* .NET 4.0 (v3 and below only)
* .NET 4.5.X
* .NET 4.6.X

## Plugins that require .NET 3.5 on v3

* BatchZipper
* PsdReader
* S3Reader
* Wic plugins
* ImageResizer.Mvc
* PdfRenderer
* PsdComposer
* MvcRoutingShim


Visual Studio 2013 or 2012 is suggested for opening the sample projects and source code, although you can use 2008 if you rebuild the project files.


## Hosting environments

Any hosting provider offering full trust will work. 

Unfortunately, many use Medium or Partial Trust, and restrict ASP.NET permissions to the point where some plugins can't run. Others just restrict (or don't provide enough) bandwidth, making image serving unbearably slow. 

Microsoft [considers Medium Trust obsolete](http://stackoverflow.com/questions/16849801/is-trying-to-develop-for-medium-trust-a-lost-cause) and [advises all hosters to migrate to proper OS-level isolation instead](https://support.microsoft.com/en-us/kb/2698981).

We suggest avoiding DiscountASP and WebHost4Life due to bandwidth limitations.
We suggest avoiding shared sites on Rackspace as they have very strict and erratic customized trust levels that can disable most plugins. Dedicated or virtual servers with Rackspace are not affected. 
Avoid Shared and Free Azure Websites  - both CPU and bandwidith use is metered. Basic and Standard tiers are not affected. 

We use Amazon EC2. 

## Notes on using ImageResizer (v3 only) in Medium trust

### ImageResizer v3 is designed to support medium trust (and low trust). Some plugins, however, require full trust due to their nature. v4 does not support medium trust

Note: Ensure you're using `requirePermission="false"` when registering the ImageResizer configSection in Web.config


Most Essential Edition plugins in v3 are medium-trust compatible. These include AutoRotate, ClientCache, DefaultEncoder, DefaultSettings, Diagnostics, FolderResizeSyntax, ImageHnadlerSyntax, Image404, Gradient, IEPngFix, Image404, MvcRoutingShim, SizeLimiting, Presets, DropShadow, and SpeedOrQuality. 

Essential Edition plugins CustomOverlay, Logging, and VirtualFolder can work as well, but are more sensitive to I/O permission restrictions.

Performance Edition plugins DiskCache, MemCache, MemSourceCache, SourceDiskCache, and AnimatedGifs should be medium-trust compatible.
  
Creative Edition plugins SimpleFilters and Watermark should be medium-trust compatible.

