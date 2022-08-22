Tags: plugin
UserVoice: true
Edition: performance
Tagline: Obsolete. Created before Amazon CloudFront natively supported query strings.
Aliases: /plugins/cloudfront


# CloudFront plugin

This plugin is **NOT** required to use Amazon CloudFront. It exists for historical reasons. At one time, Amazon CloudFront stripped off all querystrings. 

Today, just enable querystring support/preservation when you create a 'distribution' or 'endpoint'. If you can do that, you don't need this plugin.

## Installation

Either run `Install-Package ImageResizer.Plugins.CloudFront` in the NuGet package manager, or:

1. Add a reference to ImageResizer.Plugins.CloudFront.dll in your project.
2. Add `<add name="CloudFront" />` in the `<plugins>` section of Web.Config

## Details

Some CDNs strip off all querystring data before passing the request on to the origin server running ImageResizer. To avoid this limitation, we devised an alternate syntax using semicolons.

    image.jpg;width=100;height=100;crop=auto

This allows us to reference a CDN server, but still dynamically process and edge-cache images.

Here's a URL directly to my origin server

   http://images.imageresizing.net/red-leaf.jpg;width=100

Here's a URL pointing to the CDN (I've set up a CNAME to mask the distribution name). The CDN sends the request on to the origin server, caches the response, and sends it back to the current (and any future) clients.

   http://img.imageresizing.net/red-leaf.jpg;width=100

Unless you set up a CNAME to mask it, your URL will look like this: 

    http://d3urjqacv88oxz.cloudfront.net/red-leaf.jpg;width=100

Feel free to play around with my URLs and experiment. 

## Caching duration notes

By default, CloudFront caches all requests for a minimum of 24 hours (1440 minutes), but you can now configure this limit when you create a new distribution.

To set the caching time at the server instead of at CloudFront, set `<clientcache minutes="1441" />` in the `<resizer>` section of Web.config. 

If you need to invalidate a cached file sooner than 24 hours, you must change the URL (ex. by adding ";invalidate=1" to it), or by using [Amazon's invalidation request feature](http://docs.amazonwebservices.com/AmazonCloudFront/latest/DeveloperGuide/index.html?Invalidation.html).


## Automatic redirection of standard (`image.jpg?width=..`) URLs back to the CDN.

The CloudFront plugin can be configured to HTTP redirect image requests arriving in querystring (`?key=value1` format) to use the CloudFront distribution instead of directly serving the request. 

We don't suggest this except to reduce server load in an emergency. Redirects make your site load more slowly in client browsers.

### Instructions


1. In the `<resizer>` section, just add `<cloudfront redirectThrough="http://d3urjqacv88oxz.cloudfront.net" redirectPermanent="false" />`. 
2. Change d3urjqacv88oxz.cloudfront.net to match the distribution name you created in the AWS console. 

The redirectThrough setting tells the CloudFront plugin to redirect any standard URLs back through the CloudFront distribution, automatically rewriting them to the semicolon syntax so everything will work properly. This feature, when configured, allows you to use normal `image.jpg?width=100&height=200` URLs, without specifying either the distribution name, full path, or using the semicolon syntax in the anchor link.

As automatic redirection requires the browser to make an additional HTTP request, latency may be increased, but overall request time may be slightly lower for large images, due to the faster connection available between the CloudFront server and the client. The primary advantages of automatic redirection are (a) increased scalability of the origin server, and (b) low developer cost - no extra work required.

If you have configured a CNAME mask for your CloudFront distribution, and would like to transfer the 'SEO weight' from the old URLs to the new CNAME-based URLs, set redirectPermanent=true.

