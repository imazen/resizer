Tags: plugin
Edition: elite
Tagline: Decode and encode .webp images
Aliases: /plugins/webp

# WebP plugins

With [slimmage.js, you can use WebP for supporting browsers - without breaking the others](https://github.com/imazen/slimmage). Slimmage [makes responsive images easy to implement - both client *and* true image size is controlled with css `max-width` properties](https://github.com/imazen/slimmage). 

We [have a guide for using Slimmage and ImageResizer together](http://imageresizing.net/blog/2013/effortless-responsive-images).


## Security note

* Do NOT use this plugin with untrusted data. This plugin is a thin wrapper over `libwebp`, which is written in C, and has not yet reached version 1.0.
* Specifically, it is a thin wrapper over this set of bindings:  https://github.com/imazen/libwebp-net
* **You are responsible for locating and using the latest version of `libwebp.dll`. The included copy is most likely out of date, and may not contain the latest security fixes.** ImageResizer 4.1.0 uses libwebp 0.6.0.
* You can [monitor libwebp releases are here](https://github.com/webmproject/libwebp/releases) and [search CVEs for the keyword webp](https://cve.mitre.org/cgi-bin/cvekey.cgi?keyword=webp).

## Installation

1. Either run `Install-Package ImageResizer.Plugins.WebP` in the NuGet package manager, or add `ImageResizer.Plugins.WebP.dll` to your project.

2. Add `<add name="WebPEncoder" />` and/or `<add name="WebPDecoder" />` inside `<resizer><plugins></plugins></resizer>` in Web.config.

## WebPDecoder

Simply reference a .webp file as you would a .jpg

    image.webp?width=100&format=jpg

A 100px wide JPEG will be returned.

If the extension is not .webp, you can add `&decoder=webp` to force WebP decoding first, instead of waiting for the fallback path.


## WebPEncoder

Add `&format=webp` to any URL to encode the result in WebP format instead of JPEG/PNG

### Parameters

* Quality=1..100
* Lossless=true/false (defaults false)
* NoAlpha=true/false (defaults false)


### Rule of thumb for converting JPEG quality values to WebP

In general, WebP achieves the same visual quality with a much lower `quality` parameter.

The first value is the JPEG quality, second is WebP quality for same visual clarity.

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

