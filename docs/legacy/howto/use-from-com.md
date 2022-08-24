Aliases: /docs/howto/use-from-com

# Using the ImageResizer from COM

While the [URL API method is preferred](/docs/howto/use-without-asp-net), you can use ImageResizer with the COM API. This means VB6, VBScript, JScript, C++, and other COM-compatible languages can resize images, even if they're desktop apps or command-line scripts.

To make ImageResizer accessible to COM clients, you must follow [the COM installation guide](/docs/install/com).


### JScript example

	//The first step is to create a configuration object (where you register plugins, configure stuff, etc.)
	var c = new ActiveXObject("ImageResizer.Configuration.Config");

	//This is how you install a plugin
	var p = new ActiveXObject("ImageResizer.Plugins.PrettyGifs.PrettyGifs");
	p.Install(c);

	//This is how you generate an image
	c.BuildImage("..\\Images\\quality-original.jpg","grass.gif", "rotate=3&width=600&format=gif&colors=128");

	// You can even get diagnostics output if something's not working
	// c.WriteDiagnosticsTo("advancedScript-1.txt");

	//You can create different configurations
	var c2 = new ActiveXObject("ImageResizer.Configuration.Config");

	//And use them separately
	c2.BuildImage("..\\Images\\quality-original.jpg","grass-ugly.gif", "rotate=3&width=600&format=gif");

	//c2.WriteDiagnosticsTo("advancedScript-2.txt");

### VBScript example

	Set c = CreateObject("ImageResizer.Configuration.Config")

	c.BuildImage "..\\Images\\tractor.jpg", "tractor-rotated.jpg", "rotate=45"


## VBScript COM API example

	Set c = CreateObject("ImageResizer.Configuration.Config")
	c.BuildImage "tractor.jpg", "tractor-rotated.jpg", "rotate=45"

## JScript COM API example

	var c = new ActiveXObject("ImageResizer.Configuration.Config");
	c.BuildImage("tractor.jpg","tractor-rotated.jpg", "rotate=45");


### Using plugins

	Set c = CreateObject("ImageResizer.Configuration.Config")
	' This line installs the plugin, so it can enhance GIF output quality
	CreateObject("ImageResizer.Plugins.PrettyGifs.PrettyGifs").Install(c) 
	c.BuildImage "tractor.jpg", "tractor-rotated.gif", "rotate=45&format=gif"



### List of full plugin names

When using a plugin with the COM API, you must use the full name of the plugin, not the shortened one. 

The following are the full plugin names (as of July 26, 2011)


	ImageResizer.Plugins.Basic.VirtualFolder
	ImageResizer.Plugins.DiskCache.DiskCache
	ImageResizer.Plugins.PsdReader.PsdReader
	ImageResizer.Plugins.PrettyGifs.PrettyGifs
	ImageResizer.Plugins.Basic.Image404
	ImageResizer.Plugins.AnimatedGifs.AnimatedGifs
	ImageResizer.Plugins.Basic.Gradient
	ImageResizer.Plugins.Basic.FolderResizeSyntax
	ImageResizer.Plugins.SimpleFilters.SimpleFilters
	ImageResizer.Plugins.CloudFront.CloudFrontPlugin
	ImageResizer.Plugins.RemoteReader.RemoteReaderPlugin
	ImageResizer.Plugins.AdvancedFilters.AdvancedFilters
	ImageResizer.Plugins.CloudFront.CloudFrontPlugin
	ImageResizer.Plugins.SeamCarving.SeamCarvingPlugin
	ImageResizer.Plugins.Watermark.WatermarkPlugin
	ImageResizer.Plugins.SeamCarving.SeamCarvingPlugin
	ImageResizer.Plugins.S3Reader.S3Reader