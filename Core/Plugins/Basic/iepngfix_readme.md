Tags: plugin
Bundle: free
Edition: free
Tagline: Automatically serve GIF versions of PNG files to IE6 and below clients. Opt-in or opt-out, very configurable.
Aliases: /plugins/iepngfix

# IEPngFix plugin (New in V3.1.3)

This plugin automatically redirects PNG file requests from Internet Explorer 6 and below to GIF versions of the images. For best results, use with the PrettyGifs plugin, as the default .NET GIF encoding is very poor.

Does not work well with Amazon CloudFront, CDNs, or caching proxies. 

Introduced in V3.1.3

## Installation

1. Add `<add name="IEPngFix" />` to the `<plugins />` section.

## Configuration

To enable 'catchall' mode, add `catchAll="true"`

  Ex. `<add name="IEPngFix" catchAll="true" />`

Note: If you have to use the '.ashx' syntax due to your server configuration, catchAll will not be able to intercept static PNG requests; only those using the .ashx syntax.

To use rewriting instead of redirects, add `redirect="false"`.
  Ex. `<add name="IEPngFix" redirect="false" />`

If redirect="true" (the default), the requests from IE will be HTTP redirected to new URLs. 
If false, the GIF will be silently served instead of the PNG, without any redirection.
A CDN or caching proxy will mess things up regardless, but using redirection ensures that the CDN/proxy never caches the GIF version instead of the PNG.

## Syntax

* When `catchAll=false`, the behavior is opt-in. You must add `&iefix=true` to enable the browser detection and redirection behavior for the URL.
* When `catchAll=true`, the behavior is opt-out. You must add `&iefix=false` to disable the browser detection and redirection behavior for the URL.

## Examples

`image.png?iefix=true` will redirect to `image.png?format=gif&iefix=true` under IE6 or earlier.

The plugin works even if PNG is not the original format. For example, 

`image.jpg?format=png&iefix=true` will redirect to `image.jpg?format=gif&iefix=true` under IE6.

