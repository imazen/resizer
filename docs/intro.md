
# What is ImageResizer?

* An IIS/ASP.NET HttpModule & image server. On-demand image manipulation, delivery, and optimization &mdash; with low latency &mdash; makes responsive images easy
* An image processing library optimized and secured for server-side use
* A framework and collection of 40+ plugins to accomplish most common web imaging needs. 

ImageResizer has a very simple (and powerful) URL API.

![Fit modes](http://z.zr.io/rw/diagrams/resizing-modes.png)


## Basic Installation Guide

**Important!**: The MVC Routing plugin is required on .NET4+

#### Install from NuGet

Nearly all [ImageResizer plugins are on NuGet (33+)](https://www.nuget.org/packages?q=nathanaeljones). [You can enable source symbols from symbolsource.org](http://www.symbolsource.org/Public/Home/VisualStudio) for an enhanced debugging experience.

Get the basics:

```
PM> Install-Package ImageResizer.WebConfig
PM> Install-Package ImageResizer.Plugins.DiskCache
PM> Install-Package ImageResizer.Plugins.PrettyGifs
PM> Install-Package ImageResizer.MvcWebConfig
```

#### Manual Plugin Installation

1. In *your* project, add a reference to the plugin DLL (or project, if you're using the source).
2. Configure the plugin to be installed at startup via (i) or (ii). 
  1.  In the [&lt;plugins /> section](#config_reference) of Web.config, insert `<add name="PluginName" />`
  2.  In `Application_Start`, create an instance of the plugin and install it.

``` c#
  new PluginName().Install(ImageResizer.Configuration.Config.Current);
```

You will need need to add the appopriate namespace reference to access the plugin.

Most configuration and plugin installation issues can be resolved by checking ImageResizer’s self-diagnostics page. If your local website is running at `http://localhost:5000/`, then you should browse to `http://localhost:5000/resizer.debug.ashx` to access it. See [the Troubleshooting](#troubleshooting section) for more details. 
<a name="reference"></a>


#### Want the Source?

We use submodules - clone using either 

```
git clone -b develop --recursive https://github.com/imazen/resizer

- or -

git clone https://github.com/imazen/resizer
git checkout develop
git submodule update --init --recursive

```

Make sure to add a project reference. 

If you're contributing, make sure you start by checking out the `develop` branch, and *then* making your changes. See our  [CONTRIBUTING](https://github.com/imazen/resizer/blob/develop/CONTRIBUTING.md) guide on GitHub.

We also publish source code with each release that you can get [from the download page](/download).
