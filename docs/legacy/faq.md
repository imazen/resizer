Aliases: /docs/faq

# Frequently Asked Questions

## General

Why do I get an error message whenever I...
: If you're getting an error message, [see the troubleshooting page instead](/docs/troubleshoot).

How can I ensure only images with a command querystring incur additional processing?
: You don't need to do anything; that's the default behavior.

Are multi-page .TIFFs supported?
: Yes - you can convert any page of a multi-page .TIFF to .jpg, .png, or .gif. Use ?page=x&amp;format=jpg

Is load balancing across multiple servers supported?
: Yes, and all plugins support web farms, web gardens, and load balancing.

Do the image names change when they are resized? Is SEO affected? 
: No, image names are retained - SEO isn't affected.

Is it necessary to wildcard map everything to ASP.NET when using IIS6?
: No - you can map .jpg, .png, and .gif individually, but [you need to follow this KB article](http://support.microsoft.com/Default.aspx?kbid=909641) to prevent issues.

I'm getting OutOfMemory exceptions when I try to resize certain large images for the first time (subsequent requests are fine). They're only 15MB in jpeg form, and I have 100MB of free RAM.
: A 15MB JPG uncompresses to about 80MB in bitmap form (depending upon the compression level). If you are resizing to a 2MB jpeg (15MB BMP), memory requirements for the operation are roughly 110MB (15 + 80 + 15). If you plan on using the resizer for very high-resolution photos (above 8MP), I suggest making sure you have ample amounts of RAM. 400MB to 1GB is usually plenty for the average web site with disk caching enabled.

I have a bundle with ImageResizer, but I see it's no longer available for sale. What gives?
: Bundles were discontinued in February 2013, although all bundle purchases are still valid. You can find out any information you need about bundles [here](/plugins/bundles).

I have a question about a license or payment.
: Check out our store's own [faq page](https://store.imazen.io/faq) for questions on licenses.

## Resizing Tasks

How do I auto-crop a certain distance or percentage away from an edge, instead of centering?
: Manual cropping is applied before automatic cropping, so you can use that to crop the image before auto-cropping takes effect. To auto-crop against 10% away from the top of the image, use this: `crop=0,10,0,0&cropxunits=100&cropyunits=100&mode=crop&width=570&height=1500&anchor=topcenter`.

Can I resize the same image to different sizes?
: Yes, there is no limit on how many sizes of an image can be created.

Is it possible to specify the width and height, and have your image resized and cropped to fit the aspect ratio, losing as little image area as possible?
: Yes, and it is easy. Use ?width=x&amp;height=y&amp;mode=crop.

If I resize a photo to its original size, will a new photo be returned, or the original?
: All photos are re-compressed, even if the original photo is the same size. This allows ICC correction, file size improvement, and metadata removal.

Can the resizer crop, then resize at the same time? 
: Yes, you can specify a crop rectangle with ?crop=(x1,y1,x2,y2) or ?mode=crop, and add &amp;width=x or any of the resizing commands to resize the resulting crop. All commands can be combined.

When I resize a small image to larger dimensions, it stays at the original size. 
: This is by design - add &amp;scale=both to allow images to be upscaled. You might want to consider up-scaling client-side to save bandwidth. Just set both width and height on the &lt;img&gt; tag. If you just want padding, use &amp;scale=canvas

## Image Sources

Can I use this on images not located on the server? 
: Yes, with RemoteReader (Any HTTP server), S3Reader (Amazon S3 blobs), AzureReader (Azure blobs), MongoReader (GridFS) or  SqlReader (SQL blobs).

Can I use this with images stored in a database? 
: Sure, with the SqlReader plugin. 

Can I use this to resize images as users upload them? 
: Sure! I suggest keeping the original images around and using the resizer normally (in case you later want larger images).
However, it's easy to [resize during upload](/docs/howto/upload-and-resize).



