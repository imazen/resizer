Tags: plugin
Edition: elite
Tagline: Decode and encode .webp images
Aliases: /plugins/webp

# WebP plugins

With [slimmage.js, you can use WebP for supporting browsers - without breaking the others](https://github.com/imazen/slimmage). Slimmage [makes responsive images easy to implement - both client *and* true image size is controlled with css `max-width` properties](https://github.com/imazen/slimmage). 

We [have a guide for using Slimmage and ImageResizer together](http://imageresizing.net/blog/2013/effortless-responsive-images).


## Security note

* Do NOT use this plugin with untrusted data. This plugin is a thin wrapper over `libwebp`, which is written in C.
* Specifically, it is a thin wrapper over this set of bindings: https://github.com/imazen/libwebp-net
* As of v4.3, native libwebp binaries (v1.6.1) are delivered via NuGet runtime packages and no longer require manual management. Keep your NuGet packages updated to get the latest security fixes.
* You can [monitor libwebp releases here](https://github.com/webmproject/libwebp/releases) and [search CVEs for the keyword webp](https://cve.mitre.org/cgi-bin/cvekey.cgi?keyword=webp).

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

## Common issues

* **Decoding WebP from a MemoryStream or byte array** — When using the managed API (`ImageBuilder.Build()`) with a `MemoryStream` or `byte[]` source, add `&decoder=webp` to your instructions. Without a `.webp` file extension, the decoder cannot auto-detect the format.
* **Bitness mismatch** — The native `libwebp` DLL must match your application's bitness. If your app pool runs in 32-bit mode, you need the win-x86 native package. In v4.3, NuGet installs the correct native DLLs automatically; ensure you have the `Imazen.WebP.NativeRuntime.win-x64` and/or `Imazen.WebP.NativeRuntime.win-x86` packages referenced.

## Version history

### v4.3 (current)

* libwebp native library upgraded from 0.6.0 to 1.6.1.
* CDN auto-download removed; native binaries now come from NuGet runtime packages (win-x64, win-x86, win-arm64).
* **Note:** NuGet `.targets`-based native DLL copying works for direct package references, but may not propagate transitively through project references on older .NET Framework projects. If you reference a class library that depends on this plugin, you may need to install the `Imazen.WebP.NativeRuntime.win-*` package directly in your startup/web project.
* ARM64 (win-arm64) support added.
* Imazen.WebP managed wrapper upgraded from 10.0.1 to 11.0.0.
* All .NET Framework projects now target .NET 4.7.2 (previously 4.5/4.5.2).

### v4.2.8 and prior

* Used libwebp 0.6.0 (pre-1.0).
* Native DLLs were downloaded from a CDN or manually placed in `/bin`.
* Only x86 and x64 platforms were supported (no ARM64).
* Imazen.WebP 10.0.1.
* Targeted .NET 4.5/4.5.2.

