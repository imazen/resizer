Tags: plugin
Edition: creative
Tagline: Content-aware image resizing.
Aliases: /plugins/seamcarving


# SeamCarving plugin

Provides content-aware image resizing and 5 different algorithms.

Based on the [CAIR (v2.19)](https://sites.google.com/site/brainrecall/cair) library.

This plugin requires full trust to work. On 32-bit PNG images, the alpha channel is reduced to a single bit. Works best with jpeg and gif images.

For live examples, [see this page](http://nathanaeljones.com/596/dynamic-seam-carving-with-imageresizing-net/).

## Algorithm choices

* carve=true
* carve=prewitt
* carve=v1
* carve=vsquare
* carve=sobel
* carve=laplacian

## Example use:

    image.jpg?width=300&height=300&carve=true

## Installation

Either run `Install-Package ImageResizer.Plugins.SeamCarving` in the NuGet package manager, or:

1. Add ImageResizer.Plugins.SeamCarving.dll to your project
2. Add `<add name="SeamCarving" />` inside `<resizer><plugins></plugins></resizer>` in Web.config.
