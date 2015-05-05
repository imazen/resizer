Tags: plugin
UserVoice: true
Edition: performance
Tagline: Allows you to use Amazon CloudFront with the resizer. Highly recommended - offers inexpensive worldwide edge caching and great scalability.
Aliases: /plugins/cloudfront


# CloudFront plugin

Makes the ImageResizer work nicely with CDNs that strip off query strings by default, such as Amazon CloudFront and Azure CDN. When you create a 'distribution' or 'endpoint', you often have the chance to enable querystring support. If you can do that, you don't need this plugin.

## Installation

Either run `Install-Package ImageResizer.Plugins.CloudFront` in the NuGet package manager, or:

1. Add a reference to ImageResizer.Plugins.CloudFront.dll in your project.
2. Add `<add name="CloudFront" />` in the `<plugins>` section of Web.Config

## Details

Many CDNs strip off all querystring data before passing the request on to the origin server (the Image Resizer). To avoid this limitation, we've devised an alternate syntax using semicolons.

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

If you need to invalidate a cached file sooner than 24 hours, you must change the url (ex. by adding ";invalidate=1" to it), or by using [Amazon's invalidation request feature](http://docs.amazonwebservices.com/AmazonCloudFront/latest/DeveloperGuide/index.html?Invalidation.html).


## Automatic redirection of standard (`image.jpg?width=..`) URLs back to the CDN.

(In v3.1 and higher)

The CloudFront plugin can automatically redirect image requests to use the CloudFront distribution instead of directly serving the request. 

### Instructions


1. In the `<resizer>` section, just add `<cloudfront redirectThrough="http://d3urjqacv88oxz.cloudfront.net" redirectPermanent="false" />`. 
2. Change d3urjqacv88oxz.cloudfront.net to match the distribution name you created in the AWS console. 

The redirectThrough setting tells the CloudFront plugin to redirect any standard URLs back through the CloudFront distribution, automatically rewriting them to the semicolon syntax so everything will work properly. This feature, when configured, allows you to use normal `image.jpg?width=100&height=200` urls, without specifying either the distribution name, full path, or using the semicolon syntax in the anchor link. 

As automatic redirection requires the browser to make an additional HTTP request, latency may be increased, but overall request time may be slightly lower for large images, due to the faster connection available between the CloudFront server and the client. The primary advantages of automatic redirection are (a) increased scalability of the origin server, and (b) low developer cost - no extra work required.

If you have configured a CNAME mask for your CloudFront distribution, and would like to transfer the 'SEO weight' from the old URLs to the new CNAME-based urls, set redirectPermanent=true. 

## Automatic image URL translation

To remove the requirement of an extra request, yet keep the developer/webmaster load to a minimum, it is necessary to process all outgoing HTML and translate those URLs to cloudfront URLs dynamically. 

This kind of behavior could be useful outside the scope of the image resizer, as it could be used to edge-cache a variety of files (such as javascript, css, audio files, etc.) without having to manually modify the content. However, image URLs are the ones most easily changed without adverse affects.

Two possible options for modifying image URLs in HTML output are Control Adapters and Html filters. 

If you're interested in testing this functionality, send me an e-mail, as I'd like to get several use cases ready before plunging into development. 


Please send feedback! There's a little tab at the bottom that makes it easy. You can even suggest ideas and vote for them. Check it out!
