Tags: plugin
Edition: free
Bundle: free
Tagline: Allows you to configure the default setting values when commands (like `scale`) are omitted.
Aliases: /plugins/defaultsettings

# DefaultSettings plugin (V3.1+)

Allows you to specify the default settings to use when certain settings are omitted. Currently supports ScaleMode defaults. May be extended to allow defaults for other settings in the future - please [create an idea](http://resizer.uservoice.com) for additional features you'd like to see.


## Installation

1. Add `<add name="DefaultSettings" />` to the `<plugins />` section.

## Configuration Syntax

    <resizer>
    ...
      <defaultsettings explicitSizeScaleMode="DownscaleOnly" 
                       maxSizeScaleMode="DownscaleOnly" />
    ...
    </resizer>

## Configuration reference

* `explicitSizeScaleMode` is the default ScaleMode value to use when `width` and/or `height` are being used
* `maxSizeScaleMode` is the default ScaleMode value to use when `maxwidth` and/or `maxheight` are being used, or when `mode`=`max`.

### Valid ScaleMode values:

* `DownscaleOnly` - The default. Only downsamples images - never enlarges. If an image is smaller than 'width' and 'height', the image coordinates are used instead.
* `UpscaleOnly` - Only upscales (zooms) images - never downsamples except to meet web.config restrictions. If an image is larger than 'width' and 'height', the image coordinates are used instead.
* `Both` -  Upscales and downscales images according to 'width' and 'height', within web.config restrictions.
* `UpscaleCanvas` - When the image is smaller than the requested size, padding is added instead of stretching the image. `anchor` can be used to determine where the padding is added.


## History

On October 22, a 1-question survey was conducted asking users to vote one which ScaleMode defaults made the most sense.

**Should &scale=both be the default instead of &scale=downscaleonly?**

* 20% (10 votes) for *Yes! I think that (by default) both maxwidth/maxheight and width/height should upscale an image if the original is smaller than the specified size.*
* 34% (17 votes) for *Only for width/height - upscaling makes sense for width/height because they specify an exact size, but not for maxwidth/maxheight, which only imply a maximum size.*
* 46% (23 votes) for *No! Leave it the way it is - if the image is smaller than width/height, it should remain the original size unless &scale=both is set.*

In addition, one user wrote in a suggestion for scale=UpscaleCanvas to be the default instead.

Unexpectedly, 46% of users wanted to keep the existing behavior, having images to revert to their original size when smaller regardless of exact (width,height) or maximum (maxwidth,maxheight) size specification. Many users have contacted support, surprised by that behavior, and I had expected nobody to like (or expect) it.

So, nothing has changed. The defaults are the same as in V3, V2, and V1. But, we've introduced this new plugin for the 34% & 20% groups. 

If you voted for 'Only for width/height', use

    <defaultsettings explicitSizeScaleMode="Both" />

If you voted "Yes!", use

   <defaultsettings explicitSizeScaleMode="Both" maxSizeScaleMode="Both" />

If you voted for upscaling the canvas, use

   <defaultsettings explicitSizeScaleMode="UpscaleCanvas" maxSizeScaleMode="UpscaleCanvas" />
  

Hopefully this will help make everyone happy - feel free to contact me at support@imageresizing.net or [create an idea](http://resizer.uservoice.com) if you have ideas or suggestions, I'm always willing and eager to make the Image Resizer easier to understand and use.

