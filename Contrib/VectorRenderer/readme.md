Tags: plugin
Aliases: /plugins/psd /plugins/vect.. /plugins/vectorrenderer
Edition: elite
Tagline: Adds support for EPS and SVG files

# VectorRenderer plugin

*PLEASE NOTE*
* ** In case of EPS you will need Ghostscript https://www.ghostscript.com/download/gsdnld.html


Adds support for .EPS and .SVG source files. No configuration required.

## Installation

Either run `Install-Package ImageResizer.Plugins.VectorRenderer` in the NuGet package manager, or:

1. Add ImageResizer.Plugins.VectorRenderer.dll to your project
2. Add `<add name="VectorRenderer" />` inside `<resizer><plugins></plugins></resizer>` in Web.config.

## Usage

Simply reference a .eps or .svg file as you would a .jpg

	image.eps?width=100
    image.svg?width=100

A 100px wide JPEG will be returned.

You can also simply convert the image to another format.

    image.eps?format=png
    image.svg?format=jpg