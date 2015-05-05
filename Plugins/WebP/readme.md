Tags: plugin
Edition: elite
Tagline: Decode and encode .webp images
Aliases: /plugins/webp

# WebP plugins

With [slimmage.js, you can use WebP for supporting browsers - without breaking the others](https://github.com/imazen/slimmage). Slimmage [makes responsive images easy to implement - both client *and* true image size is controlled with css `max-width` properties](https://github.com/imazen/slimmage). 

We [have a guide for using Slimmage and ImageResizer together](http://imageresizing.net/blog/2013/effortless-responsive-images).

## Installation

1. Either run `Install-Package ImageResizer.Plugins.WebP` in the NuGet package manager, or add `ImageResizer.Plugins.WebP.dll` to your project.

2. Add `<add name="WebPEncoder" />` and/or `<add name="WebPDecoder" />` inside `<resizer><plugins></plugins></resizer>` in Web.config.

## WebPDecoder

Simply reference a .webp file as you would a .jpg

    image.webp?width=100&format=jpg

A 100px wide jpeg will be returned. 

If the extension is not .webp, you can add `&decoder=webp` to force webp decoding first, instead of waiting for the fallback path.


## WebPEncoder

Add `&format=webp` to any URL to encode the result in webp format instead of jpg/png

### Parameters

* Quality=1..100
* Lossless=true/false (defaults false)
* NoAlpha=true/false (defaults false)


### Rule of thumb for converting jpeg quality values to webp

In general, webp achieves the same visual quality with a much lower  `quality` parameter. 

The first value is the jpeg quality, second is webp quality for same visual clarity.

* 90->78
* 80->65 
* 70->55 
* 50->40
* 40->30
* 20->10
* 10->3
* 5->0

### HTTP Error 404.3 - Not Found

> The page you are requesting cannot be served because of the extension configuration. If the page is a script, add a handler. If the file should be downloaded, add a MIME map.


When you get this error, you'll need to add a mime-type mapping in web.config


    <configuration>
      <system.webServer>
        <staticContent>
         <mimeMap fileExtension=".webp" mimeType="image/webp" />
      </staticContent>
      </system.webServer>
    </configuration>

