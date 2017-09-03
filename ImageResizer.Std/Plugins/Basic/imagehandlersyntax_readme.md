Tags: plugin
Bundle: free
Edition: free
Aliases: /plugins/imagehandlersyntax%20compatibility%20shim%20that%20mimics%20the%20URL%20syntax%20of%20Web%20Image%20Resize%20Handler /plugins/imagehandlersyntax
Tagline: Migrate websites from other image resizing handlers without breaking any URLs.

# ImageHandlerSyntax plugin

*PLEASE NOTE*
* **This plugin will be removed in a future major release, as there are no known users.**
* **For forwards-compatibility with ImageResizer and Imageflow, avoid use.**

Adds support for the syntaxes used by 4 image resizing handlers. This plugin allows painless, gradual migration from them by supporting their URL syntax.

## Supported projects

* http://webimageresizer.codeplex.com/
* http://imagehandler.codeplex.com/
* http://bbimagehandler.codeplex.com/
* http://bip.codeplex.com/


## Installation

1. Add `<add name="ImageHandlerSyntax" />` to the `<plugins />` section.



## WebImageResizer compatibility status

As of V3.1, support has been added for *all* WebImageResizer querystring commands, *all* WebImageResizer encoding options, and *all* WebImageResizer file provider options. 100% URL compatibility has been added, and full feature parity (with improved performance and stability) is available when the [DiskCache](/plugins/diskcache), [SimpleFilters](/plugins/simplefilters), [WicEncoder](/plugins/wic), and [FreeImageEncoder](/plugins/freeimage) plugins are installed. However, unless you're doing grayscale or inversion, the only plugin you really need is the [DiskCache](/plugins/diskcache).

Supports: `src`, `width`, `height`, `format`, `greyscale`, `rotate`, `zoom`, `invert`

To encode with WPF/WIC, add `encoder=wic`. To encode with FreeImage, add `encoder=freeimage`. 

All [normal commands](/docs/reference) are also supported, and all 27+ plugins can be used in combination with the ImageHandlerSyntax compatibility plugin.

If you have implemented an `IImageProvider` class for WebImageResizer, you will need to modify it to implement the `IVirtualImageProvider` interface instead.

Please report any compatibility issues or discrepancies between ImageHandlerSyntax and WebImageResizer. 