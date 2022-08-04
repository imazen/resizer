Aliases: /docs/requirements /docs/workswith/requirements /docs/workswith/mediumtrust /docs/workswith/hosting /docs/system_requirements

# System Requirements

## Supported operating systems

Both 32 and 64-bit versions of Windows are supported.

* Windows Server 2012
* Windows 8
* Windows 8.1
* Windows Server 2016
* Windows 10
* Windows Server 2019
* Windows Server 10

## Supported Windows web servers

One of the following web servers is required to host the image resizing server, although you can use any web server to host the actual application that calls it. For example, you could run both a Ruby server and IIS on the same machine, performing the image processing in IIS, but hosting the application in Ruby, Java, Python, or whichever modern language you prefer.
 
* IIS 7 (both classic and integrated mode supported)
* IIS 7.5
* IIS 8
* IIS 8.5
* IIS 10
* IIS Express
* Cassini, the Visual Studio Web Development Server (WebDev.exe).

## Supported .NET Framework versions

* .NET 4.7.2 and higher (but not 5.x)

Visual Studio 2019 or higher is required for opening the sample projects and source code.

## Hosting environments

Any hosting provider offering full trust will work. All dedicated and virtual servers should support Full Trust. Shared hosting plans may not; we suggest avoiding these.

Microsoft ASP.NET has dropped support for Medium and Partial trust environments, and ImageResizer has as well. Microsoft [considers Medium Trust obsolete](http://stackoverflow.com/questions/16849801/is-trying-to-develop-for-medium-trust-a-lost-cause) and [advises all hosters to migrate to proper OS-level isolation instead](https://support.microsoft.com/en-us/kb/2698981).

Some hosting providers (such as DiscountASP and WebHost4Life) restrict (or don't provide enough) bandwidth, making image serving unbearably slow. 

Avoid Shared and Free Azure Websites  - both CPU and bandwidith use is metered. Basic and Standard tiers are not affected. 

We use AppHarbor internally, but often suggest Amazon EC2.