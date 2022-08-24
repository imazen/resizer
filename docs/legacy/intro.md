
# What is ImageResizer?

* An IIS/ASP.NET HttpModule & image server. On-demand image manipulation, delivery, and optimization &mdash; with low latency &mdash; makes responsive images easy
* An image processing library optimized and secured for server-side use
* A framework and collection of 40+ plugins to accomplish most common web imaging needs. 

ImageResizer has a very simple (and powerful) URL API.

![Fit modes](http://z.zr.io/rw/diagrams/resizing-modes.png)

## Basic Installation Guide

#### Install from NuGet

Nearly all [ImageResizer plugins are on NuGet (33+)](https://www.nuget.org/packages?q=nathanaeljones). If you like to live dangerously, try our nightly builds from `https://www.myget.org/F/imazen-nightlies/`.

Get the basics:

```
PM> Install-Package ImageResizer.WebConfig
PM> Install-Package ImageResizer.Plugins.DiskCache
PM> Install-Package ImageResizer.Plugins.PrettyGifs
```

#### Manual Plugin Installation

1. In *your* project, add a reference to the plugin DLL (or project, if you're using the source).
2. Configure the plugin to be installed at startup via (i) or (ii).  
   (i)  In the [&lt;plugins /> section](#config_reference) of Web.config, insert `<add name="PluginName" />`  
   (ii)  In `Application_Start`, create an instance of the plugin and install it.

```
new PluginName().Install(ImageResizer.Configuration.Config.Current);
```

You will need to add the appropriate namespace reference to access the plugin.

Most configuration and plugin installation issues can be resolved by checking ImageResizer’s self-diagnostics page. If your local website is running at `http://localhost:5000/`, then you should browse to `http://localhost:5000/resizer.debug.ashx` to access it. See [the Troubleshooting page](#troubleshooting section) for more details. 

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

We also publish source code with each stable release that you can get [from the download page](/download).
